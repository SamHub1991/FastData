using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FastData.Sharding;
using FastData.Sharding.Strategies;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 分表功能完整演示控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ShardingController : ControllerBase
    {
        private readonly string _connectionString;

        public ShardingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServer")
                ?? "server=.;database=FastDataDemo;uid=sa;pwd=YourPassword123";
        }

        /// <summary>
        /// 初始化分表测试环境
        /// </summary>
        [HttpPost("init")]
        public IActionResult InitShardingEnvironment()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 创建源表
                    var sql = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserLog' AND xtype='U')
                        CREATE TABLE UserLog (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId NVARCHAR(50),
                            Message NVARCHAR(500),
                            Level NVARCHAR(20),
                            CreateTime DATETIME DEFAULT GETDATE()
                        );

                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderData' AND xtype='U')
                        CREATE TABLE OrderData (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            OrderNo NVARCHAR(50),
                            CustomerId NVARCHAR(50),
                            Amount DECIMAL(18,2),
                            Status NVARCHAR(20),
                            Region NVARCHAR(50),
                            CreateTime DATETIME DEFAULT GETDATE()
                        );
                    ";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { Message = "Sharding environment initialized successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 插入大量测试数据
        /// </summary>
        [HttpPost("insert-data")]
        public IActionResult InsertBulkData([FromQuery] int logCount = 10000, [FromQuery] int orderCount = 5000)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 插入 UserLog 数据
                    using (var bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = "UserLog";
                        bulkCopy.BatchSize = 1000;

                        var dataTable = new DataTable();
                        dataTable.Columns.Add("UserId", typeof(string));
                        dataTable.Columns.Add("Message", typeof(string));
                        dataTable.Columns.Add("Level", typeof(string));
                        dataTable.Columns.Add("CreateTime", typeof(DateTime));

                        var random = new Random();
                        var levels = new[] { "Info", "Warning", "Error", "Debug" };
                        var users = Enumerable.Range(1, 100).Select(i => $"User{i:D3}").ToArray();

                        for (int i = 0; i < logCount; i++)
                        {
                            var row = dataTable.NewRow();
                            row["UserId"] = users[random.Next(users.Length)];
                            row["Message"] = $"Log message {i}";
                            row["Level"] = levels[random.Next(levels.Length)];
                            row["CreateTime"] = DateTime.Now.AddDays(-random.Next(365));
                            dataTable.Rows.Add(row);
                        }

                        bulkCopy.WriteToServer(dataTable);
                    }

                    // 插入 OrderData 数据
                    using (var bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = "OrderData";
                        bulkCopy.BatchSize = 1000;

                        var dataTable = new DataTable();
                        dataTable.Columns.Add("OrderNo", typeof(string));
                        dataTable.Columns.Add("CustomerId", typeof(string));
                        dataTable.Columns.Add("Amount", typeof(decimal));
                        dataTable.Columns.Add("Status", typeof(string));
                        dataTable.Columns.Add("Region", typeof(string));
                        dataTable.Columns.Add("CreateTime", typeof(DateTime));

                        var random = new Random();
                        var statuses = new[] { "Pending", "Processing", "Completed", "Cancelled" };
                        var regions = new[] { "Beijing", "Shanghai", "Guangzhou", "Shenzhen", "Hangzhou" };

                        for (int i = 0; i < orderCount; i++)
                        {
                            var row = dataTable.NewRow();
                            row["OrderNo"] = $"ORD{i:D8}";
                            row["CustomerId"] = $"CUST{random.Next(1000):D4}";
                            row["Amount"] = Math.Round((decimal)(random.NextDouble() * 10000), 2);
                            row["Status"] = statuses[random.Next(statuses.Length)];
                            row["Region"] = regions[random.Next(regions.Length)];
                            row["CreateTime"] = DateTime.Now.AddDays(-random.Next(365));
                            dataTable.Rows.Add(row);
                        }

                        bulkCopy.WriteToServer(dataTable);
                    }
                }

                stopwatch.Stop();

                return Ok(new
                {
                    Message = "Bulk data inserted successfully",
                    LogCount = logCount,
                    OrderCount = orderCount,
                    ElapsedSeconds = stopwatch.Elapsed.TotalSeconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 时间分表配置和查询
        /// </summary>
        [HttpPost("time/configure")]
        public IActionResult ConfigureTimeSharding([FromBody] TimeShardingRequest request)
        {
            var config = new ShardingConfig
            {
                BaseTableName = request.TableName ?? "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = request.TimeField ?? "CreateTime",
                    Granularity = Enum.Parse<TimeGranularity>(request.Granularity ?? "Month"),
                    StartTime = request.StartTime ?? new DateTime(2025, 1, 1)
                }
            };

            ShardingManager.Configure<UserLog>(config);

            return Ok(new
            {
                Message = "Time sharding configured",
                Config = new
                {
                    BaseTableName = config.BaseTableName,
                    TimeField = config.TimeConfig.TimeField,
                    Granularity = config.TimeConfig.Granularity.ToString(),
                    StartTime = config.TimeConfig.StartTime
                }
            });
        }

        /// <summary>
        /// 时间分表查询
        /// </summary>
        [HttpGet("time/query")]
        public IActionResult TimeShardingQuery(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", startTime },
                { "CreateTime_End", endTime }
            };

            var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);

            // 查询每个分表的数据
            var results = new Dictionary<string, int>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var sql = $"SELECT COUNT(*) FROM [{tableName}]";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            var count = (int)cmd.ExecuteScalar();
                            results[tableName] = count;
                        }
                    }
                    catch
                    {
                        results[tableName] = -1; // 表不存在
                    }
                }
            }

            return Ok(new
            {
                StartTime = startTime,
                EndTime = endTime,
                TableNames = tableNames,
                TableCounts = results,
                TotalRecords = results.Values.Where(v => v > 0).Sum()
            });
        }

        /// <summary>
        /// 哈希分表配置和查询
        /// </summary>
        [HttpPost("hash/configure")]
        public IActionResult ConfigureHashSharding([FromBody] HashShardingRequest request)
        {
            var config = new ShardingConfig
            {
                BaseTableName = request.TableName ?? "UserLog",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = request.HashField ?? "UserId",
                    ShardCount = request.ShardCount ?? 4
                }
            };

            ShardingManager.Configure<UserLog>(config);

            return Ok(new
            {
                Message = "Hash sharding configured",
                Config = new
                {
                    BaseTableName = config.BaseTableName,
                    HashField = config.HashConfig.HashField,
                    ShardCount = config.HashConfig.ShardCount
                }
            });
        }

        /// <summary>
        /// 哈希分表查询特定用户
        /// </summary>
        [HttpGet("hash/query")]
        public IActionResult HashShardingQuery([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            var queryParams = new Dictionary<string, object>
            {
                { "UserId", userId }
            };

            var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);

            // 查询该用户的数据
            var results = new Dictionary<string, int>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var sql = $"SELECT COUNT(*) FROM [{tableName}] WHERE UserId = @UserId";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            var count = (int)cmd.ExecuteScalar();
                            results[tableName] = count;
                        }
                    }
                    catch
                    {
                        results[tableName] = -1;
                    }
                }
            }

            return Ok(new
            {
                UserId = userId,
                TableNames = tableNames,
                TableCounts = results,
                TotalRecords = results.Values.Where(v => v > 0).Sum()
            });
        }

        /// <summary>
        /// 列表分表配置和查询
        /// </summary>
        [HttpPost("list/configure")]
        public IActionResult ConfigureListSharding([FromBody] ListShardingRequest request)
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
                BaseTableName = request.TableName ?? "OrderData",
                ShardingType = ShardingType.List,
                ListConfig = new ListShardingConfig
                {
                    ListField = request.ListField ?? "Status",
                    ValueMapping = valueMapping
                }
            };

            ShardingManager.Configure<OrderData>(config);

            return Ok(new
            {
                Message = "List sharding configured",
                Config = new
                {
                    BaseTableName = config.BaseTableName,
                    ListField = config.ListConfig.ListField,
                    ValueMapping = valueMapping
                }
            });
        }

        /// <summary>
        /// 列表分表查询
        /// </summary>
        [HttpGet("list/query")]
        public IActionResult ListShardingQuery([FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Status is required");
            }

            var queryParams = new Dictionary<string, object>
            {
                { "Status", status }
            };

            var tableNames = ShardingManager.GetTableNames<OrderData>(queryParams);

            // 查询该状态的订单
            var results = new Dictionary<string, int>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var sql = $"SELECT COUNT(*) FROM [{tableName}] WHERE Status = @Status";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Status", status);
                            var count = (int)cmd.ExecuteScalar();
                            results[tableName] = count;
                        }
                    }
                    catch
                    {
                        results[tableName] = -1;
                    }
                }
            }

            return Ok(new
            {
                Status = status,
                TableNames = tableNames,
                TableCounts = results,
                TotalRecords = results.Values.Where(v => v > 0).Sum()
            });
        }

        /// <summary>
        /// 查询频率分表配置
        /// </summary>
        [HttpPost("frequency/configure")]
        public IActionResult ConfigureFrequencySharding([FromBody] FrequencyShardingRequest request)
        {
            var config = new ShardingConfig
            {
                BaseTableName = request.TableName ?? "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = request.Field ?? "UserId",
                    HotThreshold = request.HotThreshold ?? 50,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.ByHash,
                    ColdShardCount = request.ColdShardCount ?? 4
                }
            };

            ShardingManager.Configure<UserLog>(config);

            return Ok(new
            {
                Message = "Query frequency sharding configured",
                Config = new
                {
                    BaseTableName = config.BaseTableName,
                    Field = config.FrequencyConfig.Field,
                    HotThreshold = config.FrequencyConfig.HotThreshold,
                    ColdShardCount = config.FrequencyConfig.ColdShardCount
                }
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
        /// 批量记录查询频率（模拟真实场景）
        /// </summary>
        [HttpPost("frequency/simulate")]
        public IActionResult SimulateQueryFrequency([FromQuery] int queryCount = 1000)
        {
            var random = new Random();
            var users = Enumerable.Range(1, 100).Select(i => $"User{i:D3}").ToArray();

            for (int i = 0; i < queryCount; i++)
            {
                var user = users[random.Next(users.Length)];
                QueryFrequencyShardingStrategy.RecordQuery("UserId", user);
            }

            var hotUsers = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 50);

            return Ok(new
            {
                Message = "Query frequency simulated",
                QueryCount = queryCount,
                HotUsersCount = hotUsers.Count,
                HotUsers = hotUsers.Take(10).ToList()
            });
        }

        /// <summary>
        /// 获取热数据列表
        /// </summary>
        [HttpGet("frequency/hot")]
        public IActionResult GetHotDataValues(
            [FromQuery] string field,
            [FromQuery] long threshold = 50)
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
        /// 分表数据同步
        /// </summary>
        [HttpPost("sync")]
        public IActionResult SyncShardingData([FromBody] SyncRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var syncLog = new List<string>();

            try
            {
                // 配置分表
                ShardingConfig config;
                switch (request.ShardingType?.ToLower())
                {
                    case "time":
                        config = new ShardingConfig
                        {
                            BaseTableName = request.SourceTable ?? "UserLog",
                            ShardingType = ShardingType.Time,
                            TimeConfig = new TimeShardingConfig
                            {
                                TimeField = request.TimeField ?? "CreateTime",
                                Granularity = TimeGranularity.Month
                            }
                        };
                        break;

                    case "hash":
                        config = new ShardingConfig
                        {
                            BaseTableName = request.SourceTable ?? "UserLog",
                            ShardingType = ShardingType.Hash,
                            HashConfig = new HashShardingConfig
                            {
                                HashField = request.HashField ?? "UserId",
                                ShardCount = request.ShardCount ?? 4
                            }
                        };
                        break;

                    case "list":
                        config = new ShardingConfig
                        {
                            BaseTableName = request.SourceTable ?? "OrderData",
                            ShardingType = ShardingType.List,
                            ListConfig = new ListShardingConfig
                            {
                                ListField = request.ListField ?? "Status",
                                ValueMapping = new Dictionary<string, string>
                                {
                                    { "Pending", "pending" },
                                    { "Processing", "processing" },
                                    { "Completed", "completed" },
                                    { "Cancelled", "cancelled" }
                                }
                            }
                        };
                        break;

                    default:
                        return BadRequest($"Unsupported sharding type: {request.ShardingType}");
                }

                ShardingManager.Configure<OrderData>(config);

                // 创建分表
                var tableNames = ShardingManager.GetAllTableNames<OrderData>();
                syncLog.Add($"Configured {tableNames.Count} sharding tables");

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 创建分表结构
                    foreach (var tableName in tableNames)
                    {
                        var createSql = $@"
                            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                            CREATE TABLE [{tableName}] (
                                Id INT,
                                OrderNo NVARCHAR(50),
                                CustomerId NVARCHAR(50),
                                Amount DECIMAL(18,2),
                                Status NVARCHAR(20),
                                Region NVARCHAR(50),
                                CreateTime DATETIME
                            );
                        ";

                        using (var cmd = new SqlCommand(createSql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    syncLog.Add("Sharding tables created");

                    // 从源表读取数据并插入到分表
                    var selectSql = $"SELECT Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime FROM [{request.SourceTable ?? "OrderData"}]";
                    int totalRecords = 0;
                    int syncedRecords = 0;

                    using (var cmd = new SqlCommand(selectSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            totalRecords++;
                            var order = new OrderData
                            {
                                Id = reader.GetInt32(0),
                                OrderNo = reader.GetString(1),
                                CustomerId = reader.GetString(2),
                                Amount = reader.GetDecimal(3),
                                Status = reader.GetString(4),
                                Region = reader.GetString(5),
                                CreateTime = reader.GetDateTime(6)
                            };

                            var targetTable = ShardingManager.GetTableName(order);
                            InsertOrderToShardingTable(conn, targetTable, order);
                            syncedRecords++;
                        }
                    }

                    syncLog.Add($"Synced {syncedRecords}/{totalRecords} records");
                }

                stopwatch.Stop();

                return Ok(new
                {
                    Message = "Sharding data sync completed",
                    ShardingType = request.ShardingType,
                    TableNames = tableNames,
                    SyncLog = syncLog,
                    ElapsedSeconds = stopwatch.Elapsed.TotalSeconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    SyncLog = syncLog
                });
            }
        }

        /// <summary>
        /// 分表数据统计
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetShardingStats([FromQuery] string entityType = "userlog")
        {
            try
            {
                List<string> tableNames;
                switch (entityType.ToLower())
                {
                    case "userlog":
                        if (!ShardingManager.IsShardingEnabled<UserLog>())
                        {
                            return Ok(new { Message = "UserLog sharding not configured" });
                        }
                        tableNames = ShardingManager.GetAllTableNames<UserLog>();
                        break;

                    case "orderdata":
                        if (!ShardingManager.IsShardingEnabled<OrderData>())
                        {
                            return Ok(new { Message = "OrderData sharding not configured" });
                        }
                        tableNames = ShardingManager.GetAllTableNames<OrderData>();
                        break;

                    default:
                        return BadRequest($"Unknown entity type: {entityType}");
                }

                var stats = new Dictionary<string, int>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    foreach (var tableName in tableNames)
                    {
                        try
                        {
                            var sql = $"SELECT COUNT(*) FROM [{tableName}]";
                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                stats[tableName] = (int)cmd.ExecuteScalar();
                            }
                        }
                        catch
                        {
                            stats[tableName] = -1;
                        }
                    }
                }

                return Ok(new
                {
                    EntityType = entityType,
                    TableNames = tableNames,
                    TableStats = stats,
                    TotalRecords = stats.Values.Where(v => v > 0).Sum()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 插入订单到分表
        /// </summary>
        private void InsertOrderToShardingTable(SqlConnection conn, string tableName, OrderData order)
        {
            var sql = $@"
                INSERT INTO [{tableName}] (Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime)
                VALUES (@Id, @OrderNo, @CustomerId, @Amount, @Status, @Region, @CreateTime);
            ";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", order.Id);
                cmd.Parameters.AddWithValue("@OrderNo", order.OrderNo);
                cmd.Parameters.AddWithValue("@CustomerId", order.CustomerId);
                cmd.Parameters.AddWithValue("@Amount", order.Amount);
                cmd.Parameters.AddWithValue("@Status", order.Status);
                cmd.Parameters.AddWithValue("@Region", order.Region);
                cmd.Parameters.AddWithValue("@CreateTime", order.CreateTime);
                cmd.ExecuteNonQuery();
            }
        }
    }

    #region Request Models

    public class TimeShardingRequest
    {
        public string TableName { get; set; }
        public string TimeField { get; set; }
        public string Granularity { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public class HashShardingRequest
    {
        public string TableName { get; set; }
        public string HashField { get; set; }
        public int? ShardCount { get; set; }
    }

    public class ListShardingRequest
    {
        public string TableName { get; set; }
        public string ListField { get; set; }
    }

    public class FrequencyShardingRequest
    {
        public string TableName { get; set; }
        public string Field { get; set; }
        public long? HotThreshold { get; set; }
        public int? ColdShardCount { get; set; }
    }

    public class RecordFrequencyRequest
    {
        public string Field { get; set; }
        public string FieldValue { get; set; }
    }

    public class SyncRequest
    {
        public string SourceTable { get; set; }
        public string ShardingType { get; set; }
        public string TimeField { get; set; }
        public string HashField { get; set; }
        public int? ShardCount { get; set; }
        public string ListField { get; set; }
    }

    #endregion

    #region Entity Models

    public class UserLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class OrderData
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public DateTime CreateTime { get; set; }
    }

    #endregion
}
