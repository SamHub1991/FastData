using FastData.Tooling.Sync;
using FastData.Config;
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
        /// <summary>
        /// 同步用户表
        /// </summary>
        /// <param name="sourceProvider">源数据库提供程序</param>
        /// <param name="sourceConnStr">源数据库连接字符串</param>
        /// <param name="targetProvider">目标数据库提供程序</param>
        /// <param name="targetConnStr">目标数据库连接字符串</param>
        /// <returns>同步结果</returns>
        Task<SyncResult> SyncUsersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);

        /// <summary>
        /// 同步所有表
        /// </summary>
        /// <param name="sourceProvider">源数据库提供程序</param>
        /// <param name="sourceConnStr">源数据库连接字符串</param>
        /// <param name="targetProvider">目标数据库提供程序</param>
        /// <param name="targetConnStr">目标数据库连接字符串</param>
        /// <returns>同步结果</returns>
        Task<SyncResult> SyncAllTablesAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);

        /// <summary>
        /// 同步订单表
        /// </summary>
        /// <param name="sourceProvider">源数据库提供程序</param>
        /// <param name="sourceConnStr">源数据库连接字符串</param>
        /// <param name="targetProvider">目标数据库提供程序</param>
        /// <param name="targetConnStr">目标数据库连接字符串</param>
        /// <returns>同步结果</returns>
        Task<SyncResult> SyncOrdersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null);
    }

    /// <summary>
    /// 数据同步服务实现
    /// 
    /// 提供数据库表之间的数据同步功能。
    /// </summary>
    public class DataSyncService : IDataSyncService
    {
        /// <summary>
        /// 同步用户表
        /// </summary>
        public async Task<SyncResult> SyncUsersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var options = BuildOptions("AppUser", "AppUser", sourceProvider, sourceConnStr, targetProvider, targetConnStr);
            return await Task.Factory.StartNew(() => ExecuteSync(options));
        }

        /// <summary>
        /// 同步所有表
        /// </summary>
        public async Task<SyncResult> SyncAllTablesAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var results = new SyncResult();
            var tables = new[] { "AppUser", "AppOrder" };

            foreach (var table in tables)
            {
                var options = BuildOptions(table, table, sourceProvider, sourceConnStr, targetProvider, targetConnStr);
                var result = await Task.Factory.StartNew(() => ExecuteSync(options));
                results.ReadCount += result.ReadCount;
                results.WriteCount += result.WriteCount;
                results.FailedCount += result.FailedCount;
            }

            results.EndTime = DateTime.Now;
            return results;
        }

        /// <summary>
        /// 同步订单表
        /// </summary>
        public async Task<SyncResult> SyncOrdersAsync(string sourceProvider = null, string sourceConnStr = null,
            string targetProvider = null, string targetConnStr = null)
        {
            var options = BuildOptions("AppOrder", "AppOrder", sourceProvider, sourceConnStr, targetProvider, targetConnStr);
            return await Task.Factory.StartNew(() => ExecuteSync(options));
        }

        /// <summary>
        /// 构建同步选项
        /// </summary>
        /// <param name="sourceTable">源表名</param>
        /// <param name="targetTable">目标表名</param>
        /// <param name="sourceProvider">源数据库提供程序</param>
        /// <param name="sourceConnStr">源数据库连接字符串</param>
        /// <param name="targetProvider">目标数据库提供程序</param>
        /// <param name="targetConnStr">目标数据库连接字符串</param>
        /// <returns>同步选项</returns>
        private DataSyncOptions BuildOptions(string sourceTable, string targetTable,
            string sourceProvider, string sourceConnStr, string targetProvider, string targetConnStr)
        {
            return new DataSyncOptions
            {
                SourceProvider = sourceProvider ?? "MySql.Data.MySqlClient",
                SourceConnectionString = sourceConnStr ?? FastDataConfig.GetConnectionString("MySql"),
                TargetProvider = targetProvider ?? "Npgsql",
                TargetConnectionString = targetConnStr ?? FastDataConfig.GetConnectionString("PostgreSql"),
                SourceTable = sourceTable,
                TargetTable = targetTable,
                BatchSize = 500,
                RetryCount = 2,
                PrimaryKeyColumns = "id",
                AlwaysDeduplicate = true
            };
        }

        /// <summary>
        /// 执行同步
        /// </summary>
        /// <param name="options">同步选项</param>
        /// <returns>同步结果</returns>
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

    /// <summary>
    /// 同步结果
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// 读取记录数
        /// </summary>
        public int ReadCount { get; set; }

        /// <summary>
        /// 写入记录数
        /// </summary>
        public int WriteCount { get; set; }

        /// <summary>
        /// 失败记录数
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 同步耗时
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }
}
