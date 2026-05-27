#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;
using NewLife.Log;

namespace FastData.Queue
{
    /// <summary>
    /// 读取队列执行器
    /// 负责将读取请求推送到消息队列
    /// </summary>
    public static class ReadQueueExecutor
    {
        private static MessageQueueIntegrationService _mqService;
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// 初始化执行器（使用默认 Redis 连接）
        /// </summary>
        /// <param name="redisConnectionString">Redis 连接字符串</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public static void Initialize(string redisConnectionString = "127.0.0.1:6379", int redisDb = 7)
        {
            lock (_lock)
            {
                if (_initialized) return;

                var redis = new FullRedis
                {
                    Server = redisConnectionString,
                    Db = redisDb,
                    Timeout = 15000
                };
                _mqService = new MessageQueueIntegrationService(redis);
                _initialized = true;
            }
        }

        /// <summary>
        /// 初始化执行器（使用现有 Redis 实例）
        /// </summary>
        /// <param name="redis">FullRedis 实例</param>
        public static void Initialize(FullRedis redis)
        {
            lock (_lock)
            {
                if (_initialized) return;
                _mqService = new MessageQueueIntegrationService(redis);
                _initialized = true;
            }
        }

        /// <summary>
        /// 获取消息队列服务
        /// </summary>
        public static MessageQueueIntegrationService MqService => _mqService;

        /// <summary>
        /// 执行读取操作列表
        /// </summary>
        /// <param name="operations">操作列表</param>
        /// <param name="databaseKey">数据库 Key</param>
        /// <param name="overrideConfig">覆盖配置（可选）</param>
        /// <returns>执行结果</returns>
        public static ReadQueueResult Execute(List<ReadOperation> operations, string databaseKey = null, WriteBehindConfig overrideConfig = null)
        {
            var result = new ReadQueueResult { Success = true };

            // 按表分组操作
            var groupedOps = GroupByTable(operations);

            foreach (var group in groupedOps)
            {
                var tableName = group.Key;
                var tableOps = group.Value;
                var config = overrideConfig ?? WriteBehindRegistry.GetConfig(tableName);
                var useQueue = config != null && config.QueueType != WriteBehindQueueType.None;

                if (useQueue)
                {
                    // 推送到消息队列
                    var tableResult = PushToQueue(tableOps, config);
                    MergeResult(result, tableResult);
                }
                else
                {
                    // 未配置队列，记录失败
                    foreach (var op in tableOps)
                    {
                        result.FailedCount++;
                        result.Details.Add(new ReadOperationResult
                        {
                            OperationType = op.OperationType,
                            TableName = op.TableName,
                            Success = false,
                            ErrorMessage = $"表 {tableName} 未配置消息队列",
                            Metadata = op.Metadata
                        });
                    }
                    result.Success = false;
                }
            }

            return result;
        }

        /// <summary>
        /// 推送到消息队列
        /// </summary>
        private static ReadQueueResult PushToQueue(List<ReadOperation> operations, WriteBehindConfig config)
        {
            var result = new ReadQueueResult { Success = true };

            try
            {
                EnsureInitialized();
                var topic = config.Topic ?? operations[0].TableName.ToLower();
                var queueType = config.QueueType == WriteBehindQueueType.ReliableQueue
                    ? MessageQueueType.ReliableQueue
                    : MessageQueueType.Stream;

                var queuedCount = _mqService.PublishData(topic, operations, queueType);
                result.QueuedCount += queuedCount;

                foreach (var op in operations)
                {
                    result.Details.Add(new ReadOperationResult
                    {
                        OperationType = op.OperationType,
                        TableName = op.TableName,
                        Success = true,
                        Metadata = op.Metadata
                    });
                }

                XTrace.WriteLine($"[ReadQueueExecutor] 已将 {queuedCount} 条查询请求推送到队列: {topic}");
            }
            catch (Exception ex)
            {
                result.FailedCount += operations.Count;
                result.Success = false;
                result.Message = ex.Message;

                foreach (var op in operations)
                {
                    result.Details.Add(new ReadOperationResult
                    {
                        OperationType = op.OperationType,
                        TableName = op.TableName,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Metadata = op.Metadata
                    });
                }

                XTrace.WriteLine($"[ReadQueueExecutor] 推送到队列失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 按表名分组操作
        /// </summary>
        private static Dictionary<string, List<ReadOperation>> GroupByTable(List<ReadOperation> operations)
        {
            var groups = new Dictionary<string, List<ReadOperation>>(StringComparer.OrdinalIgnoreCase);
            foreach (var op in operations)
            {
                if (!groups.TryGetValue(op.TableName, out var list))
                {
                    list = new List<ReadOperation>();
                    groups[op.TableName] = list;
                }
                list.Add(op);
            }
            return groups;
        }

        /// <summary>
        /// 合并结果
        /// </summary>
        private static void MergeResult(ReadQueueResult target, ReadQueueResult source)
        {
            target.QueuedCount += source.QueuedCount;
            target.FailedCount += source.FailedCount;
            target.Details.AddRange(source.Details);

            if (!source.Success)
            {
                target.Success = false;
            }
        }

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize(); // 使用默认连接
            }
        }
    }
}
#endif
