#if !NETFRAMEWORK
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastRedis.Messaging
{
    /// <summary>
    /// 消息消费者接口
    /// 用于从消息队列消费消息
    /// </summary>
    public interface IMessageConsumer : IDisposable
    {
        /// <summary>
        /// 消费单条消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="timeoutSeconds">阻塞超时秒数</param>
        /// <returns>消息对象，超时返回 null</returns>
        T Consume<T>(string topic, int timeoutSeconds = 10) where T : class;

        /// <summary>
        /// 消费单条消息（异步）
        /// </summary>
        Task<T> ConsumeAsync<T>(string topic, int timeoutSeconds = 10) where T : class;

        /// <summary>
        /// 持续消费消息（循环消费）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="handler">消息处理函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="concurrency">并发消费线程数（默认8）</param>
        Task ConsumeLoopAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken, int concurrency = 8) where T : class;

        /// <summary>
        /// 确认消息消费成功（仅 RedisReliableQueue 需要）
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="messageId">消息ID</param>
        void Acknowledge(string topic, string messageId);

        /// <summary>
        /// 确认消息消费成功（异步）
        /// </summary>
        Task AcknowledgeAsync(string topic, string messageId);
    }
}
#endif
