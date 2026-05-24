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

        public string IncrementalColumn { get; set; }

        public string LastValue { get; set; }

        public string PrimaryKeyColumns { get; set; }

        public bool IsAutoIncrementKey { get; set; }

        public int BatchSize { get; set; }

        public int RetryCount { get; set; }

        public bool CleanIntermediateData { get; set; }

        public bool AutoCreateIntermediateSchema { get; set; }

        public bool ResumeFailedRecords { get; set; }

        public bool EnableTimer { get; set; }

        public int SyncIntervalSeconds { get; set; }

        public PrimaryKeyConfigService PrimaryKeyConfigService { get; set; }
    }
}
