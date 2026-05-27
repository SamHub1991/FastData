#if !NETFRAMEWORK
using System;

namespace FastData.Queue
{
    /// <summary>
    /// 写入后端队列配置
    /// 用于配置表级别的消息队列策略
    /// </summary>
    public class WriteBehindConfig
    {
        /// <summary>
        /// 队列类型
        /// </summary>
        public WriteBehindQueueType QueueType { get; set; } = WriteBehindQueueType.ReliableQueue;

        /// <summary>
        /// 主题名称（为空则自动使用表名）
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 是否启用降级（数据库异常时自动切换到队列）
        /// </summary>
        public bool EnableFallback { get; set; } = true;

        /// <summary>
        /// 是否启用自动恢复（数据库恢复后自动刷写队列）
        /// </summary>
        public bool EnableAutoRecovery { get; set; } = true;

        /// <summary>
        /// 恢复检查间隔秒数
        /// </summary>
        public int RecoveryIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 批量刷写大小
        /// </summary>
        public int BatchFlushSize { get; set; } = 100;

        /// <summary>
        /// Redis 连接字符串（为空则使用默认连接）
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// Redis 数据库索引
        /// </summary>
        public int RedisDb { get; set; } = 7;
    }

    /// <summary>
    /// 写入后端队列类型
    /// </summary>
    public enum WriteBehindQueueType
    {
        /// <summary>
        /// 不使用队列（直接写数据库）
        /// </summary>
        None = 0,

        /// <summary>
        /// 可信队列（单消费、消费确认、消息不丢失）
        /// </summary>
        ReliableQueue = 1,

        /// <summary>
        /// 多消费组队列（多方推送）
        /// </summary>
        Stream = 2
    }
}
#endif
