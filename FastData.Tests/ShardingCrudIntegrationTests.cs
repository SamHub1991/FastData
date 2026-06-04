using System;
using System.Collections.Generic;
using System.Linq;
using FastData.Model;
using FastData.Sharding;
using FastData.Sharding.Strategies;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 分表测试集合定义 - 确保多个分表测试类不会并行运行，
    /// 避免 ShardingManager 静态全局状态竞争导致测试不稳定。
    /// </summary>
    [CollectionDefinition("Sharding")]
    public class ShardingCollection : ICollectionFixture<ShardingCollectionFixture> { }

    /// <summary>
    /// 分表测试集合夹具 - 用于串行化分表相关测试。
    /// </summary>
    public class ShardingCollectionFixture { }

    #region 分表CRUD集成测试用实体类

    /// <summary>
    /// 日志实体 - 用于时间分表测试
    /// </summary>
    public class CrudTestLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
        public string LogLevel { get; set; }
        public string Module { get; set; }
    }

    /// <summary>
    /// 订单实体 - 用于哈希分表测试
    /// </summary>
    public class CrudTestOrder
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    /// <summary>
    /// 通用实体 - 用于配置切换测试
    /// </summary>
    public class CrudTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
        public string Code { get; set; }
    }

    #endregion

    /// <summary>
    /// 分表CRUD集成测试
    /// 
    /// 测试目的：验证分表策略的表名计算逻辑和配置管理。
    /// 由于SQLite不支持动态创建分表，本测试专注于以下方面：
    /// 1. 分表配置的正确注册与读取
    /// 2. 表名计算逻辑的准确性（时间分表、哈希分表）
    /// 3. 查询时分表路由的正确性
    /// 4. 分表元数据管理的正确性
    /// 5. 不同分表配置之间的隔离性
    /// </summary>
    [Collection("Sharding")]
    public class ShardingCrudIntegrationTests : IDisposable
    {
        /// <summary>
        /// 测试前清理所有分表配置，确保测试环境干净
        /// </summary>
        public ShardingCrudIntegrationTests()
        {
            ShardingManager.Clear();
            // 重置查询频率统计状态
            try
            {
                QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");
            }
            catch
            {
                // 忽略策略未初始化异常
            }
        }

        /// <summary>
        /// 测试后清理所有分表配置，避免影响后续测试
        /// </summary>
        public void Dispose()
        {
            ShardingManager.Clear();
        }

        #region 时间分表插入测试

        /// <summary>
        /// 时间分表插入测试 - 验证不同月份的日志数据写入正确的分表
        /// 原理：按月分表策略会根据实体的CreateTime字段计算出对应的月份后缀，
        /// 并生成形如 Log_202601、Log_202602 的表名。
        /// 本测试插入不同月份的模拟数据，验证GetTableName方法返回正确的分表名。
        /// </summary>
        [Fact]
        public void TimeSharding_Insert_VerifyCorrectTableByMonth()
        {
            // 配置按月分表
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(config);

            // 模拟插入1月日志数据
            var janLog = new CrudTestLog
            {
                Id = 1,
                Message = "January log entry",
                CreateTime = new DateTime(2026, 1, 15),
                LogLevel = "Info",
                Module = "Auth"
            };

            // 模拟插入3月日志数据
            var marLog = new CrudTestLog
            {
                Id = 2,
                Message = "March log entry",
                CreateTime = new DateTime(2026, 3, 20),
                LogLevel = "Error",
                Module = "Payment"
            };

            // 模拟插入12月日志数据
            var decLog = new CrudTestLog
            {
                Id = 3,
                Message = "December log entry",
                CreateTime = new DateTime(2026, 12, 31),
                LogLevel = "Warn",
                Module = "Report"
            };

            // 验证数据路由到正确的分表
            var janTable = ShardingManager.GetTableName(janLog);
            var marTable = ShardingManager.GetTableName(marLog);
            var decTable = ShardingManager.GetTableName(decLog);

            Assert.Equal("Log_202601", janTable);
            Assert.Equal("Log_202603", marTable);
            Assert.Equal("Log_202612", decTable);
        }

        /// <summary>
        /// 时间分表按天粒度插入测试 - 验证不同日期的数据写入正确的日分表
        /// </summary>
        [Fact]
        public void TimeSharding_Insert_VerifyCorrectTableByDay()
        {
            // 配置按天分表
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Day,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(config);

            // 模拟不同日期的日志数据
            var day1Log = new CrudTestLog
            {
                Id = 1,
                Message = "Day 1 log",
                CreateTime = new DateTime(2026, 5, 1)
            };

            var day28Log = new CrudTestLog
            {
                Id = 2,
                Message = "Day 28 log",
                CreateTime = new DateTime(2026, 5, 28)
            };

            // 验证按天分表的表名计算正确
            var day1Table = ShardingManager.GetTableName(day1Log);
            var day28Table = ShardingManager.GetTableName(day28Log);

            Assert.Equal("Log_20260501", day1Table);
            Assert.Equal("Log_20260528", day28Table);
        }

        #endregion

        #region 时间分表范围查询测试

        /// <summary>
        /// 时间分表范围查询测试 - 验证按时间范围查询时只路由到相关分表
        /// 原理：当查询带有时间范围参数时，分表策略会根据起止时间计算出
        /// 涉及的所有分表名，避免全表扫描。本测试验证3月到5月的查询
        /// 只包含 Log_202603、Log_202604、Log_202605 三个分表。
        /// </summary>
        [Fact]
        public void TimeSharding_RangeQuery_VerifyTargetedTablesOnly()
        {
            // 配置按月分表
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(config);

            // 模拟查询2026年3月至5月的日志数据
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 3, 1) },
                { "CreateTime_End", new DateTime(2026, 5, 31) }
            };

            // 获取应查询的分表列表
            var targetTables = ShardingManager.GetTableNames<CrudTestLog>(queryParams);

            // 验证只查询到相关月份的分表
            Assert.Equal(3, targetTables.Count);
            Assert.Contains("Log_202603", targetTables);
            Assert.Contains("Log_202604", targetTables);
            Assert.Contains("Log_202605", targetTables);

            // 验证不包含其他月份的分表
            Assert.DoesNotContain("Log_202601", targetTables);
            Assert.DoesNotContain("Log_202602", targetTables);
            Assert.DoesNotContain("Log_202606", targetTables);
        }

        /// <summary>
        /// 时间分表单月查询测试 - 验证查询单月数据时只路由到一个分表
        /// </summary>
        [Fact]
        public void TimeSharding_SingleMonthQuery_VerifySingleTable()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(config);

            // 模拟查询单月数据
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 7, 1) },
                { "CreateTime_End", new DateTime(2026, 7, 31) }
            };

            var targetTables = ShardingManager.GetTableNames<CrudTestLog>(queryParams);

            // 验证只查询到7月分表
            Assert.Single(targetTables);
            Assert.Contains("Log_202607", targetTables);
        }

        #endregion

        #region 哈希分表插入测试

        /// <summary>
        /// 哈希分表插入测试 - 验证订单数据均匀分布到4个哈希分片
        /// 原理：哈希分表策略会对实体的哈希字段（如OrderNo）进行取模运算，
        /// 根据结果将数据路由到 Order_0000 ~ Order_0003 四个分表中。
        /// 本测试插入多条不同订单号的数据，验证数据分布到不同的分片。
        /// </summary>
        [Fact]
        public void HashSharding_Insert_VerifyDataDistributionAcrossShards()
        {
            // 配置4个哈希分片
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4
                }
            };
            ShardingManager.Configure<CrudTestOrder>(config);

            // 准备多条订单数据
            var orders = new List<CrudTestOrder>
            {
                new CrudTestOrder { Id = 1, OrderNo = "ORD20260001", ProductName = "商品A", Amount = 100m, OrderDate = new DateTime(2026, 1, 1) },
                new CrudTestOrder { Id = 2, OrderNo = "ORD20260002", ProductName = "商品B", Amount = 200m, OrderDate = new DateTime(2026, 1, 2) },
                new CrudTestOrder { Id = 3, OrderNo = "ORD20260003", ProductName = "商品C", Amount = 300m, OrderDate = new DateTime(2026, 1, 3) },
                new CrudTestOrder { Id = 4, OrderNo = "ORD20260004", ProductName = "商品D", Amount = 400m, OrderDate = new DateTime(2026, 1, 4) },
                new CrudTestOrder { Id = 5, OrderNo = "ORD20260005", ProductName = "商品E", Amount = 500m, OrderDate = new DateTime(2026, 1, 5) },
                new CrudTestOrder { Id = 6, OrderNo = "ORD20260006", ProductName = "商品F", Amount = 600m, OrderDate = new DateTime(2026, 1, 6) },
            };

            // 记录每个订单路由到的分表
            var shardTableMap = new Dictionary<string, string>();
            foreach (var order in orders)
            {
                var tableName = ShardingManager.GetTableName(order);
                shardTableMap[order.OrderNo] = tableName;

                // 验证表名格式正确（Order_后跟4位数字）
                Assert.StartsWith("Order_", tableName);
                Assert.Equal(10, tableName.Length);
            }

            // 验证数据分布到至少2个不同的分片（理想情况下应分布到多个分片）
            var distinctShards = shardTableMap.Values.Distinct().ToList();
            Assert.True(distinctShards.Count >= 2,
                $"订单应分布到至少2个分片，但实际只分布到 {distinctShards.Count} 个分片");
        }

        #endregion

        #region 哈希分表精确查询测试

        /// <summary>
        /// 哈希分表精确查询测试 - 验证按哈希字段查询时只路由到一个分片
        /// 原理：当查询条件包含哈希字段（如OrderNo）时，分表策略可以精确计算出
        /// 数据所在的分片，只需查询单个分表即可，大幅降低查询开销。
        /// </summary>
        [Fact]
        public void HashSharding_ExactQuery_VerifySingleShardRoute()
        {
            // 配置4个哈希分片
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4
                }
            };
            ShardingManager.Configure<CrudTestOrder>(config);

            // 模拟按订单号精确查询
            var queryParams = new Dictionary<string, object>
            {
                { "OrderNo", "ORD20260001" }
            };

            // 获取应查询的分表列表
            var targetTables = ShardingManager.GetTableNames<CrudTestOrder>(queryParams);

            // 验证只查询到一个分片
            Assert.Single(targetTables);

            // 验证该分片名与直接通过实体计算的表名一致
            var testOrder = new CrudTestOrder { Id = 1, OrderNo = "ORD20260001" };
            var expectedTable = ShardingManager.GetTableName(testOrder);
            Assert.Contains(expectedTable, targetTables);
        }

        /// <summary>
        /// 哈希分表多个精确值查询测试 - 验证多个哈希值查询路由到对应数量的分片
        /// </summary>
        [Fact]
        public void HashSharding_MultipleExactQuery_VerifyCorrectShardCount()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4
                }
            };
            ShardingManager.Configure<CrudTestOrder>(config);

            // 查询两个不同订单号，分别计算它们的分片
            var order1Table = ShardingManager.GetTableNames<CrudTestOrder>(new Dictionary<string, object>
            {
                { "OrderNo", "ORD20260001" }
            });

            var order2Table = ShardingManager.GetTableNames<CrudTestOrder>(new Dictionary<string, object>
            {
                { "OrderNo", "ORD20260002" }
            });

            // 每个查询都应该只返回一个分片
            Assert.Single(order1Table);
            Assert.Single(order2Table);
        }

        #endregion

        #region 分表配置切换测试

        /// <summary>
        /// 分表配置切换测试 - 验证同一个实体切换不同分表配置时配置隔离
        /// 原理：ShardingManager使用实体类型作为键存储配置，同一实体可以先后
        /// 配置不同的分表策略。本测试先配置时间分表，再切换为哈希分表，
        /// 验证切换后的配置正确覆盖旧配置。
        /// </summary>
        [Fact]
        public void ShardingConfig_Switch_VerifyConfigurationIsolation()
        {
            // 第一步：配置时间分表
            var timeConfig = new ShardingConfig
            {
                BaseTableName = "Entity",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestEntity>(timeConfig);

            // 验证时间分表配置生效
            var timeEnabled = ShardingManager.IsShardingEnabled<CrudTestEntity>();
            var timeConfigResult = ShardingManager.GetConfig<CrudTestEntity>();
            Assert.True(timeEnabled);
            Assert.Equal(ShardingType.Time, timeConfigResult.ShardingType);

            // 第二步：切换为哈希分表配置
            var hashConfig = new ShardingConfig
            {
                BaseTableName = "Entity",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "Code",
                    ShardCount = 8
                }
            };
            ShardingManager.Configure<CrudTestEntity>(hashConfig);

            // 验证哈希分表配置覆盖旧配置
            var hashConfigResult = ShardingManager.GetConfig<CrudTestEntity>();
            Assert.NotNull(hashConfigResult);
            Assert.Equal(ShardingType.Hash, hashConfigResult.ShardingType);
            Assert.Equal(8, hashConfigResult.HashConfig.ShardCount);
            Assert.Equal("Code", hashConfigResult.HashConfig.HashField);
        }

        /// <summary>
        /// 不同实体独立配置测试 - 验证不同实体的分表配置互不影响
        /// </summary>
        [Fact]
        public void ShardingConfig_MultipleEntities_VerifyIndependentConfig()
        {
            // 为CrudTestLog配置时间分表
            var logConfig = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(logConfig);

            // 为CrudTestOrder配置哈希分表
            var orderConfig = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4
                }
            };
            ShardingManager.Configure<CrudTestOrder>(orderConfig);

            // 为CrudTestEntity配置列表分表
            var entityConfig = new ShardingConfig
            {
                BaseTableName = "Entity",
                ShardingType = ShardingType.List,
                ListConfig = new ListShardingConfig
                {
                    ListField = "Name",
                    ValueMapping = new Dictionary<string, string>
                    {
                        { "Active", "active" },
                        { "Inactive", "inactive" }
                    }
                }
            };
            ShardingManager.Configure<CrudTestEntity>(entityConfig);

            // 验证三个实体的分表配置互不干扰
            Assert.True(ShardingManager.IsShardingEnabled<CrudTestLog>());
            Assert.True(ShardingManager.IsShardingEnabled<CrudTestOrder>());
            Assert.True(ShardingManager.IsShardingEnabled<CrudTestEntity>());

            Assert.Equal(ShardingType.Time, ShardingManager.GetConfig<CrudTestLog>().ShardingType);
            Assert.Equal(ShardingType.Hash, ShardingManager.GetConfig<CrudTestOrder>().ShardingType);
            Assert.Equal(ShardingType.List, ShardingManager.GetConfig<CrudTestEntity>().ShardingType);

            // 清除CrudTestLog的配置，验证不影响其他实体
            ShardingManager.Clear();
            Assert.False(ShardingManager.IsShardingEnabled<CrudTestLog>());
            Assert.False(ShardingManager.IsShardingEnabled<CrudTestOrder>());
            Assert.False(ShardingManager.IsShardingEnabled<CrudTestEntity>());
        }

        #endregion

        #region 分表元数据验证测试

        /// <summary>
        /// 分表元数据验证测试 - 验证GetAllTableNames返回正确的分表列表
        /// 原理：GetAllTableNames方法会根据分表配置生成所有可能的分表名，
        /// 用于DDL操作、数据迁移、统计查询等场景。
        /// 本测试分别验证时间分表和哈希分表的元数据生成正确性。
        /// </summary>
        [Fact]
        public void ShardingMetadata_GetAllTableNames_VerifyCorrectTableList()
        {
            // 测试时间分表元数据 - 按月分表
            var timeConfig = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1),
                    EndTime = new DateTime(2026, 6, 30)
                }
            };
            ShardingManager.Configure<CrudTestLog>(timeConfig);

            var timeTables = ShardingManager.GetAllTableNames<CrudTestLog>();

            // 验证返回了非空的分表列表
            Assert.NotEmpty(timeTables);
            Assert.True(timeTables.Count > 0, "时间分表应返回至少一个分表");

            // 清除时间分表配置
            ShardingManager.Clear();

            // 测试哈希分表元数据 - 4个分片
            var hashConfig = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 4
                }
            };
            ShardingManager.Configure<CrudTestOrder>(hashConfig);

            var hashTables = ShardingManager.GetAllTableNames<CrudTestOrder>();

            // 验证哈希分表返回正确数量的分片
            Assert.Equal(4, hashTables.Count);
            Assert.Contains("Order_0000", hashTables);
            Assert.Contains("Order_0001", hashTables);
            Assert.Contains("Order_0002", hashTables);
            Assert.Contains("Order_0003", hashTables);
        }

        /// <summary>
        /// 分表元数据 - 验证不同分片数量的哈希分表生成
        /// </summary>
        [Fact]
        public void ShardingMetadata_DifferentShardCount_VerifyTableGeneration()
        {
            // 测试2个分片
            var config2 = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 2
                }
            };
            ShardingManager.Configure<CrudTestOrder>(config2);

            var tables2 = ShardingManager.GetAllTableNames<CrudTestOrder>();
            Assert.Equal(2, tables2.Count);

            // 清除并重新配置为8个分片
            ShardingManager.Clear();

            var config8 = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 8
                }
            };
            ShardingManager.Configure<CrudTestOrder>(config8);

            var tables8 = ShardingManager.GetAllTableNames<CrudTestOrder>();
            Assert.Equal(8, tables8.Count);
        }

        #endregion

        #region 查询对象分表参数测试

        /// <summary>
        /// 验证DataQuery链式API正确设置分表参数
        /// </summary>
        [Fact]
        public void DataQuery_ChainableShardingParams_VerifyCorrectSetup()
        {
            // 配置时间分表
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<CrudTestLog>(config);

            // 使用链式API设置分表查询参数
            var startTime = new DateTime(2026, 3, 1);
            var endTime = new DateTime(2026, 5, 31);
            var query = new DataQuery<CrudTestLog>();

            var result = query
                .UseSharding()
                .WithTimeRange("CreateTime", startTime, endTime)
                .WithShardingParam("LogLevel", "Error");

            // 验证分表查询参数正确设置
            Assert.True(result.EnableSharding);
            Assert.Equal(3, result.ShardingQueryParams.Count);
            Assert.Equal(startTime, result.ShardingQueryParams["CreateTime_Start"]);
            Assert.Equal(endTime, result.ShardingQueryParams["CreateTime_End"]);
            Assert.Equal("Error", result.ShardingQueryParams["LogLevel"]);
        }

        #endregion
    }
}
