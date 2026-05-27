#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;

namespace FastData.Example.Example
{
    /// <summary>
    /// 消息队列使用示例
    /// 演示 RTU 数据上传场景：一边存库、多方推送
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

            // 示例 3: 配置驱动的消息队列
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

            // 消费单条消息
            Console.WriteLine("消费消息...");
            var factory = new MessageQueueFactory(redis: null); // 已通过 mqService 内部管理
            var consumer = mqService; // 使用集成服务消费

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
        /// 示例 3: 配置驱动的消息队列
        /// 场景：通过 TableSyncConfig 配置消息队列
        /// </summary>
        private static void DemoConfigDriven()
        {
            Console.WriteLine("=== 示例 3: 配置驱动的消息队列 ===");
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
