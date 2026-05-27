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
    /// 分表 CRUD 功能完整测试
    /// 验证分表的配置、启用/禁用、增删改查功能
    /// </summary>
    public class ShardingCrudTests : IDisposable
    {
        public ShardingCrudTests()
        {
            // 清除之前的配置
            ShardingManager.Clear();
        }

        public void Dispose()
        {
            ShardingManager.Clear();
        }

        #region 配置测试

        [Fact]
        public void Configure_TimeSharding_Success()
        {
            // Arrange & Act
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

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
            var result = ShardingManager.GetConfig<ShardingTestEntity>();
            Assert.NotNull(result);
            Assert.Equal("UserLog", result.BaseTableName);
            Assert.Equal(ShardingType.Time, result.ShardingType);
        }

        [Fact]
        public void Configure_HashSharding_Success()
        {
            // Arrange & Act
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

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
            var result = ShardingManager.GetConfig<ShardingTestEntity>();
            Assert.Equal(ShardingType.Hash, result.ShardingType);
        }

        [Fact]
        public void Configure_ListSharding_Success()
        {
            // Arrange & Act
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

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_CompositeSharding_Success()
        {
            // Arrange & Act
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

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_QueryFrequencySharding_Success()
        {
            // Arrange & Act
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

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Configure_MultipleEntities_Independent()
        {
            // Arrange
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

            // Act
            ShardingManager.Configure<ShardingTestEntity>(config1);
            ShardingManager.Configure<ShardingTestOrder>(config2);

            // Assert
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
            // Act & Assert
            Assert.False(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void IsShardingEnabled_Configured_ReturnsTrue()
        {
            // Arrange
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig { TimeField = "CreateTime", Granularity = TimeGranularity.Day }
            };

            // Act
            ShardingManager.Configure<ShardingTestEntity>(config);

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        [Fact]
        public void Clear_DisablesAllSharding()
        {
            // Arrange
            var config = new ShardingConfig
            {
                BaseTableName = "Log",
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig { TimeField = "CreateTime", Granularity = TimeGranularity.Day }
            };
            ShardingManager.Configure<ShardingTestEntity>(config);

            // Act
            ShardingManager.Clear();

            // Assert
            Assert.False(ShardingManager.IsShardingEnabled<ShardingTestEntity>());
        }

        #endregion

        #region 时间分表 CRUD 测试

        [Fact]
        public void TimeSharding_GetTableName_Day()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Day);
            var entity = new ShardingTestEntity
            {
                Id = 1,
                CreateTime = new DateTime(2026, 5, 27)
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_20260527", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_Month()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);
            var entity = new ShardingTestEntity
            {
                Id = 1,
                CreateTime = new DateTime(2026, 5, 27)
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_202605", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_Year()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Year);
            var entity = new ShardingTestEntity
            {
                Id = 1,
                CreateTime = new DateTime(2026, 5, 27)
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_2026", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableNames_WithTimeRange()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);
            var queryParams = new Dictionary<string, object>
            {
                { "CreateTime_Start", new DateTime(2026, 3, 1) },
                { "CreateTime_End", new DateTime(2026, 5, 31) }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<ShardingTestEntity>(queryParams);

            // Assert
            Assert.Contains("UserLog_202603", tableNames);
            Assert.Contains("UserLog_202604", tableNames);
            Assert.Contains("UserLog_202605", tableNames);
        }

        [Fact]
        public void TimeSharding_GetAllTableNames()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);

            // Act
            var tableNames = ShardingManager.GetAllTableNames<ShardingTestEntity>();

            // Assert
            Assert.NotEmpty(tableNames);
        }

        #endregion

        #region 哈希分表 CRUD 测试

        [Fact]
        public void HashSharding_GetTableName()
        {
            // Arrange
            ConfigureHashSharding(4);
            var entity = new ShardingTestEntity
            {
                Id = 1,
                Name = "Test"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.StartsWith("UserLog_", tableName);
        }

        [Fact]
        public void HashSharding_GetTableNames_WithHashField()
        {
            // Arrange
            ConfigureHashSharding(4);
            var queryParams = new Dictionary<string, object>
            {
                { "Name", "TestValue" }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<ShardingTestEntity>(queryParams);

            // Assert
            Assert.Single(tableNames);
        }

        [Fact]
        public void HashSharding_GetAllTableNames_ReturnsCorrectCount()
        {
            // Arrange
            ConfigureHashSharding(8);

            // Act
            var tableNames = ShardingManager.GetAllTableNames<ShardingTestEntity>();

            // Assert
            Assert.Equal(8, tableNames.Count);
        }

        #endregion

        #region 列表分表 CRUD 测试

        [Fact]
        public void ListSharding_GetTableName()
        {
            // Arrange
            ConfigureListSharding();
            var entity = new ShardingTestOrder
            {
                Id = 1,
                Status = "Completed"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("Order_completed", tableName);
        }

        [Fact]
        public void ListSharding_GetTableNames_WithMultipleValues()
        {
            // Arrange
            ConfigureListSharding();
            var queryParams = new Dictionary<string, object>
            {
                { "Status", "Pending,Processing" }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<ShardingTestOrder>(queryParams);

            // Assert
            Assert.Equal(2, tableNames.Count);
            Assert.Contains("Order_pending", tableNames);
            Assert.Contains("Order_processing", tableNames);
        }

        [Fact]
        public void ListSharding_GetAllTableNames()
        {
            // Arrange
            ConfigureListSharding();

            // Act
            var tableNames = ShardingManager.GetAllTableNames<ShardingTestOrder>();

            // Assert
            Assert.NotEmpty(tableNames);
        }

        #endregion

        #region 组合键分表 CRUD 测试

        [Fact]
        public void CompositeSharding_GetTableName()
        {
            // Arrange
            ConfigureCompositeSharding();
            var entity = new ShardingTestOrder
            {
                Id = 1,
                Region = "Beijing",
                CustomerType = "VIP"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.StartsWith("Order_", tableName);
        }

        [Fact]
        public void CompositeSharding_GetTableNames_WithAllFields()
        {
            // Arrange
            ConfigureCompositeSharding();
            var queryParams = new Dictionary<string, object>
            {
                { "Region", "Beijing" },
                { "CustomerType", "VIP" }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<ShardingTestOrder>(queryParams);

            // Assert
            Assert.Single(tableNames);
        }

        #endregion

        #region 查询频率分表 CRUD 测试

        [Fact]
        public void QueryFrequencySharding_GetTableName_HotData()
        {
            // Arrange
            ConfigureQueryFrequencySharding(10);
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            // 模拟热数据
            for (int i = 0; i < 15; i++)
            {
                QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            }

            var entity = new ShardingTestEntity
            {
                Id = 1,
                UserId = "user123"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_hot", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableName_ColdData()
        {
            // Arrange
            ConfigureQueryFrequencySharding(10);
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            var entity = new ShardingTestEntity
            {
                Id = 1,
                UserId = "user456"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_cold", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_RecordQuery_IncrementsFrequency()
        {
            // Arrange
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            // Act
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");

            // Assert
            Assert.Equal(3, QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", "user123"));
        }

        [Fact]
        public void QueryFrequencySharding_GetHotDataValues()
        {
            // Arrange
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user1");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user2");

            // Act
            var hotValues = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 3);

            // Assert
            Assert.Single(hotValues);
            Assert.Contains("user1", hotValues);
        }

        #endregion

        #region 链式 API 分表测试

        [Fact]
        public void ChainableApi_UseSharding_EnablesSharding()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);

            // Act
            var query = new DataQuery<ShardingTestEntity>();
            query.UseSharding();

            // Assert
            Assert.True(query.EnableSharding);
        }

        [Fact]
        public void ChainableApi_WithTimeRange_SetsParams()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);
            var startTime = new DateTime(2026, 1, 1);
            var endTime = new DateTime(2026, 12, 31);

            // Act
            var query = new DataQuery<ShardingTestEntity>();
            query.WithTimeRange("CreateTime", startTime, endTime);

            // Assert
            Assert.True(query.EnableSharding);
            Assert.Equal(startTime, query.ShardingQueryParams["CreateTime_Start"]);
            Assert.Equal(endTime, query.ShardingQueryParams["CreateTime_End"]);
        }

        [Fact]
        public void ChainableApi_WithHashField_SetsParams()
        {
            // Arrange
            ConfigureHashSharding(4);

            // Act
            var query = new DataQuery<ShardingTestEntity>();
            query.WithHashField("Name", "TestValue");

            // Assert
            Assert.True(query.EnableSharding);
            Assert.Equal("TestValue", query.ShardingQueryParams["Name"]);
        }

        [Fact]
        public void ChainableApi_WithListField_SetsParams()
        {
            // Arrange
            ConfigureListSharding();

            // Act
            var query = new DataQuery<ShardingTestOrder>();
            query.WithListField("Status", "Pending");

            // Assert
            Assert.True(query.EnableSharding);
            Assert.Equal("Pending", query.ShardingQueryParams["Status"]);
        }

        [Fact]
        public void ChainableApi_WithShardingConfig_OverridesConfig()
        {
            // Arrange
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

            // Act
            var query = new DataQuery<ShardingTestEntity>();
            query.WithShardingConfig(customConfig);

            // Assert
            Assert.NotNull(query.ShardingConfigOverride);
            Assert.Equal("CustomLog", query.ShardingConfigOverride.BaseTableName);
        }

        [Fact]
        public void ChainableApi_Chaining_MultipleMethods()
        {
            // Arrange
            ConfigureTimeSharding(TimeGranularity.Month);

            // Act
            var query = new DataQuery<ShardingTestEntity>();
            var result = query
                .UseSharding()
                .WithTimeRange("CreateTime", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
                .WithShardingParam("Level", "Error");

            // Assert
            Assert.True(result.EnableSharding);
            Assert.Equal(3, result.ShardingQueryParams.Count); // CreateTime_Start, CreateTime_End, Level
        }

        [Fact]
        public void ChainableApi_NotUseSharding_DisabledByDefault()
        {
            // Act
            var query = new DataQuery<ShardingTestEntity>();

            // Assert
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
    /// 测试用实体类
    /// </summary>
    public class ShardingTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
        public string UserId { get; set; }
        public string Level { get; set; }
    }

    /// <summary>
    /// 测试用订单实体类
    /// </summary>
    public class ShardingTestOrder
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public string CustomerType { get; set; }
    }
}
