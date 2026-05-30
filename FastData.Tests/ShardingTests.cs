using System;
using System.Collections.Generic;
using System.Linq;
using FastData.Model;
using FastData.Sharding;
using FastData.Sharding.Strategies;
using Xunit;

namespace FastData.Tests
{
    #region 测试用实体类

    public class TestLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
        public string LogLevel { get; set; }
        public string Module { get; set; }
        public string UserId { get; set; }
    }

    public class TestOrder
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
        public string CustomerType { get; set; }
    }

    public class ShardingTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
        public string UserId { get; set; }
        public string Level { get; set; }
    }

    public class ShardingTestOrder
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public string CustomerType { get; set; }
    }

    #endregion

    /// <summary>
    /// 分表功能完整测试（合并自 ShardingTests + ShardingCrudTests）
    /// </summary>
    public class ShardingTests : IDisposable
    {
        public ShardingTests()
        {
            ShardingManager.Clear();
        }

        public void Dispose()
        {
            ShardingManager.Clear();
        }

        #region 时间分表测试

        [Fact]
        public void TimeSharding_GetTableName_ByDay()
        {
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
            ShardingManager.Configure<TestLog>(config);

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = new DateTime(2026, 5, 27)
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("Log_20260527", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_ByMonth()
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
            ShardingManager.Configure<TestLog>(config);

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = new DateTime(2026, 5, 27)
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("Log_202605", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_ByYear()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Year,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<TestLog>(config);

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = new DateTime(2026, 5, 27)
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("Log_2026", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableNames_WithTimeRange()
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
            ShardingManager.Configure<TestLog>(config);

            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 3, 1) },
                { "CreateTime_End", new DateTime(2026, 5, 31) }
            };

            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            Assert.Contains("Log_202603", tableNames);
            Assert.Contains("Log_202604", tableNames);
            Assert.Contains("Log_202605", tableNames);
            Assert.DoesNotContain("Log_202601", tableNames);
            Assert.DoesNotContain("Log_202606", tableNames);
        }

        [Fact]
        public void TimeSharding_GetAllTableNames()
        {
            ConfigureTimeSharding(TimeGranularity.Month);

            var tableNames = ShardingManager.GetAllTableNames<ShardingTestEntity>();

            Assert.NotEmpty(tableNames);
        }

        #endregion

        #region 哈希分表测试

        [Fact]
        public void HashSharding_GetTableName()
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
            ShardingManager.Configure<TestOrder>(config);

            var entity = new TestOrder
            {
                Id = 1,
                OrderNo = "ORD20260527001"
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.StartsWith("Order_", tableName);
            Assert.Equal(10, tableName.Length);
        }

        [Fact]
        public void HashSharding_GetTableNames_WithHashField()
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
            ShardingManager.Configure<TestOrder>(config);

            var queryParams = new Dictionary<string, object>
            {
                { "OrderNo", "ORD20260527001" }
            };

            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            Assert.Single(tableNames);
        }

        [Fact]
        public void HashSharding_GetAllTableNames()
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
            ShardingManager.Configure<TestOrder>(config);

            var tableNames = ShardingManager.GetAllTableNames<TestOrder>();

            Assert.Equal(4, tableNames.Count);
            Assert.Contains("Order_0000", tableNames);
            Assert.Contains("Order_0001", tableNames);
            Assert.Contains("Order_0002", tableNames);
            Assert.Contains("Order_0003", tableNames);
        }

        [Fact]
        public void HashSharding_GetAllTableNames_ReturnsCorrectCount()
        {
            ConfigureHashSharding(8);

            var tableNames = ShardingManager.GetAllTableNames<ShardingTestEntity>();

            Assert.Equal(8, tableNames.Count);
        }

        #endregion

        #region 列表分表测试

        [Fact]
        public void ListSharding_GetTableName()
        {
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
            ShardingManager.Configure<TestOrder>(config);

            var entity = new TestOrder
            {
                Id = 1,
                OrderNo = "ORD001",
                Status = "Completed"
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("Order_completed", tableName);
        }

        [Fact]
        public void ListSharding_GetTableNames_WithMultipleValues()
        {
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
                        { "Completed", "completed" }
                    }
                }
            };
            ShardingManager.Configure<TestOrder>(config);

            var queryParams = new Dictionary<string, object>
            {
                { "Status", "Pending,Processing" }
            };

            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            Assert.Equal(2, tableNames.Count);
            Assert.Contains("Order_pending", tableNames);
            Assert.Contains("Order_processing", tableNames);
        }

        [Fact]
        public void ListSharding_GetAllTableNames()
        {
            ConfigureListSharding();

            var tableNames = ShardingManager.GetAllTableNames<ShardingTestOrder>();

            Assert.NotEmpty(tableNames);
        }

        #endregion

        #region 组合键分表测试

        [Fact]
        public void CompositeSharding_GetTableName()
        {
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
            ShardingManager.Configure<TestOrder>(config);

            var entity = new TestOrder
            {
                Id = 1,
                OrderNo = "ORD001",
                Region = "Beijing",
                CustomerType = "VIP"
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.StartsWith("Order_", tableName);
            Assert.Equal(10, tableName.Length);
        }

        [Fact]
        public void CompositeSharding_GetTableNames_WithAllFields()
        {
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
            ShardingManager.Configure<TestOrder>(config);

            var queryParams = new Dictionary<string, object>
            {
                { "Region", "Beijing" },
                { "CustomerType", "VIP" }
            };

            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            Assert.Single(tableNames);
        }

        #endregion

        #region 查询频率分表测试

        [Fact]
        public void QueryFrequencySharding_GetTableName_HotData()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 10,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold"
                }
            };
            ShardingManager.Configure<TestLog>(config);

            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");
            for (int i = 0; i < 10; i++)
            {
                QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            }

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = DateTime.Now,
                UserId = "user123"
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("UserLog_hot", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableName_ColdData()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 10,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.Single
                }
            };
            ShardingManager.Configure<TestLog>(config);

            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = DateTime.Now,
                UserId = "user456"
            };

            var tableName = ShardingManager.GetTableName(entity);

            Assert.Equal("UserLog_cold", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableNames_WithHotData()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 5,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold"
                }
            };
            ShardingManager.Configure<TestLog>(config);

            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");

            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "user123" }
            };

            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            Assert.Single(tableNames);
            Assert.Contains("UserLog_hot", tableNames);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableNames_WithColdData()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 10,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.Single
                }
            };
            ShardingManager.Configure<TestLog>(config);

            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "user789" }
            };

            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            Assert.Single(tableNames);
            Assert.Contains("UserLog_cold", tableNames);
        }

        [Fact]
        public void QueryFrequencySharding_GetQueryFrequency()
        {
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");

            var frequency = QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", "user123");

            Assert.Equal(3, frequency);
        }

        [Fact]
        public void QueryFrequencySharding_GetHotDataValues()
        {
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user2");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user3");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user3");

            var hotValues = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 3);

            Assert.Single(hotValues);
            Assert.Contains("user1", hotValues);
        }

        #endregion

        #region 分表管理器测试

        [Fact]
        public void ShardingManager_Configure_And_GetConfig()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Day
                }
            };

            ShardingManager.Configure<TestLog>(config);
            var result = ShardingManager.GetConfig<TestLog>();

            Assert.NotNull(result);
            Assert.Equal("Log", result.BaseTableName);
            Assert.Equal(ShardingType.Time, result.ShardingType);
        }

        [Fact]
        public void ShardingManager_IsShardingEnabled()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Day
                }
            };

            Assert.False(ShardingManager.IsShardingEnabled<TestLog>());
            ShardingManager.Configure<TestLog>(config);

            Assert.True(ShardingManager.IsShardingEnabled<TestLog>());
        }

        [Fact]
        public void ShardingManager_Clear()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = TimeGranularity.Day
                }
            };
            ShardingManager.Configure<TestLog>(config);

            ShardingManager.Clear();

            Assert.False(ShardingManager.IsShardingEnabled<TestLog>());
        }

        #endregion

        #region 配置测试

        [Fact]
        public void Configure_TimeSharding_Success()
        {
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
            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
            var result = ShardingManager.GetConfig<ShardingTestEntity>();
            Assert.NotNull(result);
            Assert.Equal("UserLog", result.BaseTableName);
            Assert.Equal(ShardingType.Time, result.ShardingType);
        }

        [Fact]
        public void Configure_HashSharding_Success()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "OrderNo",
                    ShardCount = 8
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
            var result = ShardingManager.GetConfig<ShardingTestEntity>();
            Assert.Equal(ShardingType.Hash, result.ShardingType);
        }

        [Fact]
        public void Configure_ListSharding_Success()
        {
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
                        { "Completed", "completed" }
                    }
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_CompositeSharding_Success()
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
            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_QueryFrequencySharding_Success()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = 100,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold"
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_MultipleEntities_Independent()
        {
            var config1 = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig { TimeField = "CreateTime", Granularity = TimeGranularity.Day }
            };
            var config2 = new ShardingConfig
            {
                BaseTableName = "Order",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig { HashField = "OrderNo", ShardCount = 4 }
            };

            ShardingManager.Configure<ShardingTestEntity>(config1);
            ShardingManager.Configure<ShardingTestOrder>(config2);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestOrder>());
            Assert.Equal(ShardingType.Time, ShardingManager.GetConfig<ShardingTestEntity>().ShardingType);
            Assert.Equal(ShardingType.Hash, ShardingManager.GetConfig<ShardingTestOrder>().ShardingType);
        }

        #endregion

        #region 启用/禁用测试

        [Fact]
        public void IsShardingEnabled_NotConfigured_ReturnsFalse()
        {
            Assert.False(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void IsShardingEnabled_Configured_ReturnsTrue()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig { TimeField = "CreateTime", Granularity = TimeGranularity.Day }
            };

            ShardingManager.Configure<ShardingTestEntity>(config);

            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Clear_DisablesAllSharding()
        {
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig { TimeField = "CreateTime", Granularity = TimeGranularity.Day }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);

            ShardingManager.Clear();

            Assert.False(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        #endregion

        #region 策略注册测试

        [Fact]
        public void Strategy_Register_CustomStrategy()
        {
            var customStrategy = new TestShardingStrategy();

            ShardingManager.RegisterStrategy(customStrategy);
            var result = ShardingManager.GetStrategy(ShardingType.Geo);

            Assert.NotNull(result);
            Assert.Equal("TestSharding", result.Name);
        }

        #endregion

        #region 链式 API 测试

        [Fact]
        public void ChainableApi_UseSharding_EnablesSharding()
        {
            ConfigureTimeSharding(TimeGranularity.Month);

            var query = new DataQuery<ShardingTestEntity>();
            query.UseSharding();

            Assert.True(query.EnableSharding);
        }

        [Fact]
        public void ChainableApi_WithTimeRange_SetsParams()
        {
            ConfigureTimeSharding(TimeGranularity.Month);
            var startTime = new DateTime(2026, 1, 1);
            var endTime = new DateTime(2026, 12, 31);

            var query = new DataQuery<ShardingTestEntity>();
            query.WithTimeRange("CreateTime", startTime, endTime);

            Assert.True(query.EnableSharding);
            Assert.Equal(startTime, query.ShardingQueryParams["CreateTime_Start"]);
            Assert.Equal(endTime, query.ShardingQueryParams["CreateTime_End"]);
        }

        [Fact]
        public void ChainableApi_WithHashField_SetsParams()
        {
            ConfigureHashSharding(4);

            var query = new DataQuery<ShardingTestEntity>();
            query.WithHashField("Name", "TestValue");

            Assert.True(query.EnableSharding);
            Assert.Equal("TestValue", query.ShardingQueryParams["Name"]);
        }

        [Fact]
        public void ChainableApi_WithListField_SetsParams()
        {
            ConfigureListSharding();

            var query = new DataQuery<ShardingTestOrder>();
            query.WithListField("Status", "Pending");

            Assert.True(query.EnableSharding);
            Assert.Equal("Pending", query.ShardingQueryParams["Status"]);
        }

        [Fact]
        public void ChainableApi_WithShardingConfig_OverridesConfig()
        {
            var customConfig = new ShardingConfig
            {
                BaseTableName = "CustomLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreatedAt",
                    Granularity = TimeGranularity.Day
                }
            };

            var query = new DataQuery<ShardingTestEntity>();
            query.WithShardingConfig(customConfig);

            Assert.NotNull(query.ShardingConfigOverride);
            Assert.Equal("CustomLog", query.ShardingConfigOverride.BaseTableName);
        }

        [Fact]
        public void ChainableApi_Chaining_MultipleMethods()
        {
            ConfigureTimeSharding(TimeGranularity.Month);

            var query = new DataQuery<ShardingTestEntity>();
            var result = query
                .UseSharding()
                .WithTimeRange("CreateTime", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
                .WithShardingParam("Level", "Error");

            Assert.True(result.EnableSharding);
            Assert.Equal(3, result.ShardingQueryParams.Count);
        }

        [Fact]
        public void ChainableApi_NotUseSharding_DisabledByDefault()
        {
            var query = new DataQuery<ShardingTestEntity>();

            Assert.False(query.EnableSharding);
        }

        #endregion

        #region 辅助方法

        private void ConfigureTimeSharding(TimeGranularity granularity)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreateTime",
                    Granularity = granularity,
                    StartTime = new DateTime(2026, 1, 1)
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);
        }

        private void ConfigureHashSharding(int shardCount)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.Hash,
                HashConfig = new HashShardingConfig
                {
                    HashField = "Name",
                    ShardCount = shardCount
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);
        }

        private void ConfigureListSharding()
        {
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
                        { "Completed", "completed" }
                    }
                }
            };
            ShardingManager.Configure<ShardingTestOrder>(config);
        }

        private void ConfigureCompositeSharding()
        {
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
            ShardingManager.Configure<ShardingTestOrder>(config);
        }

        private void ConfigureQueryFrequencySharding(long threshold)
        {
            var config = new ShardingConfig
            {
                BaseTableName = "UserLog",
                ShardingType = ShardingType.QueryFrequency,
                FrequencyConfig = new QueryFrequencyShardingConfig
                {
                    Field = "UserId",
                    HotThreshold = threshold,
                    HotSuffix = "_hot",
                    ColdSuffix = "_cold",
                    ColdShardingType = ColdShardingType.Single
                }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);
        }

        #endregion
    }

    /// <summary>
    /// 测试用分表策略
    /// </summary>
    internal class TestShardingStrategy : IShardingStrategy
    {
        public string Name => "TestSharding";
        public ShardingType Type => ShardingType.Geo;

        public string GetTableName(ShardingConfig config, object entity)
        {
            return $"{config.BaseTableName}_test";
        }

        public List<string> GetTableNames(ShardingConfig config, Dictionary<string, object> queryParams)
        {
            return new List<string> { $"{config.BaseTableName}_test" };
        }

        public List<string> GetAllTableNames(ShardingConfig config)
        {
            return new List<string> { $"{config.BaseTableName}_test" };
        }

        public bool CreateTable(ShardingConfig config, string tableName)
        {
            return true;
        }
    }
}
