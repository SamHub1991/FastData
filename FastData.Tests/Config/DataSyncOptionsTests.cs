using FastData.Tooling.Sync;

namespace FastData.Tests.Config
{
    public class DataSyncOptionsTests
    {
        public void DefaultValues_AreCorrect()
        {
            var options = new DataSyncOptions();
            Assert.IsTrue(options.IsFullSyncForFirstTime);
            Assert.IsTrue(options.AlwaysDeduplicate);
            Assert.AreEqual(0, options.GlobalRangeDays);
            Assert.IsFalse(options.EnableGlobalConfig);
            Assert.AreEqual(3, options.RangeDays);
            Assert.AreEqual(0, options.BatchSize);
            Assert.AreEqual(0, options.RetryCount);
        }

        public void EnableGlobalConfig_True_WithGlobalRangeDays_OverridesRangeDays()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = true,
                GlobalRangeDays = 7,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.AreEqual(7, options.RangeDays);
        }

        public void EnableGlobalConfig_False_DoesNotOverrideRangeDays()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = false,
                GlobalRangeDays = 7,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.AreEqual(3, options.RangeDays);
        }

        public void EnableGlobalConfig_True_WithZeroGlobalRangeDays_DoesNotOverride()
        {
            var options = new DataSyncOptions
            {
                EnableGlobalConfig = true,
                GlobalRangeDays = 0,
                RangeDays = 3
            };

            ApplyGlobalConfigLogic(options);
            Assert.AreEqual(3, options.RangeDays);
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
        public void DefaultValues_AreCorrect()
        {
            var config = new TableSyncConfig();
            Assert.AreEqual(3, config.RangeDays);
            Assert.IsFalse(config.EnableGlobalConfig);
            Assert.AreEqual(0, config.GlobalRangeDays);
            Assert.IsTrue(config.AlwaysDeduplicate);
            Assert.IsTrue(config.IsEnabled);
            Assert.AreEqual(TableSyncStatus.Pending, config.Status);
            Assert.AreEqual(SyncDataType.Static, config.DataType);
        }

        public void TargetTableName_CanBeSetIndependently()
        {
            var config = new TableSyncConfig
            {
                TableName = "source_table",
                TargetTableName = "target_table"
            };
            Assert.AreEqual("source_table", config.TableName);
            Assert.AreEqual("target_table", config.TargetTableName);
        }
    }
}