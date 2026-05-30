using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastRedis.Repository
{
    /// <summary>
    /// Redis 仓储接口
    /// 
    /// 定义 Redis 缓存和消息队列的操作方法。
    /// </summary>
    public interface IRedisRepository
    {
        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="name">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        bool Exists(string name, int db = 0);

        /// <summary>
        /// 异步判断键是否存在
        /// </summary>
        /// <param name="name">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string name, int db = 0);

        /// <summary>
        /// 设置缓存（对象）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="model">对象</param>
        /// <param name="hours">过期时间（小时）</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool Set<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0);

        /// <summary>
        /// 异步设置缓存（对象）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="model">对象</param>
        /// <param name="hours">过期时间（小时）</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0);

        /// <summary>
        /// 设置缓存（字符串，按小时过期）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="model">字符串值</param>
        /// <param name="hours">过期时间（小时）</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool Set(string key, string model, int hours = 24 * 30 * 12, int db = 0);

        /// <summary>
        /// 设置缓存（字符串，按分钟过期）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="model">字符串值</param>
        /// <param name="Minutes">过期时间（分钟）</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool Set(string key, string model, double Minutes, int db = 0);

        /// <summary>
        /// 异步设置缓存（字符串，按分钟过期）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="model">字符串值</param>
        /// <param name="Minutes">过期时间（分钟）</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync(string key, string model, double Minutes, int db = 0);

        /// <summary>
        /// 获取缓存（字符串）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值</returns>
        string Get(string key, int db = 0);

        /// <summary>
        /// 异步获取缓存（字符串）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值</returns>
        Task<string> GetAsync(string key, int db = 0);

        /// <summary>
        /// 获取缓存（对象）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象</returns>
        T Get<T>(string key, int db = 0) where T : class, new();

        /// <summary>
        /// 异步获取缓存（对象）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象</returns>
        Task<T> GetAsync<T>(string key, int db = 0) where T : class, new();

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool Remove(string key, int db = 0);

        /// <summary>
        /// 异步删除缓存
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> RemoveAsync(string key, int db = 0);

        /// <summary>
        /// 批量设置字典缓存
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="dic">字典</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool SetDic<T>(Dictionary<string, T> dic, int db = 0);

        /// <summary>
        /// 异步批量设置字典缓存
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="dic">字典</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> SetDicAsync<T>(Dictionary<string, T> dic, int db = 0);

        /// <summary>
        /// 批量获取字典缓存
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="keys">键名数组</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字典</returns>
        IDictionary<string, T> GetDic<T>(string[] keys, int db = 0) where T : class, new();

        /// <summary>
        /// 异步批量获取字典缓存
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="keys">键名数组</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字典</returns>
        Task<IDictionary<string, T>> GetDicAsync<T>(string[] keys, int db = 0) where T : class, new();

        /// <summary>
        /// 批量删除字典缓存
        /// </summary>
        /// <param name="keys">键名数组</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool RemoveDic(string[] keys, int db = 0);

        /// <summary>
        /// 异步批量删除字典缓存
        /// </summary>
        /// <param name="keys">键名数组</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> RemoveDicAsync(string[] keys, int db = 0);

        /// <summary>
        /// 发送消息到队列（生产者消费者模式）
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="db">数据库索引</param>
        void Send(string queueName, string message, int db = 0);

        /// <summary>
        /// 异步发送消息到队列
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="db">数据库索引</param>
        void SendAsync(string queueName, string message, int db = 0);

        /// <summary>
        /// 从队列接收消息（生产者消费者模式）
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="db">数据库索引</param>
        /// <returns>消息内容</returns>
        string Receive(string queueName, int db = 0);

        /// <summary>
        /// 异步从队列接收消息
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="db">数据库索引</param>
        /// <returns>消息内容</returns>
        Task<string> ReceiveAsync(string queueName, int db = 0);

        /// <summary>
        /// 发布消息到频道（发布者订阅者模式）
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="db">数据库索引</param>
        void Publish(string channel, string message, int db = 0);

        /// <summary>
        /// 异步发布消息到频道
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="db">数据库索引</param>
        void PublishAsync(string channel, string message, int db = 0);

        /// <summary>
        /// 订阅频道接收消息（发布者订阅者模式）
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="message">消息接收回调</param>
        /// <param name="subscribe">订阅成功回调</param>
        /// <param name="unSubscribe">取消订阅回调</param>
        /// <param name="db">数据库索引</param>
        void Receive(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0);

        /// <summary>
        /// 异步订阅频道接收消息
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="message">消息接收回调</param>
        /// <param name="subscribe">订阅成功回调</param>
        /// <param name="unSubscribe">取消订阅回调</param>
        /// <param name="db">数据库索引</param>
        void ReceiveAsync(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0);
    }
}
