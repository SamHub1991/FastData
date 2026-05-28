using FastData.Tooling.Sync;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Services
{
    public interface IDataSyncService
    {
        Task<SyncResult> SyncUsersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);
        Task<SyncResult> SyncAllTablesAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);
        Task<SyncResult> SyncOrdersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);
    }

    public class DataSyncService : IDataSyncService
    {
        public async Task<SyncResult> SyncUsersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var options = BuildOptions("AppUser", "AppUser", sourceProvider, sourceConnStr, targetProvider, targetConnStr);
            return await Task.Run(() => ExecuteSync(options));
        }

        public async Task<SyncResult> SyncAllTablesAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var results = new SyncResult();
            var tables = new[] { "AppUser", "AppOrder" };

            foreach (var table in tables)
            {
                var options = BuildOptions(table, table, sourceProvider, sourceConnStr, targetProvider, targetConnStr);
                var result = await Task.Run(() => ExecuteSync(options));
                results.ReadCount += result.ReadCount;
                results.WriteCount += result.WriteCount;
                results.FailedCount += result.FailedCount;
            }

            results.EndTime = DateTime.Now;
            return results;
        }

        public async Task<SyncResult> SyncOrdersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var options = BuildOptions("AppOrder", "AppOrder", sourceProvider, sourceConnStr, targetProvider, targetConnStr);
            return await Task.Run(() => ExecuteSync(options));
        }

        private DataSyncOptions BuildOptions(string sourceTable, string targetTable,
            string sourceProvider, string sourceConnStr, string targetProvider, string targetConnStr)
        {
            return new DataSyncOptions
            {
                SourceProvider = sourceProvider ?? "MySql.Data.MySqlClient",
                SourceConnectionString = sourceConnStr ?? "server=127.0.0.1;database=FastDataDemo;uid=root;pwd=FastData@Test123;SslMode=None",
                TargetProvider = targetProvider ?? "Npgsql",
                TargetConnectionString = targetConnStr ?? "server=127.0.0.1;database=fastdatademo;uid=postgres;pwd=postgres",
                SourceTable = sourceTable,
                TargetTable = targetTable,
                BatchSize = 500,
                RetryCount = 2,
                PrimaryKeyColumns = "id",
                AlwaysDeduplicate = true
            };
        }

        private SyncResult ExecuteSync(DataSyncOptions options)
        {
            var syncService = new FastData.Tooling.Sync.DataSyncService();
            var result = syncService.SyncTable(options);

            return new SyncResult
            {
                ReadCount = result.ReadCount,
                WriteCount = result.WriteCount,
                FailedCount = result.FailedCount,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                TableName = options.SourceTable
            };
        }
    }

    public class SyncResult
    {
        public int ReadCount { get; set; }
        public int WriteCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TableName { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}
