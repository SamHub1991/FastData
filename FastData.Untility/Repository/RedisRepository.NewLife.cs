#if !NETFRAMEWORK
using FastRedis.Config;
using NewLife.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FastUntility.Base;

namespace FastRedis.Repository
{
    /// <summary>
    /// Redis 仓储实现（单例模式）
    /// </summary>
    public class RedisRepository : IRedisRepository
    {
        private static readonly Lazy<FullRedis> _redisLazy = new Lazy<FullRedis>(() =>
        {
            var config = RedisConfig.GetConfig();
            var redis = new FullRedis();
            redis.Init(config.WriteServerList);
            return redis;
        });

        /// <summary>
        /// 获取 Redis 单例实例
        /// </summary>
        private static FullRedis Redis => _redisLazy.Value;

        public bool Exists(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                return Redis.ContainsKey(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Exists", key);
                return false;
            }
        }

        public Task<bool> ExistsAsync(string key, int db = 0)
        {
            return Task.FromResult(Exists(key, db));
        }

        public bool Set<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                Redis.Set(key, model, TimeSpan.FromHours(hours));
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set<T>", key);
                return false;
            }
        }

        public Task<bool> SetAsync<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
        {
            return Task.FromResult(Set(key, model, hours, db));
        }

        public bool Set(string key, string model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                Redis.Set(key, model, TimeSpan.FromHours(hours));
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set", key);
                return false;
            }
        }

        public Task<bool> SetAsync(string key, string model, int hours = 24 * 30 * 12, int db = 0)
        {
            return Task.FromResult(Set(key, model, hours, db));
        }

        public bool Set(string key, string model, double Minutes, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                Redis.Set(key, model, TimeSpan.FromMinutes(Minutes));
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set", key);
                return false;
            }
        }

        public Task<bool> SetAsync(string key, string model, double Minutes, int db = 0)
        {
            return Task.FromResult(Set(key, model, Minutes, db));
        }

        public string Get(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return "";
                return Redis.Get<string>(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Get", key);
                return "";
            }
        }

        public Task<string> GetAsync(string key, int db = 0)
        {
            return Task.FromResult(Get(key, db));
        }

        public T Get<T>(string key, int db = 0) where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return new T();
                var json = Redis.Get<string>(key);
                if (string.IsNullOrEmpty(json))
                    return new T();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Get<T>", key);
                return new T();
            }
        }

        public Task<T> GetAsync<T>(string key, int db = 0) where T : class, new()
        {
            return Task.FromResult(Get<T>(key, db));
        }

        public bool Remove(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                Redis.Remove(key);
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Remove", key);
                return false;
            }
        }

        public Task<bool> RemoveAsync(string key, int db = 0)
        {
            return Task.FromResult(Remove(key, db));
        }

        public bool SetDic<T>(Dictionary<string, T> dic, int db = 0)
        {
            try
            {
                Redis.SetAll(dic);
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetDic<T>", "");
                return false;
            }
        }

        public Task<bool> SetDicAsync<T>(Dictionary<string, T> dic, int db = 0)
        {
            return Task.FromResult(SetDic(dic, db));
        }

        public IDictionary<string, T> GetDic<T>(string[] keys, int db = 0) where T : class, new()
        {
            try
            {
                return Redis.GetAll<T>(keys);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetDic<T>", "");
                return new Dictionary<string, T>();
            }
        }

        public Task<IDictionary<string, T>> GetDicAsync<T>(string[] keys, int db = 0) where T : class, new()
        {
            return Task.FromResult(GetDic<T>(keys, db));
        }

        public bool RemoveDic(string[] keys, int db = 0)
        {
            try
            {
                Redis.Remove(keys);
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "RemoveDic", "");
                return false;
            }
        }

        public Task<bool> RemoveDicAsync(string[] keys, int db = 0)
        {
            return Task.FromResult(RemoveDic(keys, db));
        }

        // NewLife.Redis 暂不支持 Publish/Subscribe，使用空实现
        public void Publish(string channel, string message, int db = 0)
        {
            // NewLife.Redis pub/sub not supported in this version
        }

        public void PublishAsync(string channel, string message, int db = 0)
        {
            Publish(channel, message, db);
        }

        public void Receive(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0)
        {
            // NewLife.Redis pub/sub not supported in this version
        }

        public void ReceiveAsync(string channel, Action<string, string> message, Action<string> subscribe = null, Action<string> unSubscribe = null, int db = 0)
        {
            Receive(channel, message, subscribe, unSubscribe, db);
        }

        // NewLife.Redis List 操作
        public void Send(string queueName, string message, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(queueName))
                    return;
                var list = Redis.GetList<string>(queueName);
                list.Add(message);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Send", "");
            }
        }

        public void SendAsync(string queueName, string message, int db = 0)
        {
            Send(queueName, message, db);
        }

        public string Receive(string queueName, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(queueName))
                    return "";
                var list = Redis.GetList<string>(queueName);
                if (list.Count > 0)
                {
                    var item = list[0];
                    list.RemoveAt(0);
                    return item;
                }
                return "";
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Receive", "");
                return "";
            }
        }

        public Task<string> ReceiveAsync(string queueName, int db = 0)
        {
            return Task.FromResult(Receive(queueName, db));
        }

        #region 出错日志
        private static void SaveLog<T>(Exception ex, string CurrentMethod)
        {
            LogManager.SaveLog(string.Format("方法：{0},对象：{1},出错详情：{2}", CurrentMethod, typeof(T).Name, ex.ToString()), "redis_exp");
        }

        private static void SaveLog(Exception ex, string CurrentMethod, string key)
        {
            LogManager.SaveLog(string.Format("方法：{0},键：{1},出错详情：{2}", CurrentMethod, key, ex.ToString()), "redis_exp");
        }
        #endregion
    }
}
#endif
