using System;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 同步任务配置
    /// </summary>
    public class SyncTaskConfig
    {
        /// <summary>
        /// 任务 ID
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 源表名
        /// </summary>
        public string SourceTable { get; set; }

        /// <summary>
        /// 目标表名
        /// </summary>
        public string TargetTable { get; set; }

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
        /// </summary>
        public string TimeColumn { get; set; }

        /// <summary>
        /// 是否启用时间范围
        /// </summary>
        public bool EnableTimeRange { get; set; }

        /// <summary>
        /// 同步范围天数
        /// </summary>
        public int RangeDays { get; set; } = 3;

        /// <summary>
        /// 上次同步时间
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// 是否首次同步
        /// </summary>
        public bool IsFirstSync { get; set; } = true;

        /// <summary>
        /// 数据类型（静态/动态）
        /// </summary>
        public SyncDataType DataType { get; set; } = SyncDataType.Static;
    }
}
