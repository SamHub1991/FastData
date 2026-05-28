#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastData.Demo.Models;
using FastData.Queue;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;
using NewLife.Log;

namespace FastData.Demo.Services
{
    /// <summary>
    /// 消息队列示例服务
    /// 演示 RTU 数据上传场景：一边存库、多方推送
    /// 演示 FastWrite/FastRead 链式 API 和扩展元数据
    /// </summary>
    public class MessageQueueService : IDisposable
    {
        private readonly MessageQueueIntegrationService _mqService;
        private readonly CancellationTokenSource _cts;
        private bool _disposed;

        public MessageQueueIntegrationService MqService => _mqService;

        public MessageQueueService()
        {
            // 初始化 Redis 连接
            var redis = new FullRedis
            {
                Server = "127.0.0.1:6379",
                Db = 7,
                Timeout = 15000 // 消费端需要更长的超时时间
            };

            _mqService = new MessageQueueIntegrationService(redis);
            _cts = new CancellationTokenSource();

            // 初始化写入后端执行器
            WriteBehindExecutor.Initialize(redis);
            ReadQueueExecutor.Initialize(redis);
        }

        /// <summary>
        /// 示例 1: 使用可信队列存储数据（削峰）
        /// 场景：RTU 上传数据 → 队列缓冲 → 批量写入数据库
        /// </summary>
        public async Task<int> DemoReliableQueueAsync()
        {
            Console.WriteLine("=== 示例 1: 可信队列（削峰场景） ===");
            Console.WriteLine("场景：RTU 数据 → 队列缓冲 → 批量写入数据库");
            Console.WriteLine();

            var topic = "rtu:sensor";

            // 1. 模拟 RTU 上传的数据
            var sensorData = new List<SensorData>
            {
                new SensorData { DeviceId = "RTU-001", Temperature = 25.5, Humidity = 60.2, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-001", Temperature = 25.6, Humidity = 60.3, Timestamp = DateTime.Now.AddSeconds(1) },
                new SensorData { DeviceId = "RTU-002", Temperature = 23.1, Humidity = 55.8, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-002", Temperature = 23.2, Humidity = 55.9, Timestamp = DateTime.Now.AddSeconds(1) },
                new SensorData { DeviceId = "RTU-003", Temperature = 28.7, Humidity = 65.1, Timestamp = DateTime.Now }
            };

            // 2. 发布到可信队列
            Console.WriteLine($"发布 {sensorData.Count} 条数据到队列...");
            var count = await _mqService.PublishDataAsync(topic, sensorData, MessageQueueType.ReliableQueue);
            Console.WriteLine($"成功发布 {count} 条数据");

            // 3. 启动消费者（模拟数据库写入）
            Console.WriteLine("启动消费者，等待数据...");
            var processedCount = 0;

            await _mqService.StartConsumerAsync<SensorData>(
                topic,
                async (data) =>
                {
                    // 模拟写入数据库
                    await Task.Delay(10); // 模拟 DB 写入延迟
                    Console.WriteLine($"  [DB Consumer] 写入数据库: {data.DeviceId} - 温度:{data.Temperature}°C, 湿度:{data.Humidity}%");
                    Interlocked.Increment(ref processedCount);
                },
                _cts.Token,
                MessageQueueType.ReliableQueue,
                concurrency: 2);

            Console.WriteLine();
            return count;
        }

        /// <summary>
        /// 示例 2: 使用 Stream 多消费组（多方推送）
        /// 场景：RTU 数据 → Stream → 多个系统独立消费
        /// </summary>
        public async Task<int> DemoStreamMultiGroupAsync()
        {
            Console.WriteLine("=== 示例 2: Stream 多消费组（多方推送） ===");
            Console.WriteLine("场景：RTU 数据 → Stream → [数据库存储, 告警系统, 数据分析]");
            Console.WriteLine();

            var topic = "rtu:realtime";

            // 1. 模拟 RTU 上传的数据
            var sensorData = new List<SensorData>
            {
                new SensorData { DeviceId = "RTU-001", Temperature = 35.5, Humidity = 80.2, Timestamp = DateTime.Now }, // 高温
                new SensorData { DeviceId = "RTU-002", Temperature = 23.1, Humidity = 55.8, Timestamp = DateTime.Now },
                new SensorData { DeviceId = "RTU-003", Temperature = 38.7, Humidity = 90.1, Timestamp = DateTime.Now }  // 高温高湿
            };

            // 2. 发布到 Stream
            Console.WriteLine($"发布 {sensorData.Count} 条数据到 Stream...");
            var count = await _mqService.PublishDataAsync(topic, sensorData, MessageQueueType.Stream);

            // 3. 启动多个消费组
            var consumerGroups = new[] { "db-writer", "alert-system", "analytics" };
            var handlers = new Func<SensorData, Task>[]
            {
                // 消费组 1: 数据库存储
                async (data) =>
                {
                    await Task.Delay(10);
                    Console.WriteLine($"  [DB Writer] 存储数据: {data.DeviceId} - {data.Temperature}°C");
                },
                // 消费组 2: 告警系统
                async (data) =>
                {
                    if (data.Temperature > 30)
                    {
                        Console.WriteLine($"  [Alert] 高温告警: {data.DeviceId} - {data.Temperature}°C");
                    }
                },
                // 消费组 3: 数据分析
                async (data) =>
                {
                    Console.WriteLine($"  [Analytics] 分析数据: {data.DeviceId} - 温度:{data.Temperature}°C, 湿度:{data.Humidity}%");
                }
            };

            await _mqService.StartMultiGroupConsumerAsync(
                topic,
                consumerGroups,
                handlers,
                _cts.Token,
                concurrency: 1);

            Console.WriteLine();
            return count;
        }

        /// <summary>
        /// 示例 3: DataTable 消息队列
        /// 场景：数据库查询结果 → 消息队列 → 异步处理
        /// </summary>
        public async Task<int> DemoDataTableQueueAsync()
        {
            Console.WriteLine("=== 示例 3: DataTable 消息队列 ===");
            Console.WriteLine("场景：数据库查询结果 → 消息队列 → 异步处理");
            Console.WriteLine();

            var topic = "db:sync:users";

            // 1. 模拟数据库查询结果
            var table = new System.Data.DataTable("User");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("UserName", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Rows.Add(1, "张三", "zhangsan@example.com");
            table.Rows.Add(2, "李四", "lisi@example.com");
            table.Rows.Add(3, "王五", "wangwu@example.com");

            // 2. 发布到消息队列
            Console.WriteLine($"发布 {table.Rows.Count} 行数据到队列...");
            var count = _mqService.PublishDataTable(topic, table, MessageQueueType.ReliableQueue);

            // 3. 启动消费者
            await _mqService.StartDataTableConsumerAsync(
                topic,
                async (rowData) =>
                {
                    Console.WriteLine($"  [Consumer] 处理数据: Id={rowData["Id"]}, UserName={rowData["UserName"]}, Email={rowData["Email"]}");
                    await Task.CompletedTask;
                },
                _cts.Token,
                MessageQueueType.ReliableQueue,
                concurrency: 2);

            Console.WriteLine();
            return count;
        }

        /// <summary>
        /// 示例 4: FastWrite 链式 API（写入后端队列）
        /// 场景：数据库异常自动降级到可信队列，恢复后自动刷写
        /// </summary>
        public WriteBehindResult DemoFastWriteQueue()
        {
            Console.WriteLine("=== 示例 4: FastWrite 链式 API（写入后端队列） ===");
            Console.WriteLine("场景：数据库异常自动降级到可信队列");
            Console.WriteLine();

            // 1. 配置表级别的消息队列（启用降级）
            FastWrite.ConfigureQueue<AppUser>(new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                EnableFallback = true,
                EnableAutoRecovery = true,
                Topic = "demo:users"
            });

            Console.WriteLine("已配置 User 表启用可信队列（降级模式）");

            // 2. 使用链式 API 写入（带扩展元数据）
            var users = new List<AppUser>
            {
                new AppUser { Id = 101, UserName = "rtu_user_001", Email = "rtu001@example.com", Age = 25, IsActive = true, CreateTime = DateTime.Now },
                new AppUser { Id = 102, UserName = "rtu_user_002", Email = "rtu002@example.com", Age = 30, IsActive = true, CreateTime = DateTime.Now },
                new AppUser { Id = 103, UserName = "rtu_user_003", Email = "rtu003@example.com", Age = 28, IsActive = false, CreateTime = DateTime.Now }
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

            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// 示例 5: FastRead 链式 API（查询请求推送到队列）
        /// 场景：将查询请求推送到消息队列，实现异步查询或查询审计
        /// </summary>
        public ReadQueueResult DemoFastReadQueue()
        {
            Console.WriteLine("=== 示例 5: FastRead 链式 API（查询队列） ===");
            Console.WriteLine("场景：将查询请求推送到消息队列");
            Console.WriteLine();

            // 1. 配置表级别的消息队列
            FastRead.ConfigureQueue<AppUser>(new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "demo:user-queries"
            });

            Console.WriteLine("已配置 User 表启用查询队列");

            // 2. 使用链式 API 推送查询请求（带扩展元数据）
            Console.WriteLine("使用 FastRead.QueueBuilder<AppUser>() 推送查询请求...");

            var result = FastRead.QueueBuilder<AppUser>()
                .WithMetadata(new Dictionary<string, object>
                {
                    {"requestId", Guid.NewGuid().ToString()},
                    {"source", "web-ui"},
                    {"userId", 1001}
                })
                .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "active-users"} })
                .QueryCount(u => u.Age > 25, metadata: new Dictionary<string, object> { {"queryType", "age-filter"} })
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

            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// 示例 6: 配置驱动的消息队列（通过 TableSyncConfig）
        /// 场景：同步配置自动启用消息队列
        /// </summary>
        public void DemoConfigDrivenQueue()
        {
            Console.WriteLine("=== 示例 6: 配置驱动的消息队列 ===");
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
            Console.WriteLine($"表 {pushConfig.TableName}: 启用 {pushConfig.MessageQueueType} 队列, 主题: {pushConfig.MessageQueueTopic}, 消费组: {pushConfig.ConsumerGroup}");
            Console.WriteLine();
        }

        /// <summary>
        /// 获取队列状态
        /// </summary>
        public Dictionary<string, object> GetQueueStatus(string topic, MessageQueueType queueType)
        {
            return _mqService.GetQueueStatus(topic, queueType);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _mqService?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 传感器数据模型
    /// </summary>
    public class SensorData
    {
        public string DeviceId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
#endif
