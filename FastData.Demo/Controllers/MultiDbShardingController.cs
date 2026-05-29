using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data.SQLite;
using FastData.Config;
using FastData.Sharding;
using FastData.Sharding.Strategies;

namespace FastData.Demo.Controllers
{
    [ApiController]
    [Route("api/multi-sharding")]
    public class MultiDbShardingController : ControllerBase
    {
        private static readonly Dictionary<string, Func<DbConnection>> ConnectionFactories = new()
        {
            ["SqlServer"] = () => new SqlConnection(FastDataConfig.GetConnectionString("SqlServer")),
            ["MySql"] = () => new MySqlConnection(FastDataConfig.GetConnectionString("MySql")),
            ["PostgreSql"] = () => new NpgsqlConnection(FastDataConfig.GetConnectionString("PostgreSql")),
            ["Sqlite"] = () => new SQLiteConnection(FastDataConfig.GetConnectionString("Sqlite"))
        };

        private DbConnection GetConnection(string dbType)
        {
            if (!ConnectionFactories.ContainsKey(dbType))
                throw new ArgumentException($"Unsupported database: {dbType}");
            return ConnectionFactories[dbType]();
        }

        private string QuoteName(string dbType, string name)
        {
            return dbType switch
            {
                "SqlServer" => $"[{name}]",
                "MySql" => $"`{name}`",
                "PostgreSql" => $"\"{name}\"",
                "Sqlite" => $"\"{name}\"",
                _ => name
            };
        }

        private string NowExpr(string dbType)
        {
            return dbType switch
            {
                "SqlServer" => "GETDATE()",
                "MySql" => "NOW()",
                "PostgreSql" => "NOW()",
                "Sqlite" => "datetime('now')",
                _ => "NOW()"
            };
        }

        /// <summary>
        /// 初始化分表源数据（创建 UserLog + OrderData 表并插入测试数据）
        /// </summary>
        [HttpPost("init")]
        public IActionResult Init([FromQuery] string db = "SqlServer",
                                   [FromQuery] int logCount = 5000,
                                   [FromQuery] int orderCount = 2000)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var conn = GetConnection(db);
                conn.Open();

                // 创建 UserLog 表
                var userLogSql = db switch
                {
                    "SqlServer" => @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserLog' AND xtype='U')
                        CREATE TABLE UserLog (Id INT IDENTITY(1,1) PRIMARY KEY, UserId NVARCHAR(50), Message NVARCHAR(500), Level NVARCHAR(20), CreateTime DATETIME DEFAULT GETDATE())",
                    "MySql" => @"CREATE TABLE IF NOT EXISTS UserLog (Id INT AUTO_INCREMENT PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    "PostgreSql" => "CREATE TABLE IF NOT EXISTS \"UserLog\" (Id SERIAL PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime TIMESTAMP DEFAULT NOW())",
                    "Sqlite" => @"CREATE TABLE IF NOT EXISTS UserLog (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId TEXT, Message TEXT, Level TEXT, CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    _ => throw new ArgumentException($"Unsupported: {db}")
                };

                // 创建 OrderData 表
                var orderDataSql = db switch
                {
                    "SqlServer" => @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderData' AND xtype='U')
                        CREATE TABLE OrderData (Id INT IDENTITY(1,1) PRIMARY KEY, OrderNo NVARCHAR(50), CustomerId NVARCHAR(50), Amount DECIMAL(18,2), Status NVARCHAR(20), Region NVARCHAR(50), CreateTime DATETIME DEFAULT GETDATE())",
                    "MySql" => @"CREATE TABLE IF NOT EXISTS OrderData (Id INT AUTO_INCREMENT PRIMARY KEY, OrderNo VARCHAR(50), CustomerId VARCHAR(50), Amount DECIMAL(18,2), Status VARCHAR(20), Region VARCHAR(50), CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    "PostgreSql" => "CREATE TABLE IF NOT EXISTS \"OrderData\" (Id SERIAL PRIMARY KEY, OrderNo VARCHAR(50), CustomerId VARCHAR(50), Amount DECIMAL(18,2), Status VARCHAR(20), Region VARCHAR(50), CreateTime TIMESTAMP DEFAULT NOW())",
                    "Sqlite" => @"CREATE TABLE IF NOT EXISTS OrderData (Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderNo TEXT, CustomerId TEXT, Amount REAL, Status TEXT, Region TEXT, CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    _ => throw new ArgumentException($"Unsupported: {db}")
                };

                Execute(conn, db, userLogSql);
                Execute(conn, db, orderDataSql);

                // 清空旧数据
                Execute(conn, db, $"DELETE FROM {QuoteName(db, "UserLog")}");
                Execute(conn, db, $"DELETE FROM {QuoteName(db, "OrderData")}");

                // 插入 UserLog 数据
                InsertBulkLogs(conn, db, logCount);
                // 插入 OrderData 数据
                InsertBulkOrders(conn, db, orderCount);

                sw.Stop();
                return Ok(new { Message = $"Init OK ({db})", UserLogCount = logCount, OrderCount = orderCount, Elapsed = sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace?.Length ?? 0)) });
            }
        }

        /// <summary>
        /// 时间分表配置 + 查询
        /// </summary>
        [HttpPost("time")]
        public IActionResult TimeSharding([FromQuery] string db = "SqlServer",
                                           [FromQuery] string granularity = "Month",
                                           [FromQuery] string startTime = "2025-06-01",
                                           [FromQuery] string endTime = "2026-06-01")
        {
            try
            {
                var config = new ShardingConfig
                {
                    BaseTableName = "UserLog",
                    ShardingType = ShardingType.Time,
                    TimeConfig = new TimeShardingConfig
                    {
                        TimeField = "CreateTime",
                        Granularity = Enum.Parse<TimeGranularity>(granularity),
                        StartTime = DateTime.Parse(startTime)
                    }
                };

                ShardingManager.Configure<UserLog>(config);

                var queryParams = new Dictionary<string, object>
                {
                    { "CreateTime_Start", DateTime.Parse(startTime) },
                    { "CreateTime_End", DateTime.Parse(endTime) }
                };
                var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);

                // 创建分表并查询各分表数据量
                var results = new Dictionary<string, int>();
                using var conn = GetConnection(db);
                conn.Open();

                // 创建分表（如果不存在）
                foreach (var tableName in tableNames)
                {
                    CreateTableIfNotExists(conn, db, tableName, "UserLog");
                }

                // 从源表按时间范围插入数据到分表
                var inserted = InsertDataToTimeShards(conn, db, tableNames, "UserLog", config.TimeConfig);

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var count = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, tableName)}");
                        results[tableName] = count;
                    }
                    catch
                    {
                        results[tableName] = -1;
                    }
                }

                return Ok(new
                {
                    Strategy = "Time",
                    Database = db,
                    Granularity = granularity,
                    TimeRange = $"{startTime} ~ {endTime}",
                    Tables = tableNames,
                    Counts = results,
                    Total = results.Values.Where(v => v > 0).Sum()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 哈希分表配置 + 查询
        /// </summary>
        [HttpPost("hash")]
        public IActionResult HashSharding([FromQuery] string db = "SqlServer",
                                           [FromQuery] string hashField = "UserId",
                                           [FromQuery] int shardCount = 4,
                                           [FromQuery] string queryUserId = "User001")
        {
            try
            {
                var config = new ShardingConfig
                {
                    BaseTableName = "UserLog",
                    ShardingType = ShardingType.Hash,
                    HashConfig = new HashShardingConfig
                    {
                        HashField = hashField,
                        ShardCount = shardCount
                    }
                };

                ShardingManager.Configure<UserLog>(config);

                var queryParams = new Dictionary<string, object> { { "UserId", queryUserId } };
                var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);
                var allTableNames = ShardingManager.GetAllTableNames<UserLog>();

                var results = new Dictionary<string, int>();
                using var conn = GetConnection(db);
                conn.Open();

                // 创建所有分表
                foreach (var tableName in allTableNames)
                {
                    CreateTableIfNotExists(conn, db, tableName, "UserLog");
                }
                // 插入数据到所有哈希分表
                InsertDataToHashShards(conn, db, allTableNames, "UserLog", hashField, shardCount);

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var count = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, tableName)}");
                        results[tableName] = count;
                    }
                    catch
                    {
                        results[tableName] = -1;
                    }
                }

                // 获取所有分表总数据量
                var allTables = ShardingManager.GetAllTableNames<UserLog>();
                var allCounts = new Dictionary<string, int>();
                foreach (var t in allTables)
                {
                    try
                    {
                        allCounts[t] = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, t)}");
                    }
                    catch
                    {
                        allCounts[t] = -1;
                    }
                }

                return Ok(new
                {
                    Strategy = "Hash",
                    Database = db,
                    HashField = hashField,
                    ShardCount = shardCount,
                    QueryUserId = queryUserId,
                    TargetTables = tableNames,
                    TargetCounts = results,
                    AllShardCounts = allCounts,
                    Total = allCounts.Values.Where(v => v > 0).Sum()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 列表分表配置 + 查询
        /// </summary>
        [HttpPost("list")]
        public IActionResult ListSharding([FromQuery] string db = "SqlServer",
                                           [FromQuery] string queryStatus = "Pending")
        {
            try
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
                    BaseTableName = "OrderData",
                    ShardingType = ShardingType.List,
                    ListConfig = new ListShardingConfig
                    {
                        ListField = "Status",
                        ValueMapping = valueMapping
                    }
                };

                ShardingManager.Configure<OrderData>(config);

                var queryParams = new Dictionary<string, object> { { "Status", queryStatus } };
                var tableNames = ShardingManager.GetTableNames<OrderData>(queryParams);
                var allTableNames = ShardingManager.GetAllTableNames<OrderData>();

                var results = new Dictionary<string, int>();
                using var conn = GetConnection(db);
                conn.Open();

                // 创建所有分表
                foreach (var tableName in allTableNames)
                {
                    CreateOrderTableIfNotExists(conn, db, tableName);
                }
                // 插入数据到所有列表分表
                InsertDataToListShards(conn, db, allTableNames, "OrderData", "Status", valueMapping);

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var count = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, tableName)}");
                        results[tableName] = count;
                    }
                    catch
                    {
                        results[tableName] = -1;
                    }
                }

                var allTables = ShardingManager.GetAllTableNames<OrderData>();
                var allCounts = new Dictionary<string, int>();
                foreach (var t in allTables)
                {
                    try
                    {
                        allCounts[t] = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, t)}");
                    }
                    catch
                    {
                        allCounts[t] = -1;
                    }
                }

                return Ok(new
                {
                    Strategy = "List",
                    Database = db,
                    ListField = "Status",
                    QueryStatus = queryStatus,
                    TargetTables = tableNames,
                    TargetCounts = results,
                    AllShardCounts = allCounts,
                    Total = allCounts.Values.Where(v => v > 0).Sum()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 频率分表配置 + 查询
        /// </summary>
        [HttpPost("frequency")]
        public IActionResult FrequencySharding([FromQuery] string db = "SqlServer",
                                                [FromQuery] string hotThreshold = "10",
                                                [FromQuery] int simulateCount = 20)
        {
            try
            {
                var config = new ShardingConfig
                {
                    BaseTableName = "OrderData",
                    ShardingType = ShardingType.QueryFrequency,
                    FrequencyConfig = new QueryFrequencyShardingConfig
                    {
                        Field = "CustomerId",
                        HotThreshold = long.Parse(hotThreshold),
                        HotSuffix = "_hot",
                        ColdSuffix = "_cold"
                    }
                };

                ShardingManager.Configure<OrderData>(config);

                // 模拟频率访问
                using var conn = GetConnection(db);
                conn.Open();

                // 创建分表
                var allTables = ShardingManager.GetAllTableNames<OrderData>();
                foreach (var tableName in allTables)
                {
                    CreateOrderTableIfNotExists(conn, db, tableName);
                }

                // 插入更多订单数据来模拟热点
                var insertCount = 0;
                var random = new Random(42);
                var hotCustomers = new[] { "CUST0001", "CUST0002", "CUST0003" };
                for (int i = 0; i < simulateCount; i++)
                {
                    var customerId = hotCustomers[i % hotCustomers.Length];
                    var orderNo = $"FREQ{i:D8}";
                    var amount = Math.Round((decimal)(random.NextDouble() * 1000), 2);
                    var insertSql = db switch
                    {
                        "SqlServer" => $"INSERT INTO {QuoteName(db, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, 'Pending', 'Test', GETDATE())",
                        "MySql" => $"INSERT INTO {QuoteName(db, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, 'Pending', 'Test', NOW())",
                        "PostgreSql" => $"INSERT INTO {QuoteName(db, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, 'Pending', 'Test', NOW())",
                        "Sqlite" => $"INSERT INTO {QuoteName(db, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, 'Pending', 'Test', datetime('now'))",
                        _ => throw new ArgumentException()
                    };
                    ExecuteWithParams(conn, db, insertSql, new Dictionary<string, object> { { "@o", orderNo }, { "@c", customerId }, { "@a", amount } });
                    insertCount++;
                }

                // 将数据插入到对应的分表（热数据/冷数据）
                var orderCols = "Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime";
                foreach (var tableName in allTables)
                {
                    var suffix = tableName.Replace("OrderData_", "");
                    var isHot = suffix == "_hot";
                    var setIdentityOn = db == "SqlServer"
                        ? $"SET IDENTITY_INSERT {QuoteName(db, tableName)} ON; "
                        : "";
                    var setIdentityOff = db == "SqlServer"
                        ? $" SET IDENTITY_INSERT {QuoteName(db, tableName)} OFF;"
                        : "";
                    var whereClause = isHot
                        ? "CustomerId IN ('CUST0001', 'CUST0002', 'CUST0003')"
                        : "CustomerId NOT IN ('CUST0001', 'CUST0002', 'CUST0003')";
                    var insertToShardSql = db switch
                    {
                        "SqlServer" => $"{setIdentityOn}INSERT INTO {QuoteName(db, tableName)} ({orderCols}) SELECT {orderCols} FROM {QuoteName(db, "OrderData")} WHERE {whereClause}{setIdentityOff}",
                        "MySql" => $"INSERT INTO {QuoteName(db, tableName)} ({orderCols}) SELECT {orderCols} FROM {QuoteName(db, "OrderData")} WHERE {whereClause}",
                        "PostgreSql" => $"INSERT INTO {QuoteName(db, tableName)} ({orderCols}) SELECT {orderCols} FROM {QuoteName(db, "OrderData")} WHERE {whereClause}",
                        "Sqlite" => $"INSERT INTO {QuoteName(db, tableName)} ({orderCols}) SELECT {orderCols} FROM {QuoteName(db, "OrderData")} WHERE {whereClause}",
                        _ => throw new ArgumentException()
                    };
                    try
                    {
                        Execute(conn, db, insertToShardSql);
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }

                // 查询每个分表的数据
                var allCounts = new Dictionary<string, int>();
                foreach (var t in allTables)
                {
                    try
                    {
                        allCounts[t] = QueryScalar(conn, db, $"SELECT COUNT(*) FROM {QuoteName(db, t)}");
                    }
                    catch
                    {
                        allCounts[t] = -1;
                    }
                }

                return Ok(new
                {
                    Strategy = "Frequency",
                    Database = db,
                    FrequencyField = "CustomerId",
                    HotThreshold = hotThreshold,
                    SimulatedInserts = insertCount,
                    AllShardCounts = allCounts,
                    Total = allCounts.Values.Where(v => v > 0).Sum()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace?.Length ?? 0)) });
            }
        }

        /// <summary>
        /// 同步分表数据（源 → 目标）
        /// </summary>
        [HttpPost("sync")]
        public IActionResult SyncShards([FromQuery] string source = "SqlServer",
                                         [FromQuery] string target = "MySql",
                                         [FromQuery] string table = "UserLog")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var srcConn = GetConnection(source);
                using var tgtConn = GetConnection(target);
                srcConn.Open();
                tgtConn.Open();

                // 读取源表所有数据
                var srcCount = QueryScalar(srcConn, source, $"SELECT COUNT(*) FROM {QuoteName(source, table)}");

                // 创建目标表（如果不存在）
                var createSql = target switch
                {
                    "SqlServer" => $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                        CREATE TABLE {QuoteName(target, table)} (Id INT IDENTITY(1,1) PRIMARY KEY, UserId NVARCHAR(50), Message NVARCHAR(500), Level NVARCHAR(20), CreateTime DATETIME DEFAULT GETDATE())",
                    "MySql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(target, table)} (Id INT AUTO_INCREMENT PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    "PostgreSql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(target, table)} (Id SERIAL PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime TIMESTAMP DEFAULT NOW())",
                    "Sqlite" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(target, table)} (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId TEXT, Message TEXT, Level TEXT, CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                    _ => throw new ArgumentException()
                };
                Execute(tgtConn, target, createSql);

                // 读取源数据
                var readCmd = srcConn.CreateCommand();
                readCmd.CommandText = $"SELECT UserId, Message, Level, CreateTime FROM {QuoteName(source, table)}";
                using var reader = readCmd.ExecuteReader();

                var rows = new List<(string userId, string message, string level, DateTime createTime)>();
                while (reader.Read())
                {
                    rows.Add((
                        reader.IsDBNull(0) ? "" : reader.GetString(0),
                        reader.IsDBNull(1) ? "" : reader.GetString(1),
                        reader.IsDBNull(2) ? "" : reader.GetString(2),
                        reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3)
                    ));
                }
                reader.Close();

                // 写入目标表（批量）
                var written = 0;
                foreach (var (userId, message, level, createTime) in rows)
                {
                    var insertSql = target switch
                    {
                        "SqlServer" => $"INSERT INTO {QuoteName(target, table)} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                        "MySql" => $"INSERT INTO {QuoteName(target, table)} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                        "PostgreSql" => $"INSERT INTO {QuoteName(target, table)} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                        "Sqlite" => $"INSERT INTO {QuoteName(target, table)} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                        _ => throw new ArgumentException()
                    };
                    ExecuteWithParams(tgtConn, target, insertSql, new Dictionary<string, object>
                    {
                        { "@u", userId }, { "@m", message }, { "@l", level }, { "@t", createTime }
                    });
                    written++;
                }

                var tgtCount = QueryScalar(tgtConn, target, $"SELECT COUNT(*) FROM {QuoteName(target, table)}");

                sw.Stop();
                return Ok(new
                {
                    Source = source,
                    Target = target,
                    Table = table,
                    SourceCount = srcCount,
                    Written = written,
                    TargetCount = tgtCount,
                    Elapsed = $"{sw.ElapsedMilliseconds}ms"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace?.Length ?? 0)) });
            }
        }

        // ===== 辅助方法 =====

        private void Execute(DbConnection conn, string dbType, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private void ExecuteWithParams(DbConnection conn, string dbType, string sql, Dictionary<string, object> parms)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (key, value) in parms)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = key;
                p.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
            cmd.ExecuteNonQuery();
        }

        private int QueryScalar(DbConnection conn, string dbType, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        private void InsertBulkLogs(DbConnection conn, string dbType, int count)
        {
            var random = new Random(42);
            var levels = new[] { "Info", "Warning", "Error", "Debug" };
            var users = Enumerable.Range(1, 100).Select(i => $"User{i:D3}").ToArray();

            for (int i = 0; i < count; i++)
            {
                var user = users[random.Next(users.Length)];
                var message = $"Log message {i}";
                var level = levels[random.Next(levels.Length)];
                var createTime = DateTime.Now.AddDays(-random.Next(365));

                var sql = dbType switch
                {
                    "SqlServer" => $"INSERT INTO {QuoteName(dbType, "UserLog")} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                    "MySql" => $"INSERT INTO {QuoteName(dbType, "UserLog")} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                    "PostgreSql" => $"INSERT INTO {QuoteName(dbType, "UserLog")} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                    "Sqlite" => $"INSERT INTO {QuoteName(dbType, "UserLog")} (UserId, Message, Level, CreateTime) VALUES (@u, @m, @l, @t)",
                    _ => throw new ArgumentException()
                };
                ExecuteWithParams(conn, dbType, sql, new Dictionary<string, object>
                {
                    { "@u", user }, { "@m", message }, { "@l", level }, { "@t", createTime }
                });
            }
        }

        private void InsertBulkOrders(DbConnection conn, string dbType, int count)
        {
            var random = new Random(42);
            var statuses = new[] { "Pending", "Processing", "Completed", "Cancelled" };
            var regions = new[] { "Beijing", "Shanghai", "Guangzhou", "Shenzhen", "Hangzhou" };

            for (int i = 0; i < count; i++)
            {
                var orderNo = $"ORD{i:D8}";
                var customerId = $"CUST{random.Next(1000):D4}";
                var amount = Math.Round((decimal)(random.NextDouble() * 10000), 2);
                var status = statuses[random.Next(statuses.Length)];
                var region = regions[random.Next(regions.Length)];
                var createTime = DateTime.Now.AddDays(-random.Next(365));

                var sql = dbType switch
                {
                    "SqlServer" => $"INSERT INTO {QuoteName(dbType, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, @s, @r, @t)",
                    "MySql" => $"INSERT INTO {QuoteName(dbType, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, @s, @r, @t)",
                    "PostgreSql" => $"INSERT INTO {QuoteName(dbType, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, @s, @r, @t)",
                    "Sqlite" => $"INSERT INTO {QuoteName(dbType, "OrderData")} (OrderNo, CustomerId, Amount, Status, Region, CreateTime) VALUES (@o, @c, @a, @s, @r, @t)",
                    _ => throw new ArgumentException()
                };
                ExecuteWithParams(conn, dbType, sql, new Dictionary<string, object>
                {
                    { "@o", orderNo }, { "@c", customerId }, { "@a", amount },
                    { "@s", status }, { "@r", region }, { "@t", createTime }
                });
            }
        }

        /// <summary>
        /// 创建分表（如果不存在）
        /// </summary>
        private void CreateTableIfNotExists(DbConnection conn, string dbType, string tableName, string baseTable)
        {
            var sql = dbType switch
            {
                "SqlServer" => $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                    CREATE TABLE {QuoteName(dbType, tableName)} (Id INT PRIMARY KEY, UserId NVARCHAR(50), Message NVARCHAR(500), Level NVARCHAR(20), CreateTime DATETIME DEFAULT GETDATE())",
                "MySql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id INT PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                "PostgreSql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id INT PRIMARY KEY, UserId VARCHAR(50), Message VARCHAR(500), Level VARCHAR(20), CreateTime TIMESTAMP DEFAULT NOW())",
                "Sqlite" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id INTEGER PRIMARY KEY, UserId TEXT, Message TEXT, Level TEXT, CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                _ => throw new ArgumentException()
            };
            Execute(conn, dbType, sql);
        }

        /// <summary>
        /// 创建订单分表（如果不存在）
        /// </summary>
        private void CreateOrderTableIfNotExists(DbConnection conn, string dbType, string tableName)
        {
            var sql = dbType switch
            {
                "SqlServer" => $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                    CREATE TABLE {QuoteName(dbType, tableName)} (Id INT IDENTITY(1,1) PRIMARY KEY, OrderNo NVARCHAR(50), CustomerId NVARCHAR(50), Amount DECIMAL(18,2), Status NVARCHAR(20), Region NVARCHAR(50), CreateTime DATETIME DEFAULT GETDATE())",
                "MySql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id INT AUTO_INCREMENT PRIMARY KEY, OrderNo VARCHAR(50), CustomerId VARCHAR(50), Amount DECIMAL(18,2), Status VARCHAR(20), Region VARCHAR(50), CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                "PostgreSql" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id SERIAL PRIMARY KEY, OrderNo VARCHAR(50), CustomerId VARCHAR(50), Amount DECIMAL(18,2), Status VARCHAR(20), Region VARCHAR(50), CreateTime TIMESTAMP DEFAULT NOW())",
                "Sqlite" => $@"CREATE TABLE IF NOT EXISTS {QuoteName(dbType, tableName)} (Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderNo TEXT, CustomerId TEXT, Amount REAL, Status TEXT, Region TEXT, CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP)",
                _ => throw new ArgumentException()
            };
            Execute(conn, dbType, sql);
        }

        /// <summary>
        /// 将源表数据按时间范围插入到分表
        /// </summary>
        private int InsertDataToTimeShards(DbConnection conn, string dbType, List<string> tableNames, string sourceTable, TimeShardingConfig timeConfig)
        {
            var totalInserted = 0;
            foreach (var tableName in tableNames)
            {
                // 从表名解析时间范围 (格式: UserLog_202506)
                var suffix = tableName.Replace($"{sourceTable}_", "");
                if (suffix.Length == 6 && int.TryParse(suffix, out var yearMonth))
                {
                    var year = yearMonth / 100;
                    var month = yearMonth % 100;
                    var startDate = new DateTime(year, month, 1);
                    var endDate = startDate.AddMonths(1);

                    // 使用显式列名避免IDENTITY问题；SQL Server需SET IDENTITY_INSERT ON
                    var setIdentityOn = dbType == "SqlServer"
                        ? $"SET IDENTITY_INSERT {QuoteName(dbType, tableName)} ON; "
                        : "";
                    var setIdentityOff = dbType == "SqlServer"
                        ? $" SET IDENTITY_INSERT {QuoteName(dbType, tableName)} OFF;"
                        : "";
                    var insertSql = dbType switch
                    {
                        "SqlServer" => $"{setIdentityOn}INSERT INTO {QuoteName(dbType, tableName)} (Id, UserId, Message, Level, CreateTime) SELECT Id, UserId, Message, Level, CreateTime FROM {QuoteName(dbType, sourceTable)} WHERE CreateTime >= @start AND CreateTime < @end{setIdentityOff}",
                        "MySql" => $"INSERT INTO {QuoteName(dbType, tableName)} (Id, UserId, Message, Level, CreateTime) SELECT Id, UserId, Message, Level, CreateTime FROM {QuoteName(dbType, sourceTable)} WHERE CreateTime >= @start AND CreateTime < @end",
                        "PostgreSql" => $"INSERT INTO {QuoteName(dbType, tableName)} (Id, UserId, Message, Level, CreateTime) SELECT Id, UserId, Message, Level, CreateTime FROM {QuoteName(dbType, sourceTable)} WHERE CreateTime >= @start AND CreateTime < @end",
                        "Sqlite" => $"INSERT INTO {QuoteName(dbType, tableName)} (Id, UserId, Message, Level, CreateTime) SELECT Id, UserId, Message, Level, CreateTime FROM {QuoteName(dbType, sourceTable)} WHERE CreateTime >= @start AND CreateTime < @end",
                        _ => throw new ArgumentException()
                    };

                    try
                    {
                        ExecuteWithParams(conn, dbType, insertSql, new Dictionary<string, object>
                        {
                            { "@start", startDate }, { "@end", endDate }
                        });
                        totalInserted++;
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续
                        Console.WriteLine($"Error inserting to {tableName}: {ex.Message}");
                    }
                }
            }
            return totalInserted;
        }

        /// <summary>
        /// 将源表数据按哈希插入到分表
        /// </summary>
        private int InsertDataToHashShards(DbConnection conn, string dbType, List<string> tableNames, string sourceTable, string hashField, int shardCount)
        {
            var totalInserted = 0;
            foreach (var tableName in tableNames)
            {
                var suffix = tableName.Replace($"{sourceTable}_", "");
                if (int.TryParse(suffix, out var shardIndex))
                {
                    // 使用显式列名避免IDENTITY问题；SQL Server需SET IDENTITY_INSERT ON
                    var columns = sourceTable == "UserLog"
                        ? "Id, UserId, Message, Level, CreateTime"
                        : "Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime";
                    var insertSql = dbType switch
                    {
                        "SqlServer" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE ABS(CHECKSUM({QuoteName(dbType, hashField)})) % {shardCount} = {shardIndex}",
                        "MySql" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE MOD(ABS(CRC32({hashField})), {shardCount}) = {shardIndex}",
                        "PostgreSql" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE ABS(hashtext({hashField}::text)) % {shardCount} = {shardIndex}",
                        "Sqlite" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE (ABS(ROWID) % {shardCount}) = {shardIndex}",
                        _ => throw new ArgumentException()
                    };

                    try
                    {
                        Execute(conn, dbType, insertSql);
                        totalInserted++;
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
            return totalInserted;
        }

        /// <summary>
        /// 将源表数据按状态插入到分表
        /// </summary>
        private int InsertDataToListShards(DbConnection conn, string dbType, List<string> tableNames, string sourceTable, string listField, Dictionary<string, string> valueMapping)
        {
            var totalInserted = 0;
            foreach (var tableName in tableNames)
            {
                var suffix = tableName.Replace($"{sourceTable}_", "");
                var matchingStatus = valueMapping.FirstOrDefault(kvp => kvp.Value == suffix).Key;
                if (matchingStatus != null)
                {
                    // 使用显式列名避免IDENTITY问题；SQL Server需SET IDENTITY_INSERT ON
                    var columns = sourceTable == "UserLog"
                        ? "Id, UserId, Message, Level, CreateTime"
                        : "Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime";
                    var setIdentityOn = dbType == "SqlServer"
                        ? $"SET IDENTITY_INSERT {QuoteName(dbType, tableName)} ON; "
                        : "";
                    var setIdentityOff = dbType == "SqlServer"
                        ? $" SET IDENTITY_INSERT {QuoteName(dbType, tableName)} OFF;"
                        : "";
                    var insertSql = dbType switch
                    {
                        "SqlServer" => $"{setIdentityOn}INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE {QuoteName(dbType, listField)} = @val{setIdentityOff}",
                        "MySql" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE {listField} = @val",
                        "PostgreSql" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE {listField} = @val",
                        "Sqlite" => $"INSERT INTO {QuoteName(dbType, tableName)} ({columns}) SELECT {columns} FROM {QuoteName(dbType, sourceTable)} WHERE {listField} = @val",
                        _ => throw new ArgumentException()
                    };

                    try
                    {
                        ExecuteWithParams(conn, dbType, insertSql, new Dictionary<string, object> { { "@val", matchingStatus } });
                        totalInserted++;
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
            return totalInserted;
        }
    }

}
