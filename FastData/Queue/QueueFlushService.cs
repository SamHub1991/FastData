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
using Newtonsoft.Json;

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
                                    // 尝试将操作写入数据库
                                    var result = ExecuteOperation(operation);
                                    if (result.Success)
                                    {
                                        XTrace.WriteLine($"[QueueFlushService] 恢复写入成功: {operation.TableName} {operation.OperationType}");
                                    }
                                    else
                                    {
                                        // 写入仍然失败，重新入队
                                        XTrace.WriteLine($"[QueueFlushService] 恢复写入失败，重新入队: {operation.TableName} - {result.ErrorMessage}");
                                        _mqService.PublishSingle(topic, operation, queueType);
                                        await Task.Delay(config.RecoveryIntervalSeconds * 1000); // 等待后再试
                                    }
                                }
                                catch (Exception ex)
                                {
                                    XTrace.WriteLine($"[QueueFlushService] 处理异常: {ex.Message}");
                                    // 重新入队
                                    _mqService.PublishSingle(topic, operation, queueType);
                                    await Task.Delay(config.RecoveryIntervalSeconds * 1000);
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
                WriteReturn writeReturn;
                var key = operation.DatabaseKey;

                switch (operation.OperationType)
                {
                    case WriteOperationType.Add:
                        var model = JsonConvert.DeserializeObject(operation.Data, global::System.Type.GetType(operation.EntityType));
                        writeReturn = FastWrite.Add(model, key: key);
                        break;

                    case WriteOperationType.Update:
                        var updateModel = JsonConvert.DeserializeObject(operation.Data, global::System.Type.GetType(operation.EntityType));
                        writeReturn = FastWrite.Update(updateModel, key: key);
                        break;

                    case WriteOperationType.Delete:
                        var deleteModel = JsonConvert.DeserializeObject(operation.Data, global::System.Type.GetType(operation.EntityType));
                        writeReturn = FastWrite.Delete(deleteModel, key: key);
                        break;

                    default:
                        throw new NotSupportedException($"不支持的操作类型: {operation.OperationType}");
                }

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

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _cts?.Dispose();
                _disposed = true;
            }
        }
    }
}
#endif
