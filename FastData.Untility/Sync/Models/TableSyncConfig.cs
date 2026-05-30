using System;
using System.Collections.Generic;

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
        /// 是否启用全局配置
        /// </summary>
        public bool EnableGlobalConfig { get; set; } = false;

        /// <summary>
        /// 全局同步范围天数（0=使用任务配置）
        /// </summary>
        public int GlobalRangeDays { get; set; } = 0;

        /// <summary>
        /// 是否始终去重（只插入不存在的记录）
        /// </summary>
        public bool AlwaysDeduplicate { get; set; } = true;

        /// <summary>
        /// 同步字段列表（空表示所有字段）
        /// </summary>
        public IList<string> SyncColumns { get; set; }

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

        /// <summary>
        /// 是否启用消息队列（削峰/解耦）
        /// 启用后，数据会先写入消息队列，再由消费者异步处理
        /// </summary>
        public bool EnableMessageQueue { get; set; } = false;

        /// <summary>
        /// 消息队列类型
        /// ReliableQueue：可信队列（单消费，消费确认，适合数据库存储）
        /// Stream：多消费组队列（多方推送，广播通知）
        /// </summary>
        public MessageQueueType MessageQueueType { get; set; } = MessageQueueType.ReliableQueue;

        /// <summary>
        /// 消息队列主题名称（为空则自动使用表名）
        /// </summary>
        public string MessageQueueTopic { get; set; }

        /// <summary>
        /// 消费组名称（仅 Stream 模式有效）
        /// </summary>
        public string ConsumerGroup { get; set; } = "default";

        /// <summary>
        /// 消费者并发线程数（默认 8）
        /// </summary>
        public int ConsumerConcurrency { get; set; } = 8;
    }

    /// <summary>
    /// 消息队列类型
    /// </summary>
    public enum MessageQueueType
    {
        /// <summary>
        /// 可信队列（单消费，消费确认，适合数据库存储场景）
        /// 使用 RedisReliableQueue 实现
        /// </summary>
        ReliableQueue = 0,

        /// <summary>
        /// 多消费组队列（多消费组独立消费，适合多方推送场景）
        /// 使用 RedisStream 实现
        /// </summary>
        Stream = 1
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
