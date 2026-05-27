#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastData.Example.Model;
using FastData.Queue;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;

namespace FastData.Example.Example
{
    /// <summary>
    /// 消息队列使用示例
    /// 演示 RTU 数据上传场景：一边存库、多方推送
    /// 演示 FastWrite/FastRead 链式 API 和扩展元数据
    /// </summary>
    public static class MessageQueueExample
    {
        /// <summary>
        /// 运行所有消息队列示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- 消息队列示例 ---");
            Console.WriteLine();

            // 初始化 Redis
            var redis = new FullRedis
            {
                Server = "127.0.0.1:6379",
                Db = 7,
                Timeout = 15000
            };

            var mqService = new MessageQueueIntegrationService(redis);

            // 示例 1: 可信队列（削峰场景）
            DemoReliableQueue(mqService);

            // 示例 2: Stream 多消费组（多方推送）
            DemoStreamMultiGroup(mqService);

            // 示例 3: FastWrite 链式 API（写入后端队列）
            DemoFastWriteQueue(redis);

            // 示例 4: FastRead 链式 API（查询队列）
            DemoFastReadQueue(redis);

            // 示例 5: 配置驱动的消息队列
            DemoConfigDriven();

            mqService.Dispose();
        }

        /// <summary>
        /// 示例 1: 可信队列（削峰场景）
        /// 场景：RTU 数据上传 → 队列缓冲 → 批量写入数据库
        /// </summary>
        private static void DemoReliableQueue(MessageQueueIntegrationService mqService)
        {
            Console.WriteLine("=== 示例 1: 可信队列（削峰场景） ===");
            Console.WriteLine("场景：RTU 数据 → 队列缓冲 → 批量写入数据库");
            Console.WriteLine();

            var topic = "rtu:sensor";

            // 模拟 RTU 上传的数据
            var sensorData = new List<SensorData>
            {
                new SensorData { DeviceId = "RTU-001", Temperature = 25.5, Humidity = 60.2, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-001", Temperature = 25.6, Humidity = 60.3, Timestamp = DateTime.Now.AddSeconds(1) },
                new SensorData { DeviceId = "RTU-002", Temperature = 23.1, Humidity = 55.8, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-002", Temperature = 23.2, Humidity = 55.9, Timestamp = DateTime.Now.AddSeconds(1) },
                new SensorData { DeviceId = "RTU-003", Temperature = 28.7, Humidity = 65.1, Timestamp = DateTime.Now }
            };

            // 发布到可信队列
            Console.WriteLine($"发布 {sensorData.Count} 条数据到队列...");
            var count = mqService.PublishData(topic, sensorData, MessageQueueType.ReliableQueue);
            Console.WriteLine($"成功发布 {count} 条数据");

            Console.WriteLine("✓ 可信队列示例完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: Stream 多消费组（多方推送）
        /// 场景：RTU 数据 → Stream → 多个系统独立消费
        /// </summary>
        private static void DemoStreamMultiGroup(MessageQueueIntegrationService mqService)
        {
            Console.WriteLine("=== 示例 2: Stream 多消费组（多方推送） ===");
            Console.WriteLine("场景：RTU 数据 → Stream → [数据库存储, 告警系统, 数据分析]");
            Console.WriteLine();

            var topic = "rtu:realtime";

            // 模拟 RTU 上传的数据
            var sensorData = new List<SensorData>
            {
                new SensorData { DeviceId = "RTU-001", Temperature = 35.5, Humidity = 80.2, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-002", Temperature = 23.1, Humidity = 55.8, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-003", Temperature = 38.7, Humidity = 90.1, Timestamp = DateTime.Now }
            };

            // 发布到 Stream
            Console.WriteLine($"发布 {sensorData.Count} 条数据到 Stream...");
            var count = mqService.PublishData(topic, sensorData, MessageQueueType.Stream);
            Console.WriteLine($"成功发布 {count} 条数据");

            Console.WriteLine("✓ 多消费组示例完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: FastWrite 链式 API（写入后端队列）
        /// 场景：数据库异常自动降级到可信队列，恢复后自动刷写
        /// </summary>
        private static void DemoFastWriteQueue(FullRedis redis)
        {
            Console.WriteLine("=== 示例 3: FastWrite 链式 API（写入后端队列） ===");
            Console.WriteLine("场景：数据库异常自动降级到可信队列");
            Console.WriteLine();

            // 初始化写入后端执行器
            WriteBehindExecutor.Initialize(redis);

            // 1. 配置表级别的消息队列（启用降级）
            FastWrite.ConfigureQueue<User>(new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                EnableFallback = true,
                EnableAutoRecovery = true,
                Topic = "example:users"
            });

            Console.WriteLine("已配置 User 表启用可信队列（降级模式）");

            // 2. 使用链式 API 写入（带扩展元数据）
            var users = new List<User>
            {
                new User { Id = 201, UserName = "rtu_user_001", Email = "rtu001@example.com", IsActive = true, CreateTime = DateTime.Now },
                new User { Id = 202, UserName = "rtu_user_002", Email = "rtu002@example.com", IsActive = true, CreateTime = DateTime.Now },
                new User { Id = 203, UserName = "rtu_user_003", Email = "rtu003@example.com", IsActive = false, CreateTime = DateTime.Now }
            };

            Console.WriteLine($"使用 FastWrite.QueueBuilder() 写入 {users.Count} 个用户...");

            var result = FastWrite.QueueBuilder()
                .WithMetadata(new Dictionary<string, object>
                {
                    {"source", "RTU-DataSync"},
                    {"batchId", $"BATCH-{DateTime.Now:yyyyMMddHHmmss}"},
                    {"operator", "system"}
                })
                .Add(users[0])
                .Add(users[1], new Dictionary<string, object> { {"priority", "high"} })
                .Add(users[2])
                .Execute();

            // 3. 输出结果
            Console.WriteLine($"执行结果: Success={result.Success}");
            Console.WriteLine($"  直接写入数据库: {result.DirectWriteCount} 条");
            Console.WriteLine($"  写入队列（降级）: {result.QueuedCount} 条");
            Console.WriteLine($"  失败: {result.FailedCount} 条");
            Console.WriteLine($"  降级发生: {result.FallbackOccurred}");

            if (result.Details.Count > 0)
            {
                Console.WriteLine("  详细结果:");
                foreach (var detail in result.Details)
                {
                    Console.WriteLine($"    - {detail.TableName} {detail.OperationType}: Success={detail.Success}, UsedQueue={detail.UsedQueue}");
                }
            }

            Console.WriteLine("✓ FastWrite 链式 API 示例完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: FastRead 链式 API（查询队列）
        /// 场景：将查询请求推送到消息队列，实现异步查询或查询审计
        /// </summary>
        private static void DemoFastReadQueue(FullRedis redis)
        {
            Console.WriteLine("=== 示例 4: FastRead 链式 API（查询队列） ===");
            Console.WriteLine("场景：将查询请求推送到消息队列");
            Console.WriteLine();

            // 初始化读取队列执行器
            ReadQueueExecutor.Initialize(redis);

            // 1. 配置表级别的消息队列
            FastRead.ConfigureQueue<User>(new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "example:user-queries"
            });

            Console.WriteLine("已配置 User 表启用查询队列");

            // 2. 使用链式 API 推送查询请求（带扩展元数据）
            Console.WriteLine("使用 FastRead.QueueBuilder<User>() 推送查询请求...");

            var result = FastRead.QueueBuilder<User>()
                .WithMetadata(new Dictionary<string, object>
                {
                    {"requestId", Guid.NewGuid().ToString()},
                    {"source", "example-app"},
                    {"userId", 1001}
                })
                .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "active-users"} })
                .QueryCount(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "count-active"} })
                .QueryPaging(1, 10, u => u.IsActive, u => u.CreateTime, false, new Dictionary<string, object> { {"queryType", "paged-list"} })
                .Execute();

            // 3. 输出结果
            Console.WriteLine($"执行结果: Success={result.Success}");
            Console.WriteLine($"  推送到队列: {result.QueuedCount} 条");
            Console.WriteLine($"  失败: {result.FailedCount} 条");

            if (result.Details.Count > 0)
            {
                Console.WriteLine("  详细结果:");
                foreach (var detail in result.Details)
                {
                    Console.WriteLine($"    - {detail.TableName} {detail.OperationType}: Success={detail.Success}");
                    if (detail.Metadata != null && detail.Metadata.Count > 0)
                    {
                        foreach (var meta in detail.Metadata)
                        {
                            Console.WriteLine($"      Metadata: {meta.Key}={meta.Value}");
                        }
                    }
                }
            }

            Console.WriteLine("✓ FastRead 链式 API 示例完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 5: 配置驱动的消息队列
        /// 场景：通过 TableSyncConfig 配置消息队列
        /// </summary>
        private static void DemoConfigDriven()
        {
            Console.WriteLine("=== 示例 5: 配置驱动的消息队列 ===");
            Console.WriteLine("场景：通过 TableSyncConfig 配置消息队列");
            Console.WriteLine();

            // 配置可信队列（适合数据库存储）
            var dbConfig = new FastData.Tooling.Sync.TableSyncConfig
            {
                TableName = "sensor_data",
                EnableMessageQueue = true,
                MessageQueueType = FastData.Tooling.Sync.MessageQueueType.ReliableQueue,
                MessageQueueTopic = "rtu:sensor",
                ConsumerConcurrency = 8
            };

            // 配置 Stream 多消费组（适合多方推送）
            var pushConfig = new FastData.Tooling.Sync.TableSyncConfig
            {
                TableName = "realtime_data",
                EnableMessageQueue = true,
                MessageQueueType = FastData.Tooling.Sync.MessageQueueType.Stream,
                MessageQueueTopic = "rtu:realtime",
                ConsumerGroup = "default",
                ConsumerConcurrency = 4
            };

            Console.WriteLine($"表 {dbConfig.TableName}: 启用 {dbConfig.MessageQueueType} 队列, 主题: {dbConfig.MessageQueueTopic}");
            Console.WriteLine($"表 {pushConfig.TableName}: 启用 {pushConfig.MessageQueueType} 队列, 主题: {pushConfig.MessageQueueTopic}");
            Console.WriteLine();

            Console.WriteLine("配置说明：");
            Console.WriteLine("  - EnableMessageQueue: 是否启用消息队列");
            Console.WriteLine("  - MessageQueueType: ReliableQueue（削峰）或 Stream（多方推送）");
            Console.WriteLine("  - MessageQueueTopic: 队列主题名称（为空则自动使用表名）");
            Console.WriteLine("  - ConsumerGroup: 消费组名称（仅 Stream 模式有效）");
            Console.WriteLine("  - ConsumerConcurrency: 消费者并发线程数");
            Console.WriteLine();

            Console.WriteLine("API 说明：");
            Console.WriteLine("  FastWrite:");
            Console.WriteLine("    - FastWrite.QueueBuilder().Add(model).Execute() - 链式写入");
            Console.WriteLine("    - FastWrite.QueueBuilder().WithMetadata(dict).Add(model).Execute() - 带元数据");
            Console.WriteLine("    - FastWrite.ConfigureQueue<T>(config) - 配置表级别队列");
            Console.WriteLine("    - FastWrite.IsQueueEnabled<T>() - 检查是否启用队列");
            Console.WriteLine("  FastRead:");
            Console.WriteLine("    - FastRead.QueueBuilder<T>().QueryList().Execute() - 链式查询");
            Console.WriteLine("    - FastRead.QueueBuilder<T>().WithMetadata(dict).QueryList().Execute() - 带元数据");
            Console.WriteLine("    - FastRead.ConfigureQueue<T>(config) - 配置表级别队列");
            Console.WriteLine("    - FastRead.IsQueueEnabled<T>() - 检查是否启用队列");
            Console.WriteLine();

            Console.WriteLine("✓ 配置驱动示例完成");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 传感器数据模型
    /// </summary>
    public class SensorData
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 湿度
        /// </summary>
        public double Humidity { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
#endif
