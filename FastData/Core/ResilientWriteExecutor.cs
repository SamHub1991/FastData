using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using FastData.Base;
using FastData.Config;
using FastData.ConnectionPool;
using FastData.Context;
using FastData.Model;
using FastData.Queue;
using FastRedis.Messaging;
using FastUntility.Base;
using NewLife.Caching;

namespace FastData.Core
{
    /// <summary>
    /// 弹性写入执行器
    /// 集成连接池和消息队列，实现自动降级
    /// 
    /// 工作流程：
    /// 1. 尝试从连接池获取连接
    /// 2. 如果连接池耗尽，自动降级到消息队列
    /// 3. 消息队列会在数据库恢复后自动重试写入
    /// </summary>
    public class ResilientWriteExecutor : IDisposable
    {
        private readonly string _databaseKey;
        private readonly ConnectionPoolConfig _poolConfig;
        private readonly WriteBehindConfig _queueConfig;
        private readonly FullRedis _redis;
        private readonly bool _enableQueueFallback;
        private readonly int _maxRetries;
        private readonly int _retryDelayMs;

        /// <summary>
        /// 统计信息
        /// </summary>
        private long _directWriteCount;
        private long _queueFallbackCount;
        private long _totalFailureCount;

        /// <summary>Gets the number of direct database writes.</summary>
        public long DirectWriteCount => Interlocked.Read(ref _directWriteCount);
        /// <summary>Gets the number of writes that fell back to the queue.</summary>
        public long QueueFallbackCount => Interlocked.Read(ref _queueFallbackCount);
        /// <summary>Gets the number of failed writes.</summary>
        public long TotalFailureCount => Interlocked.Read(ref _totalFailureCount);

        /// <summary>
        /// 初始化弹性写入执行器
        /// </summary>
        /// <param name="databaseKey">数据库配置键</param>
        /// <param name="poolConfig">连接池配置（可选）</param>
        /// <param name="queueConfig">队列配置（可选）</param>
        /// <param name="redis">Redis 实例（可选，不提供则不启用队列降级）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        public ResilientWriteExecutor(
            string databaseKey = null,
            ConnectionPoolConfig poolConfig = null,
            WriteBehindConfig queueConfig = null,
            FullRedis redis = null,
            int maxRetries = 3,
            int retryDelayMs = 100)
        {
            _databaseKey = databaseKey;
            _poolConfig = poolConfig;
            _queueConfig = queueConfig ?? new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                EnableFallback = true,
                EnableAutoRecovery = true
            };
            _redis = redis;
            _enableQueueFallback = redis != null && _queueConfig.EnableFallback;
            _maxRetries = maxRetries;
            _retryDelayMs = retryDelayMs;

            if (_enableQueueFallback)
            {
                WriteBehindExecutor.Initialize(redis);
            }
        }

        /// <summary>
        /// 执行写入操作（带自动降级）
        /// </summary>
        /// <param name="operation">写入操作</param>
        /// <returns>执行结果</returns>
        public ResilientWriteResult ExecuteWrite(WriteOperation operation)
        {
            var result = new ResilientWriteResult();

            // 尝试直接写入数据库（带重试）
            for (int retry = 0; retry <= _maxRetries; retry++)
            {
                try
                {
                    using var context = new DataContext(_databaseKey, poolConfig: _poolConfig);
                    var writeResult = ExecuteSingleOperation(context, operation);

                    if (writeResult.IsSuccess)
                    {
                        Interlocked.Increment(ref _directWriteCount);
                        result.Success = true;
                        result.UsedDirectWrite = true;
                        result.WriteReturn = writeResult;
                        return result;
                    }

                    // 写入失败，检查是否需要重试
                    if (retry < _maxRetries)
                    {
                        Thread.Sleep(_retryDelayMs * (retry + 1)); // 指数退避
                        continue;
                    }
                }
                catch (ConnectionPoolExhaustedException)
                {
                    // 连接池耗尽，降级到消息队列
                    if (_enableQueueFallback)
                    {
                        return FallbackToQueue(operation, "连接池耗尽");
                    }

                    // 未启用队列降级，记录失败
                    result.Success = false;
                    result.ErrorMessage = "连接池耗尽且未启用队列降级";
                    Interlocked.Increment(ref _totalFailureCount);
                    return result;
                }
                catch (Exception ex)
                {
                    // 其他异常，检查是否需要重试
                    if (retry < _maxRetries)
                    {
                        Thread.Sleep(_retryDelayMs * (retry + 1));
                        continue;
                    }

                    // 重试次数用完，降级到队列
                    if (_enableQueueFallback)
                    {
                        return FallbackToQueue(operation, string.Format("数据库异常: {0}", ex.Message));
                    }

                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    Interlocked.Increment(ref _totalFailureCount);
                    return result;
                }
            }

            // 所有重试都失败
            if (_enableQueueFallback)
            {
                return FallbackToQueue(operation, "重试次数用完");
            }

            result.Success = false;
            result.ErrorMessage = "重试次数用完";
            Interlocked.Increment(ref _totalFailureCount);
            return result;
        }

        /// <summary>
        /// 执行写入操作（异步版本）
        /// </summary>
        public async Task<ResilientWriteResult> ExecuteWriteAsync(WriteOperation operation, CancellationToken cancellationToken = default)
        {
            var result = new ResilientWriteResult();

            for (int retry = 0; retry <= _maxRetries; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var context = new DataContext(_databaseKey, poolConfig: _poolConfig);
                    var writeResult = ExecuteSingleOperation(context, operation);

                    if (writeResult.IsSuccess)
                    {
                        Interlocked.Increment(ref _directWriteCount);
                        result.Success = true;
                        result.UsedDirectWrite = true;
                        result.WriteReturn = writeResult;
                        return result;
                    }

                    if (retry < _maxRetries)
                    {
                        await Task.Delay(_retryDelayMs * (retry + 1), cancellationToken);
                        continue;
                    }
                }
                catch (ConnectionPoolExhaustedException)
                {
                    if (_enableQueueFallback)
                    {
                        return FallbackToQueue(operation, "连接池耗尽");
                    }

                    result.Success = false;
                    result.ErrorMessage = "连接池耗尽且未启用队列降级";
                    Interlocked.Increment(ref _totalFailureCount);
                    return result;
                }
                catch (Exception ex)
                {
                    if (retry < _maxRetries)
                    {
                        await Task.Delay(_retryDelayMs * (retry + 1), cancellationToken);
                        continue;
                    }

                    if (_enableQueueFallback)
                    {
                        return FallbackToQueue(operation, string.Format("数据库异常: {0}", ex.Message));
                    }

                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    Interlocked.Increment(ref _totalFailureCount);
                    return result;
                }
            }

            if (_enableQueueFallback)
            {
                return FallbackToQueue(operation, "重试次数用完");
            }

            result.Success = false;
            result.ErrorMessage = "重试次数用完";
            Interlocked.Increment(ref _totalFailureCount);
            return result;
        }

        /// <summary>
        /// 批量写入（带自动降级）
        /// </summary>
        public ResilientBatchWriteResult ExecuteBatchWrite(List<WriteOperation> operations)
        {
            var result = new ResilientBatchWriteResult();
            var failedOps = new List<WriteOperation>();

            foreach (var op in operations)
            {
                var writeResult = ExecuteWrite(op);
                if (writeResult.Success)
                {
                    result.SuccessCount++;
                    if (writeResult.UsedDirectWrite)
                        result.DirectWriteCount++;
                    else
                        result.QueuedCount++;
                }
                else
                {
                    failedOps.Add(op);
                    result.FailedCount++;
                }
            }

            result.Success = result.FailedCount == 0;
            result.FailedOperations = failedOps;
            return result;
        }

        /// <summary>
        /// 降级到消息队列
        /// </summary>
        private ResilientWriteResult FallbackToQueue(WriteOperation operation, string reason)
        {
            var result = new ResilientWriteResult();

            try
            {
                var topic = _queueConfig.Topic ?? operation.TableName?.ToLower() ?? "default";
                var queueType = _queueConfig.QueueType == WriteBehindQueueType.ReliableQueue
                    ? MessageQueueType.ReliableQueue
                    : MessageQueueType.Stream;

                var mqService = WriteBehindExecutor.MqService;
                var published = mqService.PublishData(topic, new List<WriteOperation> { operation }, queueType);

                if (published > 0)
                {
                    Interlocked.Increment(ref _queueFallbackCount);
                    result.Success = true;
                    result.UsedQueueFallback = true;
                    result.FallbackReason = reason;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = string.Format("队列写入失败，原始原因: {0}", reason);
                    Interlocked.Increment(ref _totalFailureCount);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = string.Format("队列降级失败: {0}，原始原因: {1}", ex.Message, reason);
                Interlocked.Increment(ref _totalFailureCount);
            }

            return result;
        }

        /// <summary>
        /// 执行单个写入操作
        /// </summary>
        private WriteReturn ExecuteSingleOperation(DataContext context, WriteOperation operation)
        {
            switch (operation.OperationType)
            {
                case WriteOperationType.Add:
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject(operation.Data, 
                        Type.GetType(operation.EntityType));
                    var addResult = context.Add(model);
                    return addResult.WriteReturn;

                case WriteOperationType.Update:
                    var updateModel = Newtonsoft.Json.JsonConvert.DeserializeObject(operation.Data, 
                        Type.GetType(operation.EntityType));
                    var updateResult = context.Update(updateModel);
                    return updateResult.WriteReturn;

                case WriteOperationType.Delete:
                    var deleteModel = Newtonsoft.Json.JsonConvert.DeserializeObject(operation.Data, 
                        Type.GetType(operation.EntityType));
                    var deleteResult = context.Delete(deleteModel);
                    return deleteResult.WriteReturn;

                default:
                    return new WriteReturn { IsSuccess = false, Message = string.Format("不支持的操作类型: {0}", operation.OperationType) };
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public ResilientExecutorStats GetStats()
        {
            return new ResilientExecutorStats
            {
                DirectWriteCount = DirectWriteCount,
                QueueFallbackCount = QueueFallbackCount,
                TotalFailureCount = TotalFailureCount,
                TotalRequests = DirectWriteCount + QueueFallbackCount + TotalFailureCount,
                QueueFallbackRate = (DirectWriteCount + QueueFallbackCount) > 0
                    ? (double)QueueFallbackCount / (DirectWriteCount + QueueFallbackCount) * 100
                    : 0
            };
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStats()
        {
            Interlocked.Exchange(ref _directWriteCount, 0);
            Interlocked.Exchange(ref _queueFallbackCount, 0);
            Interlocked.Exchange(ref _totalFailureCount, 0);
        }

        /// <summary>
        /// Releases resources held by the executor.
        /// </summary>
        public void Dispose()
        {
            // 清理资源
        }
    }

    /// <summary>
    /// 弹性写入结果
    /// </summary>
    public class ResilientWriteResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 是否使用直接写入
        /// </summary>
        public bool UsedDirectWrite { get; set; }

        /// <summary>
        /// 是否使用队列降级
        /// </summary>
        public bool UsedQueueFallback { get; set; }

        /// <summary>
        /// 降级原因
        /// </summary>
        public string FallbackReason { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 写入返回值
        /// </summary>
        public WriteReturn WriteReturn { get; set; }
    }

    /// <summary>
    /// 弹性批量写入结果
    /// </summary>
    public class ResilientBatchWriteResult
    {
        /// <summary>
        /// 是否全部成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 直接写入数量
        /// </summary>
        public int DirectWriteCount { get; set; }

        /// <summary>
        /// 队列写入数量
        /// </summary>
        public int QueuedCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 失败的操作列表
        /// </summary>
        public List<WriteOperation> FailedOperations { get; set; } = new List<WriteOperation>();
    }

    /// <summary>
    /// 弹性执行器统计信息
    /// </summary>
    public class ResilientExecutorStats
    {
        /// <summary>Gets or sets direct database write count.</summary>
        public long DirectWriteCount { get; set; }
        /// <summary>Gets or sets queue fallback count.</summary>
        public long QueueFallbackCount { get; set; }
        /// <summary>Gets or sets total failure count.</summary>
        public long TotalFailureCount { get; set; }
        /// <summary>Gets or sets total request count.</summary>
        public long TotalRequests { get; set; }
        /// <summary>Gets or sets queue fallback rate percentage.</summary>
        public double QueueFallbackRate { get; set; }
    }
}
