using FastData.Tooling.Sync;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Services
{
    /// <summary>
    /// 数据同步服务接口
    /// </summary>
    public interface IDataSyncService
    {
        Task<SyncResult> SyncUsersAsync();
        Task<SyncResult> SyncOrdersAsync();
        Task<SyncResult> SyncAllTablesAsync();
    }

    /// <summary>
    /// 数据同步服务实现
    /// </summary>
    public class DataSyncService : IDataSyncService
    {
        private readonly List<TableSyncConfig> _syncConfigs;

        public DataSyncService()
        {
            _syncConfigs = new List<TableSyncConfig>
            {
                new TableSyncConfig
                {
                    TableName = "Users",
                    TargetTableName = "Users_Archive",
                    PrimaryKeyColumns = "Id",
                    TimeColumn = "UpdateTime",
                    EnableTimeRange = true,
                    RangeDays = 7,
                    IsEnabled = true
                },
                new TableSyncConfig
                {
                    TableName = "Orders",
                    TargetTableName = "Orders_Archive",
                    PrimaryKeyColumns = "Id",
                    TimeColumn = "CreateTime",
                    EnableTimeRange = true,
                    RangeDays = 30,
                    IsEnabled = true
                }
            };
        }

        public async Task<SyncResult> SyncUsersAsync()
        {
            var config = _syncConfigs[0];
            return await ExecuteSyncAsync(config);
        }

        public async Task<SyncResult> SyncOrdersAsync()
        {
            var config = _syncConfigs[1];
            return await ExecuteSyncAsync(config);
        }

        public async Task<SyncResult> SyncAllTablesAsync()
        {
            var results = new SyncResult();

            foreach (var config in _syncConfigs)
            {
                if (!config.IsEnabled) continue;

                var result = await ExecuteSyncAsync(config);
                results.ReadCount += result.ReadCount;
                results.WriteCount += result.WriteCount;
                results.FailedCount += result.FailedCount;
            }

            return results;
        }

        private async Task<SyncResult> ExecuteSyncAsync(TableSyncConfig config)
        {
            // 这里只是模拟同步逻辑，实际需要数据库连接
            await Task.Delay(100);

            return new SyncResult
            {
                ReadCount = 100,
                WriteCount = 95,
                FailedCount = 5,
                StartTime = DateTime.Now.AddSeconds(-1),
                EndTime = DateTime.Now,
                TableName = config.TableName
            };
        }
    }

    /// <summary>
    /// 同步结果
    /// </summary>
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
