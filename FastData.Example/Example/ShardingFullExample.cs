using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FastData.Config;
using FastData.Sharding;
using FastData.Sharding.Strategies;
#if NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace FastData.Example.Example
{
    /// <summary>
    /// 分表功能完整示例
    /// 演示在 SQL Server 中使用分表功能
    /// </summary>
    public class ShardingFullExample
    {
        private static string _connectionString;

        static ShardingFullExample()
        {
            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            // 使用 FastData 统一配置系统
            var connections = FastDataConfig.GetConnectionSummaries();
            var sqlServerConfig = connections.Find(c => c["key"] == "SqlServer");
            
            if (sqlServerConfig != null)
            {
                // 从配置中获取连接字符串（已脱敏，使用默认值）
                _connectionString = "server=localhost;database=FastDataDemo;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";
            }
            else
            {
                _connectionString = "server=localhost;database=FastDataDemo;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";
            }
        }

        /// <summary>
        /// 运行所有分表示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== 分表功能完整示例 (SQL Server) ===\n");

            try
            {
                // 1. 创建测试表
                CreateTestTables();

                // 2. 插入大量测试数据
                InsertBulkData();

                // 3. 时间分表测试
                TimeShardingTest();

                // 4. 哈希分表测试
                HashShardingTest();

                // 5. 列表分表测试
                ListShardingTest();

                // 6. 查询频率分表测试
                QueryFrequencyShardingTest();

                // 7. 链式 API 测试
                ChainableApiTest();

                Console.WriteLine("\n=== 分表功能完整示例完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 创建测试表
        /// </summary>
        static void CreateTestTables()
        {
            Console.WriteLine("\n--- 创建测试表 ---");

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

                Console.WriteLine("  测试表创建完成");
            }
        }

        /// <summary>
        /// 插入大量测试数据
        /// </summary>
        static void InsertBulkData()
        {
            Console.WriteLine("\n--- 插入大量测试数据 ---");
            var stopwatch = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 插入 UserLog 数据（10000 条）
                Console.WriteLine("  插入 UserLog 数据...");
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

                    for (int i = 0; i < 10000; i++)
                    {
                        var row = dataTable.NewRow();
                        row["UserId"] = users[random.Next(users.Length)];
                        row["Message"] = $"Log message {i}";
                        row["Level"] = levels[random.Next(levels.Length)];
                        row["CreateTime"] = DateTime.Now.AddDays(-random.Next(365));
                        dataTable.Rows.Add(row);
                    }

                    bulkCopy.WriteToServer(dataTable);
                    Console.WriteLine($"  UserLog: {dataTable.Rows.Count} 条记录");
                }

                // 插入 OrderData 数据（5000 条）
                Console.WriteLine("  插入 OrderData 数据...");
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

                    for (int i = 0; i < 5000; i++)
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
                    Console.WriteLine($"  OrderData: {dataTable.Rows.Count} 条记录");
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"  数据插入完成，耗时: {stopwatch.Elapsed.TotalSeconds:F2} 秒");
        }

        /// <summary>
        /// 时间分表测试
        /// </summary>
        static void TimeShardingTest()
        {
            Console.WriteLine("\n--- 时间分表测试 ---");

            // 配置时间分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2025, 1, 1)
                }
            };

            ShardingManager.Configure<ShardingUserLog>(config);

            // 创建分表
            CreateShardingTables(config, "UserLog");

            // 查询并插入数据到分表
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 查询源数据
                var selectSql = "SELECT Id, UserId, Message, Level, CreateTime FROM UserLog";
                using (var cmd = new SqlCommand(selectSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var batchCount = 0;
                    while (reader.Read())
                    {
                        var log = new ShardingUserLog
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetString(1),
                            Message = reader.GetString(2),
                            Level = reader.GetString(3),
                            CreateTime = reader.GetDateTime(4)
                        };

                        var tableName = ShardingManager.GetTableName(log);
                        InsertToShardingTable(conn, tableName, log);
                        batchCount++;
                    }

                    Console.WriteLine($"  时间分表处理: {batchCount} 条记录");
                }
            }

            // 查询分表数据
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2025, 6, 1) },
                { "CreateTime_End", new DateTime(2025, 12, 31) }
            };

            var tableNames = ShardingManager.GetTableNames<ShardingUserLog>(queryParams);
            Console.WriteLine($"  查询 2025 年下半年数据，涉及分表: {string.Join(", ", tableNames)}");
        }

        /// <summary>
        /// 哈希分表测试
        /// </summary>
        static void HashShardingTest()
        {
            Console.WriteLine("\n--- 哈希分表测试 ---");

            // 配置哈希分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "UserId",
                    ShardCount = 4
                }
            };

            ShardingManager.Configure<ShardingUserLog>(config);

            // 创建分表
            CreateShardingTables(config, "UserLog");

            // 查询并插入数据到分表
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var selectSql = "SELECT Id, UserId, Message, Level, CreateTime FROM UserLog";
                using (var cmd = new SqlCommand(selectSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var batchCount = 0;
                    while (reader.Read())
                    {
                        var log = new ShardingUserLog
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetString(1),
                            Message = reader.GetString(2),
                            Level = reader.GetString(3),
                            CreateTime = reader.GetDateTime(4)
                        };

                        var tableName = ShardingManager.GetTableName(log);
                        InsertToShardingTable(conn, tableName, log);
                        batchCount++;
                    }

                    Console.WriteLine($"  哈希分表处理: {batchCount} 条记录");
                }
            }

            // 查询特定用户的日志
            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "User050" }
            };

            var tableNames = ShardingManager.GetTableNames<ShardingUserLog>(queryParams);
            Console.WriteLine($"  查询 User050 的日志，涉及分表: {string.Join(", ", tableNames)}");
        }

        /// <summary>
        /// 列表分表测试
        /// </summary>
        static void ListShardingTest()
        {
            Console.WriteLine("\n--- 列表分表测试 ---");

            // 配置列表分表
            var config = new ShardingConfig
            {
                BaseTableName = "OrderData",
                ShardingType = ShardingType.List,
                ListConfig = new ListShardingConfig
                {
                    ListField = "Status",
                    ValueMapping = new Dictionary<string, string>
                    {
                        { "Pending", "pending" },
                        { "Processing", "processing" },
                        { "Completed", "completed" },
                        { "Cancelled", "cancelled" }
                    }
                }
            };

            ShardingManager.Configure<ShardingOrderData>(config);

            // 创建分表
            CreateShardingTables(config, "OrderData");

            // 查询并插入数据到分表
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var selectSql = "SELECT Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime FROM OrderData";
                using (var cmd = new SqlCommand(selectSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var batchCount = 0;
                    while (reader.Read())
                    {
                        var order = new ShardingOrderData
                        {
                            Id = reader.GetInt32(0),
                            OrderNo = reader.GetString(1),
                            CustomerId = reader.GetString(2),
                            Amount = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            Region = reader.GetString(5),
                            CreateTime = reader.GetDateTime(6)
                        };

                        var tableName = ShardingManager.GetTableName(order);
                        InsertOrderToShardingTable(conn, tableName, order);
                        batchCount++;
                    }

                    Console.WriteLine($"  列表分表处理: {batchCount} 条记录");
                }
            }

            // 查询特定状态的订单
            var queryParams = new Dictionary<string, object>
            {
                { "Status", "Completed" }
            };

            var tableNames = ShardingManager.GetTableNames<ShardingOrderData>(queryParams);
            Console.WriteLine($"  查询已完成订单，涉及分表: {string.Join(", ", tableNames)}");
        }

        /// <summary>
        /// 查询频率分表测试
        /// </summary>
        static void QueryFrequencyShardingTest()
        {
            Console.WriteLine("\n--- 查询频率分表测试 ---");

            // 配置查询频率分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 50,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.ByHash,
                    ColdShardCount = 4
                }
            };

            ShardingManager.Configure<ShardingUserLog>(config);

            // 模拟查询频率
            Console.WriteLine("  模拟查询频率...");
            var random = new Random();
            var users = Enumerable.Range(1, 100).Select(i => $"User{i:D3}").ToArray();

            foreach (var user in users)
            {
                var queryCount = random.Next(1, 200);
                for (int i = 0; i < queryCount; i++)
                {
                    QueryFrequencyShardingStrategy.RecordQuery("UserId", user);
                }
            }

            // 获取热数据
            var hotUsers = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 50);
            Console.WriteLine($"  热数据用户 ({hotUsers.Count} 个): {string.Join(", ", hotUsers.Take(5))}...");

            // 创建分表
            CreateShardingTables(config, "UserLog");

            // 查询并插入数据到分表
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var selectSql = "SELECT Id, UserId, Message, Level, CreateTime FROM UserLog";
                using (var cmd = new SqlCommand(selectSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var batchCount = 0;
                    while (reader.Read())
                    {
                        var log = new ShardingUserLog
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetString(1),
                            Message = reader.GetString(2),
                            Level = reader.GetString(3),
                            CreateTime = reader.GetDateTime(4)
                        };

                        var tableName = ShardingManager.GetTableName(log);
                        InsertToShardingTable(conn, tableName, log);
                        batchCount++;
                    }

                    Console.WriteLine($"  查询频率分表处理: {batchCount} 条记录");
                }
            }

            // 查询热数据
            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "User001" }
            };

            var tableNames = ShardingManager.GetTableNames<ShardingUserLog>(queryParams);
            Console.WriteLine($"  查询 User001（热数据），涉及分表: {string.Join(", ", tableNames)}");
        }

        /// <summary>
        /// 链式 API 测试
        /// </summary>
        static void ChainableApiTest()
        {
            Console.WriteLine("\n--- 链式 API 测试 ---");

            // 配置时间分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month
                }
            };

            ShardingManager.Configure<ShardingUserLog>(config);

            // 使用链式 API
            Console.WriteLine("  使用链式 API 查询...");

            // 模拟链式查询（实际需要数据库连接）
            var query = FastRead.Query<ShardingUserLog>(l => l.Level == "Error")
                .UseSharding()
                .WithTimeRange("CreateTime", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

            Console.WriteLine($"  链式查询配置:");
            Console.WriteLine($"    - 启用分表: {query.EnableSharding}");
            Console.WriteLine($"    - 分表参数数量: {query.ShardingQueryParams.Count}");

            foreach (var param in query.ShardingQueryParams)
            {
                Console.WriteLine($"    - {param.Key}: {param.Value}");
            }
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        static void CreateShardingTables(ShardingConfig config, string baseTableName)
        {
            var tableNames = ShardingManager.GetAllTableNames<object>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var tableName in tableNames)
                {
                    var createSql = $@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                        EXEC sp_rename '{baseTableName}', '{tableName}';
                    ";

                    try
                    {
                        using (var cmd = new SqlCommand(createSql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // 表可能已存在，忽略错误
                    }
                }
            }
        }

        /// <summary>
        /// 插入日志到分表
        /// </summary>
        static void InsertToShardingTable(SqlConnection conn, string tableName, ShardingUserLog log)
        {
            var sql = $@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                CREATE TABLE {tableName} (
                    Id INT,
                    UserId NVARCHAR(50),
                    Message NVARCHAR(500),
                    Level NVARCHAR(20),
                    CreateTime DATETIME
                );

                INSERT INTO {tableName} (Id, UserId, Message, Level, CreateTime)
                VALUES (@Id, @UserId, @Message, @Level, @CreateTime);
            ";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", log.Id);
                cmd.Parameters.AddWithValue("@UserId", log.UserId);
                cmd.Parameters.AddWithValue("@Message", log.Message);
                cmd.Parameters.AddWithValue("@Level", log.Level);
                cmd.Parameters.AddWithValue("@CreateTime", log.CreateTime);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 插入订单到分表
        /// </summary>
        static void InsertOrderToShardingTable(SqlConnection conn, string tableName, ShardingOrderData order)
        {
            var sql = $@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                CREATE TABLE {tableName} (
                    Id INT,
                    OrderNo NVARCHAR(50),
                    CustomerId NVARCHAR(50),
                    Amount DECIMAL(18,2),
                    Status NVARCHAR(20),
                    Region NVARCHAR(50),
                    CreateTime DATETIME
                );

                INSERT INTO {tableName} (Id, OrderNo, CustomerId, Amount, Status, Region, CreateTime)
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

    /// <summary>
    /// 用户日志实体
    /// </summary>
    public class ShardingUserLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 订单数据实体
    /// </summary>
    public class ShardingOrderData
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
