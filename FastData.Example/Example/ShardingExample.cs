using System;
using System.Collections.Generic;
using FastData.Sharding;
using FastData.Sharding.Strategies;

namespace FastData.Example.Example
{
    /// <summary>
    /// 分表功能示例
    /// 演示各种分表策略的使用方法
    /// </summary>
    public class ShardingExample
    {
        /// <summary>
        /// 运行所有分表示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== 分表功能示例 ===\n");

            TimeShardingExample();
            HashShardingExample();
            ListShardingExample();
            CompositeShardingExample();
            QueryFrequencyShardingExample();
            ShardingQueryExample();
            ShardingWriteExample();

            Console.WriteLine("\n=== 分表功能示例完成 ===");
        }

        /// <summary>
        /// 时间分表示例
        /// </summary>
        static void TimeShardingExample()
        {
            Console.WriteLine("\n--- 时间分表 ---");

            // 配置时间分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };

            // 注册配置
            ShardingManager.Configure<UserLog>(config);

            // 创建不同时间的日志
            var logs = new List<UserLog>
            {
                new UserLog { Id = 1, Message = "January log", CreateTime = new DateTime(2026, 1, 15) },
                new UserLog { Id = 2, Message = "February log", CreateTime = new DateTime(2026, 2, 20) },
                new UserLog { Id = 3, Message = "March log", CreateTime = new DateTime(2026, 3, 10) }
            };

            // 获取分表名称
            foreach (var log in logs)
            {
                var tableName = ShardingManager.GetTableName(log);
                Console.WriteLine($"  Log {log.Id} -> {tableName}");
            }

            // 查询指定时间范围的表
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 1, 1) },
                { "CreateTime_End", new DateTime(2026, 2, 28) }
            };
            var tableNames = ShardingManager.GetTableNames<UserLog>(queryParams);
            Console.WriteLine($"  查询表: {string.Join(", ", tableNames)}");
        }

        /// <summary>
        /// 哈希分表示例
        /// </summary>
        static void HashShardingExample()
        {
            Console.WriteLine("\n--- 哈希分表 ---");

            // 配置哈希分表
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4,
                    Algorithm = HashAlgorithm.Modulo
                }
            };

            ShardingManager.Configure<Order>(config);

            // 创建不同订单号的订单
            var orders = new List<Order>
            {
                new Order { Id = 1, OrderNo = "ORD20260101001" },
                new Order { Id = 2, OrderNo = "ORD20260202002" },
                new Order { Id = 3, OrderNo = "ORD20260303003" },
                new Order { Id = 4, OrderNo = "ORD20260404004" }
            };

            // 获取分表名称
            foreach (var order in orders)
            {
                var tableName = ShardingManager.GetTableName(order);
                Console.WriteLine($"  Order {order.OrderNo} -> {tableName}");
            }

            // 获取所有分表
            var allTables = ShardingManager.GetAllTableNames<Order>();
            Console.WriteLine($"  所有分表: {string.Join(", ", allTables)}");
        }

        /// <summary>
        /// 列表分表示例
        /// </summary>
        static void ListShardingExample()
        {
            Console.WriteLine("\n--- 列表分表 ---");

            // 配置列表分表
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
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

            ShardingManager.Configure<Order>(config);

            // 创建不同状态的订单
            var orders = new List<Order>
            {
                new Order { Id = 1, OrderNo = "ORD001", Status = "Pending" },
                new Order { Id = 2, OrderNo = "ORD002", Status = "Processing" },
                new Order { Id = 3, OrderNo = "ORD003", Status = "Completed" }
            };

            // 获取分表名称
            foreach (var order in orders)
            {
                var tableName = ShardingManager.GetTableName(order);
                Console.WriteLine($"  Order {order.OrderNo} ({order.Status}) -> {tableName}");
            }
        }

        /// <summary>
        /// 组合键分表示例
        /// </summary>
        static void CompositeShardingExample()
        {
            Console.WriteLine("\n--- 组合键分表 ---");

            // 配置组合键分表
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Composite,
                CompositeConfig = new CompositeShardingConfig
                {
                    CompositeFields = new List<string> { "Region", "CustomerType" },
                    UseHash = true,
                    ShardCount = 8
                }
            };

            ShardingManager.Configure<Order>(config);

            // 创建不同组合的订单
            var orders = new List<Order>
            {
                new Order { Id = 1, OrderNo = "ORD001", Region = "Beijing", CustomerType = "VIP" },
                new Order { Id = 2, OrderNo = "ORD002", Region = "Shanghai", CustomerType = "Normal" },
                new Order { Id = 3, OrderNo = "ORD003", Region = "Guangzhou", CustomerType = "VIP" }
            };

            // 获取分表名称
            foreach (var order in orders)
            {
                var tableName = ShardingManager.GetTableName(order);
                Console.WriteLine($"  Order {order.OrderNo} ({order.Region}-{order.CustomerType}) -> {tableName}");
            }
        }

        /// <summary>
        /// 查询频率分表示例
        /// </summary>
        static void QueryFrequencyShardingExample()
        {
            Console.WriteLine("\n--- 查询频率分表 ---");

            // 配置查询频率分表
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 5,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.ByHash,
                    ColdShardCount = 4
                }
            };

            ShardingManager.Configure<UserLog>(config);

            // 模拟查询频率
            Console.WriteLine("  模拟查询频率...");
            for (int i = 0; i < 10; i++)
            {
                QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            }
            for (int i = 0; i < 2; i++)
            {
                QueryFrequencyShardingStrategy.RecordQuery("UserId", "user456");
            }

            // 获取查询频率
            var freq1 = QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", "user123");
            var freq2 = QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", "user456");
            Console.WriteLine($"  user123 查询频率: {freq1}");
            Console.WriteLine($"  user456 查询频率: {freq2}");

            // 获取热数据
            var hotValues = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 5);
            Console.WriteLine($"  热数据: {string.Join(", ", hotValues)}");

            // 创建日志
            var log1 = new UserLog { Id = 1, Message = "Hot data", UserId = "user123" };
            var log2 = new UserLog { Id = 2, Message = "Cold data", UserId = "user456" };

            var tableName1 = ShardingManager.GetTableName(log1);
            var tableName2 = ShardingManager.GetTableName(log2);
            Console.WriteLine($"  user123 -> {tableName1}");
            Console.WriteLine($"  user456 -> {tableName2}");
        }

        /// <summary>
        /// 分表查询示例
        /// </summary>
        static void ShardingQueryExample()
        {
            Console.WriteLine("\n--- 分表查询 ---");

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
            ShardingManager.Configure<UserLog>(config);

            // 查询参数
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 1, 1) },
                { "CreateTime_End", new DateTime(2026, 3, 31) }
            };

            Console.WriteLine("  查询 2026 年第一季度的日志...");
            Console.WriteLine($"  查询表: {string.Join(", ", ShardingManager.GetTableNames<UserLog>(queryParams))}");

            // 实际查询（需要数据库连接）
            // var logs = ShardingReadHelper.Query<UserLog>(
            //     log => log.Level == "Error",
            //     queryParams
            // );

            // 分页查询
            Console.WriteLine("  分页查询支持...");
            // var pagedResult = ShardingReadHelper.QueryPage<UserLog>(
            //     log => log.Level == "Error",
            //     queryParams,
            //     pageIndex: 1,
            //     pageSize: 20
            // );
        }

        /// <summary>
        /// 分表写入示例
        /// </summary>
        static void ShardingWriteExample()
        {
            Console.WriteLine("\n--- 分表写入 ---");

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
            ShardingManager.Configure<UserLog>(config);

            // 创建日志
            var log = new UserLog
            {
                Id = 1,
                Message = "Test log",
                Level = "Info",
                CreateTime = DateTime.Now,
                UserId = "user123"
            };

            var tableName = ShardingManager.GetTableName(log);
            Console.WriteLine($"  写入表: {tableName}");

            // 实际写入（需要数据库连接）
            // ShardingWriteHelper.Add(log);

            // 批量写入
            var logs = new List<UserLog>
            {
                new UserLog { Id = 2, Message = "Log 1", CreateTime = DateTime.Now.AddDays(-1) },
                new UserLog { Id = 3, Message = "Log 2", CreateTime = DateTime.Now.AddDays(-2) }
            };

            Console.WriteLine($"  批量写入 {logs.Count} 条记录...");
            // ShardingWriteHelper.AddList(logs);
        }
    }

    // 示例实体类
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
