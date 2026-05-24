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
        /// 增量字段
        /// </summary>
        public string IncrementalColumn { get; set; }

        /// <summary>
        /// 上次同步时间
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// 是否首次同步
        /// </summary>
        public bool IsFirstSync { get; set; } = true;

        /// <summary>
        /// 同步范围天数
        /// </summary>
        public int RangeDays { get; set; } = 3;

        /// <summary>
        /// 同步模式
        /// </summary>
        public SyncMode SyncMode { get; set; } = SyncMode.Smart;

        /// <summary>
        /// 手动模式起始时间
        /// </summary>
        public DateTime? ManualStartTime { get; set; }

        /// <summary>
        /// 手动模式结束时间
        /// </summary>
        public DateTime? ManualEndTime { get; set; }
    }

    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 智能范围（自动判断首次/后续）
        /// </summary>
        Smart,

        /// <summary>
        /// 手动范围（指定起止时间）
        /// </summary>
        Manual,

        /// <summary>
        /// 全量同步
        /// </summary>
        Full
    }
}
