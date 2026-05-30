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
    /// 可信队列消息生产者
    /// 使用 RedisReliableQueue 实现，支持消费确认，消息不丢失
    /// 适用场景：数据库存储、关键业务处理
    /// </summary>
    public class ReliableQueueProducer : IMessageProducer
    {
        private readonly FullRedis _redis;
        private readonly MessageQueueOptions _options;
        private bool _disposed;

        public ReliableQueueProducer(FullRedis redis, MessageQueueOptions options = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _options = options ?? new MessageQueueOptions();
        }

        /// <summary>
        /// 发布单条消息到可信队列
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="message">消息</param>
        /// <returns>是否成功</returns>
        public bool Publish<T>(string topic, T message) where T : class
        {
            var queue = GetQueue<T>(topic);
            var envelope = CreateEnvelope(topic, message);
            queue.Add(envelope);
            return true;
        }

        /// <summary>
        /// 发布单条消息（异步）
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="message">消息</param>
        /// <returns>是否成功任务</returns>
        public Task<bool> PublishAsync<T>(string topic, T message) where T : class
        {
            return Task.FromResult(Publish(topic, message));
        }

        /// <summary>
        /// 批量发布消息
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="messages">消息列表</param>
        /// <returns>发布数量</returns>
        public int PublishBatch<T>(string topic, IEnumerable<T> messages) where T : class
        {
            var queue = GetQueue<T>(topic);
            var envelopes = new List<MessageEnvelope<T>>();
            foreach (var msg in messages)
            {
                envelopes.Add(CreateEnvelope(topic, msg));
            }
            queue.Add(envelopes.ToArray());
            return envelopes.Count;
        }

        /// <summary>
        /// 批量发布消息（异步）
        /// </summary>
        public Task<int> PublishBatchAsync<T>(string topic, IEnumerable<T> messages) where T : class
        {
            return Task.FromResult(PublishBatch(topic, messages));
        }

        private RedisReliableQueue<MessageEnvelope<T>> GetQueue<T>(string topic) where T : class
        {
            var fullTopic = $"{_options.TopicPrefix}:{topic}";
            return _redis.GetReliableQueue<MessageEnvelope<T>>(fullTopic);
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
    /// 可信队列消息消费者
    /// 使用 RedisReliableQueue 实现，支持消费确认
    /// 消费失败时消息会自动回滚到主队列（60秒后）
    /// </summary>
    public class ReliableQueueConsumer : IMessageConsumer
    {
        private readonly FullRedis _redis;
        private readonly MessageQueueOptions _options;
        private bool _disposed;

        public ReliableQueueConsumer(FullRedis redis, MessageQueueOptions options = null)
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
        /// 消费单条消息
        /// </summary>
        public T Consume<T>(string topic, int timeoutSeconds = 10) where T : class
        {
            var queue = GetQueue<T>(topic);
            var msg = queue.TakeOne(timeoutSeconds);
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
        /// 建议每个 Topic 开 8 个线程消费
        /// </summary>
        public async Task ConsumeLoopAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken, int concurrency = 8) where T : class
        {
            var tasks = new List<Task>();
            for (int i = 0; i < concurrency; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var queue = GetQueue<T>(topic);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var envelope = queue.TakeOne(_options.BlockingTimeoutSeconds);
                            if (envelope != null)
                            {
                                try
                                {
                                    await handler(envelope.Body);
                                    // 消费成功，确认消息
                                    queue.Acknowledge(new[] { envelope.MessageId });
                                }
                                catch (Exception ex)
                                {
                                    XTrace.WriteLine($"[ReliableQueue] 消费消息失败: {ex.Message}");
                                    // 不确认消息，60秒后自动回滚到主队列重新消费
                                }
                            }
                        }
                        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                        {
                            XTrace.WriteLine($"[ReliableQueue] 消费循环异常: {ex.Message}");
                            await Task.Delay(1000, cancellationToken);
                        }
                    }
                }, cancellationToken));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 确认消息消费成功
        /// </summary>
        public void Acknowledge(string topic, string messageId)
        {
            var queue = GetQueue<MessageEnvelope<object>>(topic);
            queue.Acknowledge(new[] { messageId });
        }

        /// <summary>
        /// 确认消息消费成功（异步）
        /// </summary>
        public Task AcknowledgeAsync(string topic, string messageId)
        {
            Acknowledge(topic, messageId);
            return Task.CompletedTask;
        }

        private RedisReliableQueue<MessageEnvelope<T>> GetQueue<T>(string topic) where T : class
        {
            var fullTopic = $"{_options.TopicPrefix}:{topic}";
            return _redis.GetReliableQueue<MessageEnvelope<T>>(fullTopic);
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
