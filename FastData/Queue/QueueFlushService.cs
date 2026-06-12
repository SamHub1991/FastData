#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastData.Model;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;
using NewLife.Log;

namespace FastData.Queue
{
    /// <summary>
    /// 队列刷写服务
    /// 后台服务，监控消息队列并在数据库恢复后自动刷写
    /// 实现"写入后端"模式的自动恢复功能
    /// </summary>
    public class QueueFlushService : IDisposable
    {
        private readonly MessageQueueIntegrationService _mqService;
        private readonly CancellationTokenSource _cts;
        private readonly Dictionary<string, Task> _consumerTasks;
        private bool _disposed;

        /// <summary>
        /// Initializes a queue flush service.
        /// </summary>
        /// <param name="mqService">Message queue integration service.</param>
        public QueueFlushService(MessageQueueIntegrationService mqService)
        {
            _mqService = mqService ?? throw new ArgumentNullException(nameof(mqService));
            _cts = new CancellationTokenSource();
            _consumerTasks = new Dictionary<string, Task>();
        }

        /// <summary>
        /// 启动所有已注册表的队列消费者
        /// </summary>
        public void Start()
        {
            var configs = WriteBehindRegistry.GetAllConfigs();

            foreach (var kvp in configs)
            {
                var tableName = kvp.Key;
                var config = kvp.Value;

                if (config.QueueType == WriteBehindQueueType.None || !config.EnableAutoRecovery)
                    continue;

                StartConsumerForTable(tableName, config);
            }

            XTrace.WriteLine($"[QueueFlushService] 已启动 {_consumerTasks.Count} 个队列消费者");
        }

        /// <summary>
        /// 启动指定表的队列消费者
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="config">写入配置</param>
        private void StartConsumerForTable(string tableName, WriteBehindConfig config)
        {
            var topic = config.Topic ?? tableName.ToLower();
            var queueType = config.QueueType == WriteBehindQueueType.ReliableQueue
                ? MessageQueueType.ReliableQueue
                : MessageQueueType.Stream;

            var task = Task.Run(async () =>
            {
                XTrace.WriteLine($"[QueueFlushService] 启动消费者: {topic} ({queueType})");

                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        // 消费队列中的操作
                        await _mqService.StartConsumerAsync<WriteOperation>(
                            topic,
                            async (operation) =>
                            {
                                try
                                {
                                    // 检查是否超过最大重试次数
                                    if (operation.RetryCount >= operation.MaxRetries)
                                    {
                                        // 超过最大重试次数，移入死信队列
                                        XTrace.WriteLine($"[QueueFlushService] 超过最大重试次数({operation.MaxRetries})，移入死信队列: {operation.TableName} {operation.OperationType} ID={operation.OperationId}");
                                        await MoveToDeadLetterQueue(topic, operation, $"超过最大重试次数({operation.MaxRetries})");
                                        return; // 消费完成，不再重新入队
                                    }

                                    // 尝试将操作写入数据库
                                    var result = ExecuteOperation(operation);
                                    if (result.Success)
                                    {
                                        XTrace.WriteLine($"[QueueFlushService] 恢复写入成功: {operation.TableName} {operation.OperationType}");
                                    }
                                    else
                                    {
                                        // 写入仍然失败，增加重试计数后重新入队
                                        operation.RetryCount++;
                                        XTrace.WriteLine($"[QueueFlushService] 恢复写入失败({operation.RetryCount}/{operation.MaxRetries})，重新入队: {operation.TableName} - {result.ErrorMessage}");
                                        _mqService.PublishSingle(topic, operation, queueType);
                                        await Task.Delay(config.RecoveryIntervalSeconds * 1000, _cts.Token); // 等待后再试
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    throw; // 取消异常，向上传播
                                }
                                catch (Exception ex)
                                {
                                    // 增加重试计数
                                    operation.RetryCount++;
                                    
                                    if (operation.RetryCount >= operation.MaxRetries)
                                    {
                                        // 超过最大重试次数，移入死信队列
                                        XTrace.WriteLine($"[QueueFlushService] 处理异常，超过最大重试次数({operation.MaxRetries})，移入死信队列: {operation.TableName} - {ex.Message}");
                                        await MoveToDeadLetterQueue(topic, operation, ex.Message);
                                    }
                                    else
                                    {
                                        XTrace.WriteLine($"[QueueFlushService] 处理异常({operation.RetryCount}/{operation.MaxRetries})，重新入队: {operation.TableName} - {ex.Message}");
                                        _mqService.PublishSingle(topic, operation, queueType);
                                    }
                                    
                                    await Task.Delay(config.RecoveryIntervalSeconds * 1000, _cts.Token);
                                }
                            },
                            _cts.Token,
                            queueType,
                            concurrency: 1);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine($"[QueueFlushService] 消费者异常: {ex.Message}");
                        await Task.Delay(5000); // 等待 5 秒后重试
                    }
                }
            });

            _consumerTasks[topic] = task;
        }

        /// <summary>
        /// 执行单个写入操作
        /// </summary>
        private WriteOperationResult ExecuteOperation(WriteOperation operation)
        {
            var result = new WriteOperationResult
            {
                OperationType = operation.OperationType,
                TableName = operation.TableName
            };

            try
            {
                var key = operation.DatabaseKey;
                var writeReturn = QueueWriteInvoker.Execute(operation, key);

                result.Success = writeReturn.IsSuccess;
                if (!writeReturn.IsSuccess)
                {
                    result.ErrorMessage = writeReturn.Message;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 停止所有消费者
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            XTrace.WriteLine("[QueueFlushService] 已停止所有消费者");
        }

        /// <summary>
        /// 获取队列状态
        /// </summary>
        public Dictionary<string, object> GetStatus()
        {
            var status = new Dictionary<string, object>();
            var configs = WriteBehindRegistry.GetAllConfigs();

            foreach (var kvp in configs)
            {
                var tableName = kvp.Key;
                var config = kvp.Value;
                var topic = config.Topic ?? tableName.ToLower();

                try
                {
                    var queueStatus = _mqService.GetQueueStatus(topic,
                        config.QueueType == WriteBehindQueueType.ReliableQueue
                            ? MessageQueueType.ReliableQueue
                            : MessageQueueType.Stream);
                    status[tableName] = queueStatus;
                }
                catch (Exception ex)
                {
                    status[tableName] = new { Error = ex.Message };
                }
            }

            return status;
        }

        /// <summary>
        /// 将失败的操作移入死信队列
        /// </summary>
        private async Task MoveToDeadLetterQueue(string originalTopic, WriteOperation operation, string reason)
        {
            var deadLetterTopic = $"{originalTopic}.deadletter";
            
            try
            {
                // 在元数据中记录失败信息
                if (operation.Metadata == null)
                    operation.Metadata = new Dictionary<string, object>();
                
                operation.Metadata["DeadLetterReason"] = reason;
                operation.Metadata["DeadLetterTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                operation.Metadata["OriginalTopic"] = originalTopic;
                operation.Metadata["FinalRetryCount"] = operation.RetryCount;
                
                // 发布到死信队列
                _mqService.PublishSingle(deadLetterTopic, operation, MessageQueueType.ReliableQueue);
                
                XTrace.WriteLine($"[QueueFlushService] 已移入死信队列: {deadLetterTopic}, 操作ID={operation.OperationId}, 原因={reason}");
            }
            catch (Exception ex)
            {
                // 死信队列也失败时，记录错误日志
                XTrace.WriteLine($"[QueueFlushService] 移入死信队列失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从死信队列重新处理指定操作
        /// </summary>
        public async Task<bool> RetryFromDeadLetterQueue(string operationId, string topic = null)
        {
            try
            {
                var deadLetterTopic = topic != null ? $"{topic}.deadletter" : null;
                
                // 这里需要提供具体的重试逻辑
                // 实际实现需要从死信队列中取出操作并重新处理
                XTrace.WriteLine($"[QueueFlushService] 重新处理死信队列操作: {operationId}");
                
                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"[QueueFlushService] 重新处理死信队列操作失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops consumers and releases service resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    _cts?.Dispose();
                    
                    // 等待所有消费者任务完成
                    foreach (var task in _consumerTasks.Values)
                    {
                        try
                        {
                            task?.Wait(TimeSpan.FromSeconds(10));
                        }
                        catch
                        {
                            // 忽略等待异常
                        }
                    }
                }
                
                _disposed = true;
            }
        }
    }
}
#endif
