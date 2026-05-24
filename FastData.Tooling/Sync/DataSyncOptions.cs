using System;

namespace FastData.Tooling.Sync
{
    public class DataSyncOptions
    {
        public string SourceProvider { get; set; }

        public string SourceConnectionString { get; set; }

        public string TargetProvider { get; set; }

        public string TargetConnectionString { get; set; }

        public string IntermediateProvider { get; set; }

        public string IntermediateConnectionString { get; set; }

        public string TaskId { get; set; }

        public string SourceTable { get; set; }

        public string TargetTable { get; set; }

        public string PrimaryKeyColumns { get; set; }

        public bool IsAutoIncrementKey { get; set; }

        /// <summary>
        /// 时间字段（可选）
        /// </summary>
        public string TimeColumn { get; set; }

        /// <summary>
        /// 是否启用时间范围
        /// </summary>
        public bool EnableTimeRange { get; set; }

        /// <summary>
        /// 首次同步时是否全量
        /// </summary>
        public bool IsFullSyncForFirstTime { get; set; } = true;

        /// <summary>
        /// 同步结束时间（默认当前时间）
        /// </summary>
        public DateTime? EndTime { get; set; }

        public int BatchSize { get; set; }

        public int RetryCount { get; set; }

        public bool CleanIntermediateData { get; set; }

        public bool AutoCreateIntermediateSchema { get; set; }

        public bool ResumeFailedRecords { get; set; }

        public bool EnableTimer { get; set; }

        public int SyncIntervalSeconds { get; set; }

        public PrimaryKeyConfigService PrimaryKeyConfigService { get; set; }

        public SyncConfigManager ConfigManager { get; set; }

        public int RangeDays { get; set; } = 3;
    }
}
