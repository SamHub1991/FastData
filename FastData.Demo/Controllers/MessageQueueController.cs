using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastData.Config;
using FastData.Queue;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 消息队列 Demo
    /// 演示 FastData 的消息队列功能
    /// </summary>
    [ApiController]
    [Route("api/MessageQueue")]
    public class MessageQueueController : ControllerBase
    {
        private static MessageQueueIntegrationService _mqService;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取或初始化消息队列服务
        /// </summary>
        private MessageQueueIntegrationService GetMqService()
        {
            if (_mqService == null)
            {
                lock (_lock)
                {
                    if (_mqService == null)
                    {
                        var redisConfig = FastDataConfig.GetRedisConfig();
                        var redis = new FullRedis
                        {
                            Server = redisConfig?.Server ?? "127.0.0.1:6379",
                            Db = redisConfig?.Db ?? 7,
                            Timeout = 15000
                        };
                        _mqService = new MessageQueueIntegrationService(redis);
                        WriteBehindExecutor.Initialize(redis);
                        ReadQueueExecutor.Initialize(redis);
                    }
                }
            }
            return _mqService;
        }

        /// <summary>
        /// 场景 1: RTU 削峰
        /// 大量 RTU 数据上传时，通过队列缓冲，异步批量写入数据库
        /// </summary>
        [HttpPost("rtu-peak-shaving")]
        public IActionResult RtuPeakShaving([FromBody] List<SensorDataRequest> dataList)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();
                var topic = "rtu:sensor";

                // 配置表级别队列（启用降级）
                FastWrite.ConfigureQueue<SensorDataRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    EnableFallback = true,
                    EnableAutoRecovery = true,
                    Topic = topic
                });

                // 使用链式 API 写入（带元数据）
                var writeResult = FastWrite.QueueBuilder()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"source", "RTU-DataSync"},
                        {"batchId", $"BATCH-{DateTime.Now:yyyyMMddHHmmss}"},
                        {"operator", "system"}
                    })
                    .AddRange(dataList)
                    .Execute();

                result["success"] = writeResult.Success;
                result["directWriteCount"] = writeResult.DirectWriteCount;
                result["queuedCount"] = writeResult.QueuedCount;
                result["failedCount"] = writeResult.FailedCount;
                result["fallbackOccurred"] = writeResult.FallbackOccurred;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 2: 多方推送
        /// 使用 Stream 将数据推送到多个消费组，每个组独立消费
        /// </summary>
        [HttpPost("multi-group-push")]
        public async Task<IActionResult> MultiGroupPush([FromBody] List<RealtimeDataRequest> dataList)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();
                var topic = "rtu:realtime";

                // 配置 Stream 队列
                FastWrite.ConfigureQueue<RealtimeDataRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.Stream,
                    Topic = topic
                });

                // 发布数据到 Stream
                var count = await mqService.PublishDataAsync(topic, dataList, MessageQueueType.Stream);

                // 启动多个消费组
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await mqService.StartMultiGroupConsumerAsync(
                    topic,
                    new[] { "db-writer", "alert-system", "analytics" },
                    new Func<RealtimeDataRequest, Task>[]
                    {
                        // 消费组 1: 数据库存储
                        async (data) =>
                        {
                            Console.WriteLine($"[DB Writer] 存储数据: {data.DeviceId} - {data.Value}");
                            await Task.CompletedTask;
                        },
                        // 消费组 2: 告警系统
                        async (data) =>
                        {
                            if (data.Value > 100)
                            {
                                Console.WriteLine($"[Alert] 高值告警: {data.DeviceId} - {data.Value}");
                            }
                            await Task.CompletedTask;
                        },
                        // 消费组 3: 数据分析
                        async (data) =>
                        {
                            Console.WriteLine($"[Analytics] 分析数据: {data.DeviceId} - {data.Value}");
                            await Task.CompletedTask;
                        }
                    },
                    cts.Token,
                    concurrency: 1);

                result["success"] = true;
                result["publishedCount"] = count;
                result["consumerGroups"] = new[] { "db-writer", "alert-system", "analytics" };
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 3: 数据库降级
        /// 数据库异常时自动降级到队列，恢复后自动刷写
        /// </summary>
        [HttpPost("db-fallback")]
        public IActionResult DbFallback([FromBody] List<OrderRequest> orderList)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();

                // 配置降级模式
                FastWrite.ConfigureQueue<OrderRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    EnableFallback = true,        // 启用降级
                    EnableAutoRecovery = true,    // 启用自动恢复
                    RecoveryIntervalSeconds = 30, // 恢复检查间隔
                    Topic = "orders"
                });

                // 启动后台刷写服务
                var flushService = new QueueFlushService(mqService);
                flushService.Start();

                // 写入操作（自动降级）
                var writeResult = FastWrite.QueueBuilder()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"source", "order-service"},
                        {"operator", "user-1001"}
                    })
                    .AddRange(orderList)
                    .Execute();

                // 获取队列状态
                var status = flushService.GetStatus();

                result["success"] = writeResult.Success;
                result["directWriteCount"] = writeResult.DirectWriteCount;
                result["queuedCount"] = writeResult.QueuedCount;
                result["failedCount"] = writeResult.FailedCount;
                result["fallbackOccurred"] = writeResult.FallbackOccurred;
                result["queueStatus"] = status;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 4: 查询审计
        /// 将查询请求推送到队列，实现异步查询或查询审计
        /// </summary>
        [HttpPost("query-audit")]
        public IActionResult QueryAudit([FromBody] QueryAuditRequest request)
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 配置查询队列
                FastRead.ConfigureQueue<OrderRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    Topic = "order-queries"
                });

                // 推送查询请求
                var readResult = FastRead.QueueBuilder<OrderRequest>()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"requestId", Guid.NewGuid().ToString()},
                        {"source", request.Source ?? "api"},
                        {"userId", request.UserId ?? 0}
                    })
                    .QueryList(metadata: new Dictionary<string, object> { {"queryType", "list-orders"} })
                    .QueryCount(metadata: new Dictionary<string, object> { {"queryType", "count-orders"} })
                    .QueryPaging(request.PageIndex, request.PageSize,
                        metadata: new Dictionary<string, object> { {"queryType", "paged-orders"} })
                    .Execute();

                result["success"] = readResult.Success;
                result["queuedCount"] = readResult.QueuedCount;
                result["failedCount"] = readResult.FailedCount;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 5: 批量写入
        /// 使用链式 API 批量写入多种实体
        /// </summary>
        [HttpPost("batch-write")]
        public IActionResult BatchWrite([FromBody] BatchWriteRequest request)
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 配置多个表的队列
                FastWrite.ConfigureQueue<SensorDataRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    Topic = "sensor-data"
                });

                FastWrite.ConfigureQueue<OrderRequest>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    Topic = "orders"
                });

                // 使用链式 API 批量写入
                var builder = FastWrite.QueueBuilder()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"batchId", Guid.NewGuid().ToString()},
                        {"source", "batch-service"}
                    });

                // 添加传感器数据
                if (request.SensorData != null)
                {
                    builder.AddRange(request.SensorData);
                }

                // 添加订单数据
                if (request.Orders != null)
                {
                    builder.AddRange(request.Orders);
                }

                var writeResult = builder.Execute();

                result["success"] = writeResult.Success;
                result["directWriteCount"] = writeResult.DirectWriteCount;
                result["queuedCount"] = writeResult.QueuedCount;
                result["failedCount"] = writeResult.FailedCount;
                result["operationCount"] = writeResult.DirectWriteCount + writeResult.QueuedCount + writeResult.FailedCount;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 6: 队列状态查询
        /// 查询队列的运行状态
        /// </summary>
        [HttpGet("status/{topic}")]
        public IActionResult GetQueueStatus(string topic, [FromQuery] string queueType = "ReliableQueue")
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();
                var type = queueType == "Stream" ? MessageQueueType.Stream : MessageQueueType.ReliableQueue;

                var status = mqService.GetQueueStatus(topic, type);

                result["success"] = true;
                result["topic"] = topic;
                result["queueType"] = queueType;
                result["status"] = status;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 7: 发布单条消息
        /// 向指定主题发布单条消息
        /// </summary>
        [HttpPost("publish")]
        public IActionResult Publish([FromBody] PublishRequest request)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();
                var queueType = request.QueueType == "Stream"
                    ? MessageQueueType.Stream
                    : MessageQueueType.ReliableQueue;

                var success = mqService.PublishSingle(request.Topic, request.Data, queueType);

                result["success"] = success;
                result["topic"] = request.Topic;
                result["queueType"] = queueType.ToString();
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 8: 消费消息
        /// 从指定主题消费消息
        /// </summary>
        [HttpPost("consume")]
        public async Task<IActionResult> Consume([FromBody] ConsumeRequest request)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var mqService = GetMqService();
                var queueType = request.QueueType == "Stream"
                    ? MessageQueueType.Stream
                    : MessageQueueType.ReliableQueue;

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(request.TimeoutSeconds));
                var messages = new List<object>();

                await mqService.StartConsumerAsync<string>(
                    request.Topic,
                    async (message) =>
                    {
                        messages.Add(message);
                        await Task.CompletedTask;
                    },
                    cts.Token,
                    queueType,
                    concurrency: 1);

                result["success"] = true;
                result["topic"] = request.Topic;
                result["consumedCount"] = messages.Count;
                result["messages"] = messages;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 9: 配置管理
        /// 查看和管理队列配置
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var configs = WriteBehindRegistry.GetAllConfigs();

                result["success"] = true;
                result["registeredTables"] = configs.Count;
                result["configs"] = configs;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 场景 10: 检查表是否启用队列
        /// </summary>
        [HttpGet("enabled/{tableName}")]
        public IActionResult IsQueueEnabled(string tableName)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var isEnabled = WriteBehindRegistry.IsQueueEnabled(tableName);
                var config = WriteBehindRegistry.GetConfig(tableName);

                result["success"] = true;
                result["tableName"] = tableName;
                result["isEnabled"] = isEnabled;
                result["config"] = config;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }
    }

    #region 请求模型

    /// <summary>
    /// 传感器数据请求
    /// </summary>
    public class SensorDataRequest
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
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 实时数据请求
    /// </summary>
    public class RealtimeDataRequest
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 数据值
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 订单请求
    /// </summary>
    public class OrderRequest
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 查询审计请求
    /// </summary>
    public class QueryAuditRequest
    {
        /// <summary>
        /// 来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// 批量写入请求
    /// </summary>
    public class BatchWriteRequest
    {
        /// <summary>
        /// 传感器数据
        /// </summary>
        public List<SensorDataRequest> SensorData { get; set; }

        /// <summary>
        /// 订单数据
        /// </summary>
        public List<OrderRequest> Orders { get; set; }
    }

    /// <summary>
    /// 发布请求
    /// </summary>
    public class PublishRequest
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 队列类型
        /// </summary>
        public string QueueType { get; set; } = "ReliableQueue";
    }

    /// <summary>
    /// 消费请求
    /// </summary>
    public class ConsumeRequest
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 队列类型
        /// </summary>
        public string QueueType { get; set; } = "ReliableQueue";

        /// <summary>
        /// 超时秒数
        /// </summary>
        public int TimeoutSeconds { get; set; } = 5;
    }

    #endregion
}
