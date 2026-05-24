using System;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 单表同步配置（用于多表批量同步）
    /// </summary>
    public class TableSyncConfig
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 目标表名（为空则与源表同名）
        /// </summary>
        public string TargetTableName { get; set; }

        /// <summary>
        /// 主键字段（逗号分隔）
        /// </summary>
        public string PrimaryKeyColumns { get; set; }

        /// <summary>
        /// 是否自增主键
        /// </summary>
        public bool IsAutoIncrementKey { get; set; }

        /// <summary>
        /// 增量字段
        /// </summary>
        public string IncrementalColumn { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 同步状态
        /// </summary>
        public TableSyncStatus Status { get; set; } = TableSyncStatus.Pending;

        /// <summary>
        /// 最后同步时间
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// 最后同步结果消息
        /// </summary>
        public string LastResultMessage { get; set; }
    }

    /// <summary>
    /// 表同步状态
    /// </summary>
    public enum TableSyncStatus
    {
        /// <summary>
        /// 待同步
        /// </summary>
        Pending,

        /// <summary>
        /// 同步中
        /// </summary>
        Syncing,

        /// <summary>
        /// 同步成功
        /// </summary>
        Success,

        /// <summary>
        /// 同步失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped
    }
}
