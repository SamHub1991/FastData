using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FastData.Sharding;
using FastData.Sharding.Strategies;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 分表功能演示控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ShardingController : ControllerBase
    {
        /// <summary>
        /// 获取分表配置信息
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                Message = "分表配置 API",
                Strategies = new[]
                {
                    new { Name = "Time", Description = "时间分表（日/周/月/季/年）" },
                    new { Name = "Hash", Description = "哈希分表（取模/一致性/CRC32）" },
                    new { Name = "List", Description = "列表分表（枚举值映射）" },
                    new { Name = "Composite", Description = "组合键分表（多字段组合）" },
                    new { Name = "QueryFrequency", Description = "查询频率分表（热数据/冷数据分离）" }
                }
            });
        }

        /// <summary>
        /// 时间分表查询示例
        /// </summary>
        [HttpGet("time")]
        public IActionResult TimeSharding(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string granularity = "Month")
        {
            // 配置时间分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = Enum.Parse<TimeGranularity>(granularity),
                    StartTime = startTime
                }
            };

            ShardingManager.Configure<UserLog>(config);

            // 查询参数
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", startTime },
                { "CreateTime_End", endTime }
            };

            var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);

            return Ok(new
            {
                StartTime = startTime,
                EndTime = endTime,
                Granularity = granularity,
                TableNames = tableNames,
                TableCount = tableNames.Count
            });
        }

        /// <summary>
        /// 哈希分表查询示例
        /// </summary>
        [HttpGet("hash")]
        public IActionResult HashSharding(
            [FromQuery] string hashField = "OrderNo",
            [FromQuery] int shardCount = 8)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = hashField,
                    ShardCount = shardCount
                }
            };

            ShardingManager.Configure<Order>(config);

            var allTables = ShardingManager.GetAllTableNames<Order>();

            return Ok(new
            {
                HashField = hashField,
                ShardCount = shardCount,
                TableNames = allTables
            });
        }

        /// <summary>
        /// 列表分表查询示例
        /// </summary>
        [HttpGet("list")]
        public IActionResult ListSharding(
            [FromQuery] string listField = "Status",
            [FromQuery] string value = null)
        {
            var valueMapping = new Dictionary<string, string>
            {
                { "Pending", "pending" },
                { "Processing", "processing" },
                { "Completed", "completed" },
                { "Cancelled", "cancelled" }
            };

            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.List,
                ListConfig = new ListShardingConfig
                {
                    ListField = listField,
                    ValueMapping = valueMapping
                }
            };

            ShardingManager.Configure<Order>(config);

            var queryParams = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(value))
            {
                queryParams[listField] = value;
            }

            var tableNames = ShardingManager.GetTableNames<Order>(queryParams);

            return Ok(new
            {
                ListField = listField,
                Value = value,
                TableNames = tableNames,
                AllMappings = valueMapping
            });
        }

        /// <summary>
        /// 组合键分表查询示例
        /// </summary>
        [HttpGet("composite")]
        public IActionResult CompositeSharding(
            [FromQuery] string region = null,
            [FromQuery] string customerType = null)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Composite,
                CompositeConfig = new CompositeShardingConfig
                {
                    CompositeFields = new List<string> { "Region", "CustomerType" },
                    UseHash = true,
                    ShardCount = 16
                }
            };

            ShardingManager.Configure<Order>(config);

            var queryParams = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(region))
                queryParams["Region"] = region;
            if (!string.IsNullOrEmpty(customerType))
                queryParams["CustomerType"] = customerType;

            var tableNames = ShardingManager.GetTableNames<Order>(queryParams);

            return Ok(new
            {
                Region = region,
                CustomerType = customerType,
                TableNames = tableNames
            });
        }

        /// <summary>
        /// 查询频率分表示例
        /// </summary>
        [HttpGet("frequency")]
        public IActionResult QueryFrequencySharding(
            [FromQuery] string userId,
            [FromQuery] long hotThreshold = 10)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = hotThreshold,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.ByHash,
                    ColdShardCount = 4
                }
            };

            ShardingManager.Configure<UserLog>(config);

            // 记录查询频率
            if (!string.IsNullOrEmpty(userId))
            {
                QueryFrequencyShardingStrategy.RecordQuery("UserId", userId);
            }

            var queryParams = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(userId))
                queryParams["UserId"] = userId;

            var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);
            var frequency = !string.IsNullOrEmpty(userId)
                ? QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", userId)
                : 0;

            return Ok(new
            {
                UserId = userId,
                QueryFrequency = frequency,
                IsHot = frequency >= hotThreshold,
                HotThreshold = hotThreshold,
                TableNames = tableNames
            });
        }

        /// <summary>
        /// 记录查询频率
        /// </summary>
        [HttpPost("frequency/record")]
        public IActionResult RecordQueryFrequency([FromBody] RecordFrequencyRequest request)
        {
            if (string.IsNullOrEmpty(request.Field) || string.IsNullOrEmpty(request.FieldValue))
            {
                return BadRequest("Field and FieldValue are required");
            }

            QueryFrequencyShardingStrategy.RecordQuery(request.Field, request.FieldValue);

            return Ok(new
            {
                Message = "Query frequency recorded",
                Field = request.Field,
                FieldValue = request.FieldValue,
                Frequency = QueryFrequencyShardingStrategy.GetQueryFrequency(request.Field, request.FieldValue)
            });
        }

        /// <summary>
        /// 获取热数据列表
        /// </summary>
        [HttpGet("frequency/hot")]
        public IActionResult GetHotDataValues(
            [FromQuery] string field,
            [FromQuery] long threshold = 10)
        {
            if (string.IsNullOrEmpty(field))
            {
                return BadRequest("Field is required");
            }

            var hotValues = QueryFrequencyShardingStrategy.GetHotDataValues(field, threshold);

            return Ok(new
            {
                Field = field,
                Threshold = threshold,
                HotValues = hotValues,
                Count = hotValues.Count
            });
        }

        /// <summary>
        /// 重置查询频率统计
        /// </summary>
        [HttpPost("frequency/reset")]
        public IActionResult ResetQueryFrequency([FromQuery] string field = null)
        {
            QueryFrequencyShardingStrategy.ResetFrequencyStats(field);

            return Ok(new
            {
                Message = "Query frequency stats reset",
                Field = field ?? "all"
            });
        }

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        [HttpGet("tables/{entityType}")]
        public IActionResult GetAllTableNames(string entityType)
        {
            List<string> tableNames;

            switch (entityType.ToLower())
            {
                case "userlog":
                    if (!ShardingManager.IsShardingEnabled<UserLog>())
                    {
                        return Ok(new { Message = "UserLog sharding not configured", TableNames = new List<string>() });
                    }
                    tableNames = ShardingManager.GetAllTableNames<UserLog>();
                    break;

                case "order":
                    if (!ShardingManager.IsShardingEnabled<Order>())
                    {
                        return Ok(new { Message = "Order sharding not configured", TableNames = new List<string>() });
                    }
                    tableNames = ShardingManager.GetAllTableNames<Order>();
                    break;

                default:
                    return BadRequest($"Unknown entity type: {entityType}");
            }

            return Ok(new
            {
                EntityType = entityType,
                TableNames = tableNames,
                Count = tableNames.Count
            });
        }
    }

    /// <summary>
    /// 记录查询频率请求
    /// </summary>
    public class RecordFrequencyRequest
    {
        public string Field { get; set; }
        public string FieldValue { get; set; }
    }

    // Demo 实体类
    public class UserLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime CreateTime { get; set; }
        public string UserId { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public string CustomerType { get; set; }
    }
}
