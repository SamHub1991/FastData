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
        /// 时间字段（可选，用于动态数据）
        /// 有时间的表填写此字段（如 UpdateTime、CreateTime）
        /// 没有时间的表留空，按静态数据处理
        /// </summary>
        public string TimeColumn { get; set; }

        /// <summary>
        /// 是否启用时间范围（可选）
        /// </summary>
        public bool EnableTimeRange { get; set; }

        /// <summary>
        /// 同步范围天数（默认 3 天）
        /// </summary>
        public int RangeDays { get; set; } = 3;

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

        /// <summary>
        /// 数据类型（静态/动态）
        /// </summary>
        public SyncDataType DataType { get; set; } = SyncDataType.Static;
    }

    /// <summary>
    /// 表同步状态
    /// </summary>
    public enum TableSyncStatus
    {
        Pending,
        Syncing,
        Success,
        Failed,
        Skipped
    }
}
