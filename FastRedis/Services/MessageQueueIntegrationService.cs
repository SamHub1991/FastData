#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FastRedis.Messaging;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Log;

namespace FastRedis.Services
{
    /// <summary>
    /// 消息队列集成服务
    /// 提供消息队列与数据同步的集成能力
    /// 支持 RTU 数据上传场景：一边存库、多方推送
    /// </summary>
    public class MessageQueueIntegrationService : IDisposable
    {
        private readonly FullRedis _redis;
        private readonly MessageQueueFactory _factory;
        private readonly Dictionary<string, IMessageProducer> _producers;
        private readonly Dictionary<string, IMessageConsumer> _consumers;
        private bool _disposed;

        public MessageQueueIntegrationService(FullRedis redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _factory = new MessageQueueFactory(redis);
            _producers = new Dictionary<string, IMessageProducer>();
            _consumers = new Dictionary<string, IMessageConsumer>();
        }

        /// <summary>
        /// 发布数据到消息队列（支持批量）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="data">数据列表</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="consumerGroup">消费组名称（仅 Stream 模式）</param>
        /// <returns>成功发布的数量</returns>
        public int PublishData<T>(string topic, IEnumerable<T> data, MessageQueueType queueType = MessageQueueType.ReliableQueue, string consumerGroup = null) where T : class
        {
            var producer = GetOrCreateProducer(topic, queueType, consumerGroup);
            return producer.PublishBatch(topic, data);
        }

        /// <summary>
        /// 发布数据到消息队列（异步批量）
        /// </summary>
        public Task<int> PublishDataAsync<T>(string topic, IEnumerable<T> data, MessageQueueType queueType = MessageQueueType.ReliableQueue, string consumerGroup = null) where T : class
        {
            var producer = GetOrCreateProducer(topic, queueType, consumerGroup);
            return producer.PublishBatchAsync(topic, data);
        }

        /// <summary>
        /// 发布单条数据到消息队列
        /// </summary>
        public bool PublishSingle<T>(string topic, T data, MessageQueueType queueType = MessageQueueType.ReliableQueue, string consumerGroup = null) where T : class
        {
            var producer = GetOrCreateProducer(topic, queueType, consumerGroup);
            return producer.Publish(topic, data);
        }

        /// <summary>
        /// 发布 DataTable 数据到消息队列（每行作为一个消息）
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="table">数据表</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="consumerGroup">消费组名称</param>
        /// <returns>成功发布的行数</returns>
        public int PublishDataTable(string topic, DataTable table, MessageQueueType queueType = MessageQueueType.ReliableQueue, string consumerGroup = null)
        {
            if (table == null || table.Rows.Count == 0) return 0;

            var producer = GetOrCreateProducer(topic, queueType, consumerGroup);
            var count = 0;

            foreach (DataRow row in table.Rows)
            {
                var rowData = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    rowData[col.ColumnName] = row[col];
                }

                var envelope = new MessageEnvelope<Dictionary<string, object>>
                {
                    Topic = topic,
                    Body = rowData,
                    Source = topic,
                    CreateTime = DateTime.Now
                };

                if (producer is ReliableQueueProducer reliableProducer)
                {
                    var queue = _redis.GetReliableQueue<MessageEnvelope<Dictionary<string, object>>>($"fastdata:{topic}");
                    queue.Add(envelope);
                    count++;
                }
                else if (producer is StreamProducer streamProducer)
                {
                    var stream = _redis.GetStream<MessageEnvelope<Dictionary<string, object>>>($"fastdata:{topic}");
                    stream.Add(envelope);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 启动消费者（循环消费模式）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="handler">消息处理函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="consumerGroup">消费组名称</param>
        /// <param name="concurrency">并发线程数</param>
        public Task StartConsumerAsync<T>(
            string topic,
            Func<T, Task> handler,
            CancellationToken cancellationToken,
            MessageQueueType queueType = MessageQueueType.ReliableQueue,
            string consumerGroup = null,
            int concurrency = 8) where T : class
        {
            var consumer = GetOrCreateConsumer(topic, queueType, consumerGroup);
            return consumer.ConsumeLoopAsync(topic, handler, cancellationToken, concurrency);
        }

        /// <summary>
        /// 启动 DataTable 消费者（循环消费模式，返回字典数据）
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="handler">消息处理函数（接收字典数据）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="consumerGroup">消费组名称</param>
        /// <param name="concurrency">并发线程数</param>
        public Task StartDataTableConsumerAsync(
            string topic,
            Func<Dictionary<string, object>, Task> handler,
            CancellationToken cancellationToken,
            MessageQueueType queueType = MessageQueueType.ReliableQueue,
            string consumerGroup = null,
            int concurrency = 8)
        {
            var consumer = GetOrCreateConsumer(topic, queueType, consumerGroup);
            return consumer.ConsumeLoopAsync<MessageEnvelope<Dictionary<string, object>>>(
                topic,
                async (envelope) => await handler(envelope.Body),
                cancellationToken,
                concurrency);
        }

        /// <summary>
        /// 启动多消费组消费者（Stream 模式专用）
        /// 不同消费组独立消费同一份数据
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="consumerGroups">消费组列表</param>
        /// <param name="handlers">每个消费组对应的处理函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="concurrency">并发线程数</param>
        public Task StartMultiGroupConsumerAsync<T>(
            string topic,
            string[] consumerGroups,
            Func<T, Task>[] handlers,
            CancellationToken cancellationToken,
            int concurrency = 8) where T : class
        {
            if (consumerGroups.Length != handlers.Length)
                throw new ArgumentException("消费组数量必须与处理函数数量一致");

            var tasks = new List<Task>();
            for (int i = 0; i < consumerGroups.Length; i++)
            {
                var group = consumerGroups[i];
                var handler = handlers[i];
                var consumer = GetOrCreateConsumer(topic, MessageQueueType.Stream, group);
                tasks.Add(consumer.ConsumeLoopAsync(topic, handler, cancellationToken, concurrency));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// 获取或创建生产者
        /// </summary>
        private IMessageProducer GetOrCreateProducer(string topic, MessageQueueType queueType, string consumerGroup)
        {
            var key = $"{topic}:{queueType}:{consumerGroup ?? ""}";
            if (!_producers.TryGetValue(key, out var producer))
            {
                var options = new MessageQueueOptions
                {
                    QueueType = queueType,
                    TopicPrefix = "fastdata",
                    ConsumerGroup = consumerGroup ?? "default"
                };
                producer = _factory.CreateProducer(options);
                _producers[key] = producer;
            }
            return producer;
        }

        /// <summary>
        /// 获取或创建消费者
        /// </summary>
        private IMessageConsumer GetOrCreateConsumer(string topic, MessageQueueType queueType, string consumerGroup)
        {
            var key = $"{topic}:{queueType}:{consumerGroup ?? ""}";
            if (!_consumers.TryGetValue(key, out var consumer))
            {
                var options = new MessageQueueOptions
                {
                    QueueType = queueType,
                    TopicPrefix = "fastdata",
                    ConsumerGroup = consumerGroup ?? "default"
                };
                consumer = _factory.CreateConsumer(options);
                _consumers[key] = consumer;
            }
            return consumer;
        }

        /// <summary>
        /// 获取队列状态信息
        /// </summary>
        public Dictionary<string, object> GetQueueStatus(string topic, MessageQueueType queueType)
        {
            var status = new Dictionary<string, object>
            {
                ["Topic"] = topic,
                ["QueueType"] = queueType.ToString(),
                ["TopicPrefix"] = "fastdata"
            };

            try
            {
                if (queueType == MessageQueueType.ReliableQueue)
                {
                    var queue = _redis.GetReliableQueue<MessageEnvelope<object>>($"fastdata:{topic}");
                    status["PendingCount"] = queue.Count;
                }
                else
                {
                    var stream = _redis.GetStream<MessageEnvelope<object>>($"fastdata:{topic}");
                    status["PendingCount"] = stream.Count;
                }
            }
            catch (Exception ex)
            {
                status["Error"] = ex.Message;
            }

            return status;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var producer in _producers.Values)
                {
                    producer?.Dispose();
                }
                foreach (var consumer in _consumers.Values)
                {
                    consumer?.Dispose();
                }
                _producers.Clear();
                _consumers.Clear();
                _disposed = true;
            }
        }
    }
}
#endif
