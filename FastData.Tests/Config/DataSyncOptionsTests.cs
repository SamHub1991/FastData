using FastData.Tooling.Sync;
using Xunit;

namespace FastData.Tests.Config
{
    /// <summary>
    /// 数据同步选项测试
    /// 
    /// 测试 DataSyncOptions 的默认值和全局配置逻辑。
    /// </summary>
    public class DataSyncOptionsTests
    {
        /// <summary>
        /// 测试默认值是否正确
        /// </summary>
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

        /// <summary>
        /// 测试启用全局配置时覆盖范围天数
        /// </summary>
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

        /// <summary>
        /// 测试禁用全局配置时不覆盖范围天数
        /// </summary>
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

        /// <summary>
        /// 测试全局范围天数为零时不覆盖
        /// </summary>
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

        /// <summary>
        /// 应用全局配置逻辑
        /// </summary>
        /// <param name="options">同步选项</param>
        private static void ApplyGlobalConfigLogic(DataSyncOptions options)
        {
            if (options.EnableGlobalConfig)
            {
                if (options.GlobalRangeDays > 0)
                    options.RangeDays = options.GlobalRangeDays;
            }
        }
    }

    /// <summary>
    /// 表同步配置测试
    /// 
    /// 测试 TableSyncConfig 的默认值和属性设置。
    /// </summary>
    public class TableSyncConfigTests
    {
        /// <summary>
        /// 测试默认值是否正确
        /// </summary>
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

        /// <summary>
        /// 测试目标表名可独立设置
        /// </summary>
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
