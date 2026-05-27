using System;
using FastUntility.Base;

#if NETFRAMEWORK
using System.Runtime.Caching;
#else
using Microsoft.Extensions.Caching.Memory;
#endif

namespace FastUntility.Cache
{
    /// <summary>
    /// 缓存
    /// </summary>
    public static class BaseCache
    {
#if NETFRAMEWORK
        public static ObjectCache cache = MemoryCache.Default;
#else
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
#endif

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="Hours">过期小时</param>
        public static void Set(string key, string value, int Hours = 24 * 30 * 12)
        {
            if (!string.IsNullOrEmpty(key))
            {
#if NETFRAMEWORK
                cache.Remove(key);
                var policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTime.Now.AddHours(Hours);
                cache.Set(key, value, policy);
#else
                _cache.Remove(key);
                var options = new MemoryCacheEntryOptions();
                options.AbsoluteExpiration = DateTimeOffset.Now.AddHours(Hours);
                _cache.Set(key, value, options);
#endif
            }
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="Hours">过期小时</param>
        public static void Set<T>(string key, T value, int Hours = 24 * 30 * 12) where T : class, new()
        {
            if (!string.IsNullOrEmpty(key))
            {
#if NETFRAMEWORK
                cache.Remove(key);
                var policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTime.Now.AddHours(Hours);
                cache.Set(key, value, policy);
#else
                _cache.Remove(key);
                var options = new MemoryCacheEntryOptions();
                options.AbsoluteExpiration = DateTimeOffset.Now.AddHours(Hours);
                _cache.Set(key, value, options);
#endif
            }
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">键</param>
        public static string Get(string key)
        {
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
#if NETFRAMEWORK
                    return cache.Get(key).ToStr();
#else
                    return _cache.Get<string>(key);
#endif
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">键</param>
        public static T Get<T>(string key) where T : class, new()
        {
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
#if NETFRAMEWORK
                    var result = new T();
                    var obj = cache.Get(key);
                    if (obj != null)
                        result = (T)obj;
                    return result;
#else
                    return _cache.Get<T>(key) ?? new T();
#endif
                }
                else
                    return new T();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">键</param>
        public static void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
#if NETFRAMEWORK
                cache.Remove(key);
#else
                _cache.Remove(key);
#endif
            }
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="key">键</param>
        public static bool Exists(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
#if NETFRAMEWORK
                return cache.Contains(key);
#else
                return _cache.TryGetValue(key, out _);
#endif
            }
            else
                return false;
        }
    }
}
