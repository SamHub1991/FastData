#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Caching;
using Newtonsoft.Json;
using FastRedis.Config;
using System.Reflection;
using System.IO;
using FastUntility.Base;

namespace FastRedis
{
    /// <summary>
    /// redis操作类（NewLife.Redis 实现，单例模式）
    /// </summary>
    public static class RedisInfo
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
        public static FullRedis Redis => _redisLazy.Value;

        /// <summary>
        /// 资源文件初始化
        /// </summary>
        public static void Init(string dbFile = "db.config", string projectName = null)
        {
            if (projectName == null)
                projectName = Assembly.GetCallingAssembly().GetName().Name;
            RedisConfig.GetConfig(projectName, dbFile);
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        public static bool Exists(string key, int db = 0)
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

        /// <summary>
        /// 是否存在 asy
        /// </summary>
        public static Task<bool> ExistsAsync(string key, int db = 0)
        {
            return Task.FromResult(Exists(key, db));
        }

        /// <summary>
        /// 设置值 item
        /// </summary>
        public static bool Set<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
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

        /// <summary>
        /// 设置值 item asy
        /// </summary>
        public static Task<bool> SetAsync<T>(string key, T model, int hours = 24 * 30 * 12, int db = 0)
        {
            return Task.FromResult(Set(key, model, hours, db));
        }

        /// <summary>
        /// 设置值 item (string)
        /// </summary>
        public static bool Set(string key, string model, int hours = 24 * 30 * 12, int db = 0)
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

        /// <summary>
        /// 设置值 item asy (string)
        /// </summary>
        public static Task<bool> SetAsync(string key, string model, int hours = 24 * 30 * 12, int db = 0)
        {
            return Task.FromResult(Set(key, model, hours, db));
        }

        /// <summary>
        /// 设置值 item (分钟)
        /// </summary>
        public static bool Set(string key, string model, double Minutes, int db = 0)
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

        /// <summary>
        /// 获取值 item
        /// </summary>
        public static string Get(string key, int db = 0)
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

        /// <summary>
        /// 获取值 item asy
        /// </summary>
        public static Task<string> GetAsync(string key, int db = 0)
        {
            return Task.FromResult(Get(key, db));
        }

        /// <summary>
        /// 获取值 item (泛型)
        /// </summary>
        public static T Get<T>(string key, int db = 0) where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return null;
                var json = Redis.Get<string>(key);
                if (string.IsNullOrEmpty(json))
                    return null;
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Get<T>", key);
                return null;
            }
        }

        /// <summary>
        /// 删除值 item
        /// </summary>
        public static bool Remove(string key, int db = 0)
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

        /// <summary>
        /// 删除值 item asy
        /// </summary>
        public static Task<bool> RemoveAsync(string key, int db = 0)
        {
            return Task.FromResult(Remove(key, db));
        }

        /// <summary>
        /// 设置值 Dic（批量）
        /// </summary>
        public static bool SetDic<T>(Dictionary<string, T> dic, int db = 0)
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

        /// <summary>
        /// 获取值 dic（批量）
        /// </summary>
        public static IDictionary<string, T> GetDic<T>(string[] keys, int db = 0) where T : class, new()
        {
            try
            {
                var result = Redis.GetAll<T>(keys);
                return result;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetDic<T>", "");
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 删除值 dic（批量）
        /// </summary>
        public static bool RemoveDic(string[] keys, int db = 0)
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

        /// <summary>
        /// 获取或添加（缓存不存在时添加）
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> factory, int hours = 24 * 30 * 12) where T : class, new()
        {
            try
            {
                if (Redis.ContainsKey(key))
                    return Get<T>(key);

                var value = factory();
                Set(key, value, hours);
                return value;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetOrAdd<T>", key);
                return new T();
            }
        }

        /// <summary>
        /// 递增
        /// </summary>
        public static long Increment(string key, int value = 1)
        {
            try
            {
                return Redis.Increment(key, value);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Increment", key);
                return 0;
            }
        }

        /// <summary>
        /// 递减
        /// </summary>
        public static long Decrement(string key, int value = 1)
        {
            try
            {
                return Redis.Decrement(key, value);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "Decrement", key);
                return 0;
            }
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        public static bool SetExpire(string key, TimeSpan expire)
        {
            try
            {
                Redis.SetExpire(key, expire);
                return true;
            }
            catch (Exception ex)
            {
                SaveLog(ex, "SetExpire", key);
                return false;
            }
        }

        /// <summary>
        /// 获取过期时间
        /// </summary>
        public static TimeSpan GetExpire(string key)
        {
            try
            {
                return Redis.GetExpire(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetExpire", key);
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 获取列表集合
        /// </summary>
        public static IList<T> GetList<T>(string key) where T : class
        {
            try
            {
                return Redis.GetList<T>(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetList<T>", key);
                return new List<T>();
            }
        }

        /// <summary>
        /// 获取哈希集合
        /// </summary>
        public static IDictionary<string, T> GetDictionary<T>(string key) where T : class
        {
            try
            {
                return Redis.GetDictionary<T>(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetDictionary<T>", key);
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 获取 Set 集合
        /// </summary>
        public static ICollection<T> GetSet<T>(string key) where T : class
        {
            try
            {
                return Redis.GetSet<T>(key);
            }
            catch (Exception ex)
            {
                SaveLog(ex, "GetSet<T>", key);
                return new HashSet<T>();
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
