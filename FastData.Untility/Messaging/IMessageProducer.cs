#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastRedis.Messaging
{
    /// <summary>
    /// 消息生产者接口
    /// 用于向消息队列发布消息
    /// </summary>
    public interface IMessageProducer : IDisposable
    {
        /// <summary>
        /// 发布单条消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="message">消息对象</param>
        /// <returns>是否成功</returns>
        bool Publish<T>(string topic, T message) where T : class;

        /// <summary>
        /// 发布单条消息（异步）
        /// </summary>
        Task<bool> PublishAsync<T>(string topic, T message) where T : class;

        /// <summary>
        /// 批量发布消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="messages">消息列表</param>
        /// <returns>成功发布的数量</returns>
        int PublishBatch<T>(string topic, IEnumerable<T> messages) where T : class;

        /// <summary>
        /// 批量发布消息（异步）
        /// </summary>
        Task<int> PublishBatchAsync<T>(string topic, IEnumerable<T> messages) where T : class;
    }
}
#endif
