using System;
using System.Collections.Generic;
using FastData.Sharding;
using FastData.Sharding.Strategies;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 测试用实体类
    /// </summary>
    public class TestLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
        public string LogLevel { get; set; }
        public string Module { get; set; }
        public string UserId { get; set; }
    }

    /// <summary>
    /// 测试用实体类
    /// </summary>
    public class TestOrder
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
        public string CustomerType { get; set; }
    }

    /// <summary>
    /// 分表功能测试
    /// </summary>
    public class ShardingTests : IDisposable
    {
        public ShardingTests()
        {
            // 清除之前的配置
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
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("Log_20260527", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_ByMonth()
        {
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("Log_202605", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableName_ByYear()
        {
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("Log_2026", tableName);
        }

        [Fact]
        public void TimeSharding_GetTableNames_WithTimeRange()
        {
            // Arrange
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

            // Act
            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            // Assert
            Assert.Contains("Log_202603", tableNames);
            Assert.Contains("Log_202604", tableNames);
            Assert.Contains("Log_202605", tableNames);
            Assert.DoesNotContain("Log_202601", tableNames);
            Assert.DoesNotContain("Log_202606", tableNames);
        }

        #endregion

        #region 哈希分表测试

        [Fact]
        public void HashSharding_GetTableName()
        {
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.StartsWith("Order_", tableName);
            Assert.Equal(10, tableName.Length); // Order_ + 4 digits
        }

        [Fact]
        public void HashSharding_GetTableNames_WithHashField()
        {
            // Arrange
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

            // Act
            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            // Assert
            Assert.Single(tableNames);
        }

        [Fact]
        public void HashSharding_GetAllTableNames()
        {
            // Arrange
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

            // Act
            var tableNames = ShardingManager.GetAllTableNames<TestOrder>();

            // Assert
            Assert.Equal(4, tableNames.Count);
            Assert.Contains("Order_0000", tableNames);
            Assert.Contains("Order_0001", tableNames);
            Assert.Contains("Order_0002", tableNames);
            Assert.Contains("Order_0003", tableNames);
        }

        #endregion

        #region 列表分表测试

        [Fact]
        public void ListSharding_GetTableName()
        {
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("Order_completed", tableName);
        }

        [Fact]
        public void ListSharding_GetTableNames_WithMultipleValues()
        {
            // Arrange
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

            // Act
            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            // Assert
            Assert.Equal(2, tableNames.Count);
            Assert.Contains("Order_pending", tableNames);
            Assert.Contains("Order_processing", tableNames);
        }

        #endregion

        #region 组合键分表测试

        [Fact]
        public void CompositeSharding_GetTableName()
        {
            // Arrange
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.StartsWith("Order_", tableName);
            Assert.Equal(10, tableName.Length); // Order_ + 4 digits
        }

        [Fact]
        public void CompositeSharding_GetTableNames_WithAllFields()
        {
            // Arrange
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

            // Act
            var tableNames = ShardingManager.GetTableNames<TestOrder>(queryParams);

            // Assert
            Assert.Single(tableNames);
        }

        #endregion

        #region 分表管理器测试

        [Fact]
        public void ShardingManager_Configure_And_GetConfig()
        {
            // Arrange
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

            // Act
            ShardingManager.Configure<TestLog>(config);
            var result = ShardingManager.GetConfig<TestLog>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Log", result.BaseTableName);
            Assert.Equal(ShardingType.Time, result.ShardingType);
        }

        [Fact]
        public void ShardingManager_IsShardingEnabled()
        {
            // Arrange
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

            // Act
            Assert.False(ShardingManager.IsShardingEnabled<TestLog>());
            ShardingManager.Configure<TestLog>(config);

            // Assert
            Assert.True(ShardingManager.IsShardingEnabled<TestLog>());
        }

        [Fact]
        public void ShardingManager_Clear()
        {
            // Arrange
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

            // Act
            ShardingManager.Clear();

            // Assert
            Assert.False(ShardingManager.IsShardingEnabled<TestLog>());
        }

        #endregion

        #region 策略注册测试

        [Fact]
        public void Strategy_Register_CustomStrategy()
        {
            // Arrange
            var customStrategy = new TestShardingStrategy();

            // Act
            ShardingManager.RegisterStrategy(customStrategy);
            var result = ShardingManager.GetStrategy(ShardingType.Geo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestSharding", result.Name);
        }

        #endregion

        #region 查询频率分表测试

        [Fact]
        public void QueryFrequencySharding_GetTableName_HotData()
        {
            // Arrange
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

            // 记录查询频率（模拟热数据）
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

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_hot", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableName_ColdData()
        {
            // Arrange
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

            // 重置频率统计
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            var entity = new TestLog
            {
                Id = 1,
                Message = "Test",
                CreateTime = DateTime.Now,
                UserId = "user456"
            };

            // Act
            var tableName = ShardingManager.GetTableName(entity);

            // Assert
            Assert.Equal("UserLog_cold", tableName);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableNames_WithHotData()
        {
            // Arrange
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

            // 记录查询频率
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");

            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "user123" }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("UserLog_hot", tableNames);
        }

        [Fact]
        public void QueryFrequencySharding_GetTableNames_WithColdData()
        {
            // Arrange
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

            // 重置频率统计
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            var queryParams = new Dictionary<string, object>
            {
                { "UserId", "user789" }
            };

            // Act
            var tableNames = ShardingManager.GetTableNames<TestLog>(queryParams);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("UserLog_cold", tableNames);
        }

        [Fact]
        public void QueryFrequencySharding_GetQueryFrequency()
        {
            // Arrange
            QueryFrequencyShardingStrategy.ResetFrequencyStats("UserId");

            // Act
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user123");

            var frequency = QueryFrequencyShardingStrategy.GetQueryFrequency("UserId", "user123");

            // Assert
            Assert.Equal(3, frequency);
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
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user3");
            QueryFrequencyShardingStrategy.RecordQuery("UserId", "user3");

            // Act
            var hotValues = QueryFrequencyShardingStrategy.GetHotDataValues("UserId", 3);

            // Assert
            Assert.Single(hotValues);
            Assert.Contains("user1", hotValues);
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
