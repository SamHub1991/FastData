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
            
            // 验证配置
            if (string.IsNullOrEmpty(config.WriteServerList))
            {
                throw new InvalidOperationException("Redis 配置无效：WriteServerList 不能为空，请在 db.config 中配置 Redis 服务器地址");
            }
            
            var redis = new FullRedis();
            redis.Init(config.WriteServerList);
            return redis;
        });

        /// <summary>
        /// 获取 Redis 单例实例
        /// </summary>
        private static FullRedis Redis => _redisLazy.Value;

        /// <summary>
        /// 切换到指定的 Redis 数据库索引
        /// </summary>
        /// <param name="db">数据库索引</param>
        /// <returns>切换前的数据库索引</returns>
        private static int SwitchDatabase(int db)
        {
            if (db < 0 || db > 15)
                throw new ArgumentException("Redis 数据库索引必须在 0-15 之间", nameof(db));
            
            var previousDb = Redis.Db;
            if (previousDb != db)
            {
                Redis.Db = db;
            }
            return previousDb;
        }

        public bool Exists(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    return Redis.ContainsKey(key);
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Exists", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key, int db = 0)
        {
            try
            {
                return await Task.Run(() => Exists(key, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "ExistsAsync", key);
                return false;
            }
        }

        public bool Set<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.Set(key, model, TimeSpan.FromHours(hours));
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set<T>", key);
                return false;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                return await Task.Run(() => Set(key, model, hours, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetAsync<T>", key);
                return false;
            }
        }

        public bool Set(string key, string model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.Set(key, model, TimeSpan.FromHours(hours));
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set", key);
                return false;
            }
        }

        public async Task<bool> SetAsync(string key, string model, int hours = 24 * 30 * 12, int db = 0)
        {
            try
            {
                return await Task.Run(() => Set(key, model, hours, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetAsync", key);
                return false;
            }
        }

        public bool Set(string key, string model, double Minutes, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.Set(key, model, TimeSpan.FromMinutes(Minutes));
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Set", key);
                return false;
            }
        }

        public async Task<bool> SetAsync(string key, string model, double Minutes, int db = 0)
        {
            try
            {
                return await Task.Run(() => Set(key, model, Minutes, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetAsync", key);
                return false;
            }
        }

        public string Get(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return "";
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    return Redis.Get<string>(key);
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Get", key);
                return "";
            }
        }

        public async Task<string> GetAsync(string key, int db = 0)
        {
            try
            {
                return await Task.Run(() => Get(key, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetAsync", key);
                return "";
            }
        }

        public T Get<T>(string key, int db = 0) where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return new T();
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    var json = Redis.Get<string>(key);
                    if (string.IsNullOrEmpty(json))
                        return new T();
                    return JsonConvert.DeserializeObject<T>(json);
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Get<T>", key);
                return new T();
            }
        }

        public async Task<T> GetAsync<T>(string key, int db = 0) where T : class, new()
        {
            try
            {
                return await Task.Run(() => Get<T>(key, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetAsync<T>", key);
                return new T();
            }
        }

        public bool Remove(string key, int db = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.Remove(key);
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Remove", key);
                return false;
            }
        }

        public async Task<bool> RemoveAsync(string key, int db = 0)
        {
            try
            {
                return await Task.Run(() => Remove(key, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "RemoveAsync", key);
                return false;
            }
        }

        public bool SetDic<T>(Dictionary<string, T> dic, int db = 0)
        {
            try
            {
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.SetAll(dic);
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetDic<T>", "");
                return false;
            }
        }

        public async Task<bool> SetDicAsync<T>(Dictionary<string, T> dic, int db = 0)
        {
            try
            {
                return await Task.Run(() => SetDic(dic, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetDicAsync<T>", "");
                return false;
            }
        }

        public IDictionary<string, T> GetDic<T>(string[] keys, int db = 0) where T : class, new()
        {
            try
            {
                var previousDb = SwitchDatabase(db);
                try
                {
                    return Redis.GetAll<T>(keys);
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetDic<T>", "");
                return new Dictionary<string, T>();
            }
        }

        public async Task<IDictionary<string, T>> GetDicAsync<T>(string[] keys, int db = 0) where T : class, new()
        {
            try
            {
                return await Task.Run(() => GetDic<T>(keys, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetDicAsync<T>", "");
                return new Dictionary<string, T>();
            }
        }

        public bool RemoveDic(string[] keys, int db = 0)
        {
            try
            {
                var previousDb = SwitchDatabase(db);
                try
                {
                    Redis.Remove(keys);
                    return true;
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "RemoveDic", "");
                return false;
            }
        }

        public async Task<bool> RemoveDicAsync(string[] keys, int db = 0)
        {
            try
            {
                return await Task.Run(() => RemoveDic(keys, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "RemoveDicAsync", "");
                return false;
            }
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
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    var list = Redis.GetList<string>(queueName);
                    list.Add(message);
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
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
                
                var previousDb = SwitchDatabase(db);
                try
                {
                    var list = Redis.GetList<string>(queueName);
                    if (list.Count > 0)
                    {
                        var item = list[0];
                        list.RemoveAt(0);
                        return item;
                    }
                    return "";
                }
                finally
                {
                    SwitchDatabase(previousDb);
                }
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Receive", "");
                return "";
            }
        }

        public async Task<string> ReceiveAsync(string queueName, int db = 0)
        {
            try
            {
                return await Task.Run(() => Receive(queueName, db));
            }
            catch (Exception ex)
            {
                SaveLog(ex, "ReceiveAsync", "");
                return "";
            }
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