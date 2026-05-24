namespace FastData.Tooling.Sync
{
    public class DataSyncResult
    {
        public int ReadCount { get; set; }

        public int WriteCount { get; set; }

        public int FailedCount { get; set; }

        public int RetryCount { get; set; }

        public int RecoveredCount { get; set; }

        public string Message { get; set; }
    }
}
