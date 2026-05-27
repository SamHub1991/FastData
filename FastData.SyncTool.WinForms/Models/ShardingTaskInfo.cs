using System;
using System.Threading;

namespace FastData.SyncTool.WinForms.Models
{
    /// <summary>
    /// 分表任务状态
    /// </summary>
    public enum ShardingTaskStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 分表任务信息
    /// </summary>
    public class ShardingTaskInfo
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 源表名
        /// </summary>
        public string SourceTable { get; set; }

        /// <summary>
        /// 分表策略类型
        /// </summary>
        public string ShardingType { get; set; }

        /// <summary>
        /// 分表配置描述
        /// </summary>
        public string ConfigDescription { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public ShardingTaskStatus Status { get; set; } = ShardingTaskStatus.Pending;

        /// <summary>
        /// 总记录数
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// 已处理记录数
        /// </summary>
        public long ProcessedRecords { get; set; }

        /// <summary>
        /// 成功记录数
        /// </summary>
        public long SuccessRecords { get; set; }

        /// <summary>
        /// 失败记录数
        /// </summary>
        public long FailedRecords { get; set; }

        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// CancellationTokenSource
        /// </summary>
        internal CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// ManualResetEventSlim for pause
        /// </summary>
        internal ManualResetEventSlim PauseEvent { get; set; } = new ManualResetEventSlim(true);
    }
}
