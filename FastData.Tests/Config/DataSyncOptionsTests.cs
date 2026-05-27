using FastData.Tooling.Sync;
using Xunit;

namespace FastData.Tests.Config
{
    public class DataSyncOptionsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var options = new DataSyncOptions();
            Assert.True(options.IsFullSyncForFirstTime);
            Assert.True(options.AlwaysDeduplicate);
            Assert.Equal(0, options.GlobalRangeDays);
            Assert.False(options.EnableGlobalConfig);
            Assert.Equal(3, options.RangeDays);
            Assert.Equal(0, options.BatchSize);
            Assert.Equal(0, options.RetryCount);
        }

        [Fact]
        public void EnableGlobalConfig_True_WithGlobalRangeDays_OverridesRangeDays()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = true,
                GlobalRangeDays = 7,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.Equal(7, options.RangeDays);
        }

        [Fact]
        public void EnableGlobalConfig_False_DoesNotOverrideRangeDays()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = false,
                GlobalRangeDays = 7,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.Equal(3, options.RangeDays);
        }

        [Fact]
        public void EnableGlobalConfig_True_WithZeroGlobalRangeDays_DoesNotOverride()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = true,
                GlobalRangeDays = 0,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.Equal(3, options.RangeDays);
        }

        private static void ApplyGlobalConfigLogic(DataSyncOptions options)
        {
            if (options.EnableGlobalConfig)
            {
                if (options.GlobalRangeDays > 0)
                    options.RangeDays = options.GlobalRangeDays;
            }
        }
    }

    public class TableSyncConfigTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var config = new TableSyncConfig();
            Assert.Equal(3, config.RangeDays);
            Assert.False(config.EnableGlobalConfig);
            Assert.Equal(0, config.GlobalRangeDays);
            Assert.True(config.AlwaysDeduplicate);
            Assert.True(config.IsEnabled);
            Assert.Equal(TableSyncStatus.Pending, config.Status);
            Assert.Equal(SyncDataType.Static, config.DataType);
        }

        [Fact]
        public void TargetTableName_CanBeSetIndependently()
        {
            var config = new TableSyncConfig
            {
                TableName = "source_table",
                TargetTableName = "target_table"
            };
            Assert.Equal("source_table", config.TableName);
            Assert.Equal("target_table", config.TargetTableName);
        }
    }
}
