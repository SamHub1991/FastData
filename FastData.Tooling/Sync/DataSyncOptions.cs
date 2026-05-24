namespace FastData.Tooling.Sync
{
    public class DataSyncOptions
    {
        public string SourceProvider { get; set; }

        public string SourceConnectionString { get; set; }

        public string TargetProvider { get; set; }

        public string TargetConnectionString { get; set; }

        public string SourceTable { get; set; }

        public string TargetTable { get; set; }

        public int BatchSize { get; set; }
    }
}
