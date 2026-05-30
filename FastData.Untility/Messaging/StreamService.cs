#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Log;

namespace FastRedis.Messaging
{
    /// <summary>
    /// Stream 消息生产者
    /// 使用 RedisStream 实现，支持多消费组独立消费
    /// 适用场景：多方推送、广播通知、数据分发
    /// </summary>
    public class StreamProducer : IMessageProducer
    {
        private readonly FullRedis _redis;
        private readonly MessageQueueOptions _options;
        private bool _disposed;

        public StreamProducer(FullRedis redis, MessageQueueOptions options = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _options = options ?? new MessageQueueOptions();
        }

        /// <summary>
        /// 发布消息到 Stream
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="message">消息</param>
        /// <returns>是否成功</returns>
        public bool Publish<T>(string topic, T message) where T : class
        {
            var stream = _redis.GetStream<MessageEnvelope<T>>($"{_options.TopicPrefix}:{topic}");
            var envelope = CreateEnvelope(topic, message);
            stream.Add(envelope);
            return true;
        }

        /// <summary>
        /// 发布消息到 Stream（异步）
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="message">消息</param>
        /// <returns>是否成功任务</returns>
        public Task<bool> PublishAsync<T>(string topic, T message) where T : class
        {
            return Task.FromResult(Publish(topic, message));
        }

        /// <summary>
        /// 批量发布消息到 Stream
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="messages">消息列表</param>
        /// <returns>发布数量</returns>
        public int PublishBatch<T>(string topic, IEnumerable<T> messages) where T : class
        {
            var stream = _redis.GetStream<MessageEnvelope<T>>($"{_options.TopicPrefix}:{topic}");
            var count = 0;
            foreach (var msg in messages)
            {
                var envelope = CreateEnvelope(topic, msg);
                stream.Add(envelope);
                count++;
            }
            return count;
        }

        /// <summary>
        /// 批量发布消息到 Stream（异步）
        /// </summary>
        public Task<int> PublishBatchAsync<T>(string topic, IEnumerable<T> messages) where T : class
        {
            return Task.FromResult(PublishBatch(topic, messages));
        }

        private MessageEnvelope<T> CreateEnvelope<T>(string topic, T message) where T : class
        {
            return new MessageEnvelope<T>
            {
                Topic = topic,
                Body = message,
                Source = _options.TopicPrefix,
                CreateTime = DateTime.Now
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Stream 消息消费者
    /// 使用 RedisStream 实现，支持多消费组独立消费
    /// 每个消费组内的消息只会被消费一次，不同消费组独立消费
    /// </summary>
    public class StreamConsumer : IMessageConsumer
    {
        private readonly FullRedis _redis;
        private readonly MessageQueueOptions _options;
        private bool _disposed;

        public StreamConsumer(FullRedis redis, MessageQueueOptions options = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _options = options ?? new MessageQueueOptions();
            // 消费端需要更长的超时时间
            if (_redis.Timeout < (_options.BlockingTimeoutSeconds + 1) * 1000)
            {
                _redis.Timeout = (_options.BlockingTimeoutSeconds + 1) * 1000;
            }
        }

        /// <summary>
        /// 消费组名称
        /// </summary>
        public string ConsumerGroup
        {
            get => _options.ConsumerGroup;
            set => _options.ConsumerGroup = value;
        }

        /// <summary>
        /// 消费单条消息
        /// </summary>
        public T Consume<T>(string topic, int timeoutSeconds = 10) where T : class
        {
            var stream = _redis.GetStream<MessageEnvelope<T>>($"{_options.TopicPrefix}:{topic}");
            if (!string.IsNullOrEmpty(_options.ConsumerGroup))
            {
                stream.Group = _options.ConsumerGroup;
            }
            var msg = stream.TakeOne(timeoutSeconds);
            return msg?.Body;
        }

        /// <summary>
        /// 消费单条消息（异步）
        /// </summary>
        public Task<T> ConsumeAsync<T>(string topic, int timeoutSeconds = 10) where T : class
        {
            return Task.FromResult(Consume<T>(topic, timeoutSeconds));
        }

        /// <summary>
        /// 持续消费消息（循环消费）
        /// 使用 ConsumeAsync 回调模式，支持多消费组
        /// </summary>
        public async Task ConsumeLoopAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken, int concurrency = 8) where T : class
        {
            var stream = _redis.GetStream<MessageEnvelope<T>>($"{_options.TopicPrefix}:{topic}");

            // 设置消费组
            if (!string.IsNullOrEmpty(_options.ConsumerGroup))
            {
                stream.Group = _options.ConsumerGroup;
            }

            // 使用 NewLife.Redis 的 ConsumeAsync 方法
            await stream.ConsumeAsync(async (MessageEnvelope<T> envelope) =>
            {
                try
                {
                    await handler(envelope.Body);
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine($"[Stream] 消费消息失败: {ex.Message}");
                    throw; // 让消息保持 pending 状态
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 确认消息消费成功
        /// </summary>
        public void Acknowledge(string topic, string messageId)
        {
            var stream = _redis.GetStream<MessageEnvelope<object>>($"{_options.TopicPrefix}:{topic}");
            stream.Acknowledge(new[] { messageId });
        }

        /// <summary>
        /// 确认消息消费成功（异步）
        /// </summary>
        public Task AcknowledgeAsync(string topic, string messageId)
        {
            Acknowledge(topic, messageId);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
#endif
