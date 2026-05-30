#if !NETFRAMEWORK
using System;
using NewLife.Caching;

namespace FastRedis.Messaging
{
    /// <summary>
    /// 消息队列工厂
    /// 根据配置创建对应的生产者和消费者
    /// </summary>
    public class MessageQueueFactory
    {
        private readonly FullRedis _redis;

        public MessageQueueFactory(FullRedis redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        }

        /// <summary>
        /// 创建消息生产者
        /// </summary>
        /// <param name="options">队列配置</param>
        public IMessageProducer CreateProducer(MessageQueueOptions options = null)
        {
            options = options ?? new MessageQueueOptions();
            return options.QueueType switch
            {
                MessageQueueType.ReliableQueue => new ReliableQueueProducer(_redis, options),
                MessageQueueType.Stream => new StreamProducer(_redis, options),
                _ => throw new NotSupportedException($"不支持的队列类型: {options.QueueType}")
            };
        }

        /// <summary>
        /// 创建消息消费者
        /// </summary>
        /// <param name="options">队列配置</param>
        public IMessageConsumer CreateConsumer(MessageQueueOptions options = null)
        {
            options = options ?? new MessageQueueOptions();
            return options.QueueType switch
            {
                MessageQueueType.ReliableQueue => new ReliableQueueConsumer(_redis, options),
                MessageQueueType.Stream => new StreamConsumer(_redis, options),
                _ => throw new NotSupportedException($"不支持的队列类型: {options.QueueType}")
            };
        }

        /// <summary>
        /// 创建可信队列生产者（简化工厂方法）
        /// </summary>
        /// <param name="topicPrefix">主题前缀</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public IMessageProducer CreateReliableProducer(string topicPrefix = "fastdata", int redisDb = 0)
        {
            return CreateProducer(new MessageQueueOptions
            {
                QueueType = MessageQueueType.ReliableQueue,
                TopicPrefix = topicPrefix,
                RedisDb = redisDb
            });
        }

        /// <summary>
        /// 创建可信队列消费者（简化工厂方法）
        /// </summary>
        /// <param name="topicPrefix">主题前缀</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public IMessageConsumer CreateReliableConsumer(string topicPrefix = "fastdata", int redisDb = 0)
        {
            return CreateConsumer(new MessageQueueOptions
            {
                QueueType = MessageQueueType.ReliableQueue,
                TopicPrefix = topicPrefix,
                RedisDb = redisDb
            });
        }

        /// <summary>
        /// 创建 Stream 生产者（简化工厂方法）
        /// </summary>
        /// <param name="topicPrefix">主题前缀</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public IMessageProducer CreateStreamProducer(string topicPrefix = "fastdata", int redisDb = 0)
        {
            return CreateProducer(new MessageQueueOptions
            {
                QueueType = MessageQueueType.Stream,
                TopicPrefix = topicPrefix,
                RedisDb = redisDb
            });
        }

        /// <summary>
        /// 创建 Stream 消费者（简化工厂方法）
        /// </summary>
        /// <param name="consumerGroup">消费组名称</param>
        /// <param name="topicPrefix">主题前缀</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public IMessageConsumer CreateStreamConsumer(string consumerGroup, string topicPrefix = "fastdata", int redisDb = 0)
        {
            return CreateConsumer(new MessageQueueOptions
            {
                QueueType = MessageQueueType.Stream,
                ConsumerGroup = consumerGroup,
                TopicPrefix = topicPrefix,
                RedisDb = redisDb
            });
        }
    }
}
#endif
