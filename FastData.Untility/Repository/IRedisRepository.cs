using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastRedis.Repository
{
    public interface IRedisRepository
    {
        bool Exists(string key, int db = 0);

        Task<bool> ExistsAsync(string key, int db = 0);

        bool Set<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0);

        Task<bool> SetAsync<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0);

        bool Set(string key, string model, int hours = 24 * 30 * 12, int db = 0);

        bool Set(string key, string model, double Minutes, int db = 0);

        Task<bool> SetAsync(string key, string model, double Minutes, int db = 0);

        string Get(string key, int db = 0);

        Task<string> GetAsync(string key, int db = 0);

        T Get<T>(string key, int db = 0) where T : class, new();

        Task<T> GetAsync<T>(string key, int db = 0) where T : class, new();

        bool Remove(string key, int db = 0);

        Task<bool> RemoveAsync(string key, int db = 0);

        bool SetDic<T>(Dictionary<string, T> dic, int db = 0);

        Task<bool> SetDicAsync<T>(Dictionary<string, T> dic, int db = 0);

        IDictionary<string, T> GetDic<T>(string[] keys, int db = 0) where T : class, new();

        Task<IDictionary<string, T>> GetDicAsync<T>(string[] keys, int db = 0) where T : class, new();

        bool RemoveDic(string[] keys, int db = 0);

        Task<bool> RemoveDicAsync(string[] keys, int db = 0);

        void Send(string queueName, string message, int db = 0);

        void SendAsync(string queueName, string message, int db = 0);

        string Receive(string queueName, int db = 0);

        Task<string> ReceiveAsync(string queueName, int db = 0);

        void Publish(string channel, string message, int db = 0);

        void PublishAsync(string channel, string message, int db = 0);

        void Receive(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0);

        void ReceiveAsync(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0);
    }
}
