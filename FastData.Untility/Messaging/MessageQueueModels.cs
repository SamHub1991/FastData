#if !NETFRAMEWORK
using System;

namespace FastRedis.Messaging
{
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
    /// 消息信封，包装消息元数据
    /// </summary>
    /// <typeparam name="T">消息体类型</typeparam>
    public class MessageEnvelope<T> where T : class
    {
        /// <summary>
        /// 消息唯一标识
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 消息主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public T Body { get; set; }

        /// <summary>
        /// 消息创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息来源标识（如表名、设备ID等）
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 附加标签（用于消息过滤）
        /// </summary>
        public string Tag { get; set; }
    }

    /// <summary>
    /// 消息队列配置选项
    /// </summary>
    public class MessageQueueOptions
    {
        /// <summary>
        /// 队列类型
        /// </summary>
        public MessageQueueType QueueType { get; set; } = MessageQueueType.ReliableQueue;

        /// <summary>
        /// 主题名称前缀（自动拼接表名）
        /// </summary>
        public string TopicPrefix { get; set; } = "fastdata";

        /// <summary>
        /// 消费者并发线程数
        /// </summary>
        public int ConsumerConcurrency { get; set; } = 8;

        /// <summary>
        /// 阻塞超时秒数
        /// </summary>
        public int BlockingTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// 消费组名称（仅 Stream 模式有效）
        /// </summary>
        public string ConsumerGroup { get; set; } = "default";

        /// <summary>
        /// Redis 数据库索引
        /// </summary>
        public int RedisDb { get; set; } = 0;

        /// <summary>
        /// 批量消费大小
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 是否启用消息持久化（Stream 模式自动持久化）
        /// </summary>
        public bool EnablePersistence { get; set; } = true;
    }
}
#endif
