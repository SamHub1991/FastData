using FastData.Tooling.Sync;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Tooling.Abstractions
{
    /// <summary>
    /// 数据同步服务接口
    /// 定义数据同步的核心操作
    /// </summary>
    public interface IDataSyncService
    {
        /// <summary>
        /// 同步表数据
        /// </summary>
        /// <param name="config">表同步配置</param>
        /// <param name="progressReporter">进度报告回调</param>
        /// <returns>同步结果</returns>
        Task<SyncResult> SyncTableAsync(TableSyncConfig config, Action<string> progressReporter = null);

        /// <summary>
        /// 同步任务配置
        /// </summary>
        /// <param name="taskConfig">任务配置</param>
        /// <param name="progressReporter">进度报告回调</param>
        /// <returns>同步结果</returns>
        Task<SyncResult> SyncTaskAsync(SyncTaskConfig taskConfig, Action<string> progressReporter = null);

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>连接测试结果</returns>
        Task<ConnectionTestResult> TestConnectionAsync(string provider, string connectionString);

        /// <summary>
        /// 加载表的列信息
        /// </summary>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="tableName">表名</param>
        /// <returns>列信息列表</returns>
        Task<IList<ColumnInfo>> GetTableColumnsAsync(string provider, string connectionString, string tableName);

        /// <summary>
        /// 获取表的主键列
        /// </summary>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="tableName">表名</param>
        /// <returns>主键列名列表</returns>
        Task<IList<string>> GetPrimaryKeyColumnsAsync(string provider, string connectionString, string tableName);

        /// <summary>
        /// 获取数据库中的所有表
        /// </summary>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>表名列表</returns>
        Task<IList<string>> GetTablesAsync(string provider, string connectionString);
    }

    /// <summary>
    /// 同步结果
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 处理的记录数
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 成功插入的记录数
        /// </summary>
        public int InsertedCount { get; set; }

        /// <summary>
        /// 成功更新的记录数
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 跳过的记录数（去重）
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 失败的记录数
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// 连接测试结果
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 服务器版本
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 列信息
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }
    }
}
