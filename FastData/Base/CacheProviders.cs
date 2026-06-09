using System;
using System.Collections.Concurrent;

namespace FastData.Base
{
    /// <summary>
    /// 内存缓存提供者
    /// 基于 FastUntility.Cache.BaseCache 实现
    /// </summary>
    internal class MemoryCacheProvider : ICacheProvider
    {
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="hours">缓存时间（小时）</param>
        public void Set(string key, string value, int hours)
        {
            FastUntility.Cache.BaseCache.Set(key, value, hours);
        }

        /// <summary>
        /// 设置泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存实体对象</param>
        /// <param name="hours">缓存时间（小时）</param>
        public void Set<T>(string key, T value, int hours) where T : class, new()
        {
            FastUntility.Cache.BaseCache.Set<T>(key, value, hours);
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值；缓存不存在时返回 null</returns>
        public string Get(string key)
        {
            if (!Exists(key))
                return null;
            return FastUntility.Cache.BaseCache.Get(key);
        }

        /// <summary>
        /// 获取泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存实体对象；缓存不存在时返回 default(T)</returns>
        public T Get<T>(string key) where T : class, new()
        {
            return Exists(key) ? FastUntility.Cache.BaseCache.Get<T>(key) : default(T);
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存存在返回 true，否则返回 false</returns>
        public bool Exists(string key)
        {
            return FastUntility.Cache.BaseCache.Exists(key);
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        public void Remove(string key)
        {
            FastUntility.Cache.BaseCache.Remove(key);
        }
    }

    /// <summary>
    /// Redis 缓存提供者
    /// 基于 FastRedis.RedisInfo 实现
    /// </summary>
    internal class RedisCacheProvider : ICacheProvider
    {
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="hours">缓存时间（小时）</param>
        public void Set(string key, string value, int hours)
        {
            FastRedis.RedisInfo.Set(key, value, hours);
        }

        /// <summary>
        /// 设置泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存实体对象</param>
        /// <param name="hours">缓存时间（小时）</param>
        public void Set<T>(string key, T value, int hours) where T : class, new()
        {
            FastRedis.RedisInfo.Set<T>(key, value, hours);
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值；缓存不存在时返回 null</returns>
        public string Get(string key)
        {
            if (!Exists(key))
                return null;
            return FastRedis.RedisInfo.Get(key);
        }

        /// <summary>
        /// 获取泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存实体对象；缓存不存在时返回 default(T)</returns>
        public T Get<T>(string key) where T : class, new()
        {
            return Exists(key) ? FastRedis.RedisInfo.Get<T>(key) : default(T);
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存存在返回 true，否则返回 false</returns>
        public bool Exists(string key)
        {
            return FastRedis.RedisInfo.Exists(key);
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        public void Remove(string key)
        {
            FastRedis.RedisInfo.Remove(key);
        }
    }

    /// <summary>
    /// 缓存提供者工厂
    /// 使用注册表模式管理缓存类型到提供者的映射
    /// </summary>
    internal static class CacheProviderFactory
    {
        /// <summary>
        /// 默认缓存时间（小时），1 天
        /// </summary>
        private const int DefaultHours = 24;

        /// <summary>
        /// 缓存提供者注册表，线程安全
        /// </summary>
        private static readonly ConcurrentDictionary<string, ICacheProvider> Providers = new ConcurrentDictionary<string, ICacheProvider>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 静态构造函数，注册内置缓存提供者
        /// </summary>
        static CacheProviderFactory()
        {
            Register(CacheType.Web, new MemoryCacheProvider());
            Register(CacheType.Redis, new RedisCacheProvider());
        }

        /// <summary>
        /// 注册缓存提供者
        /// </summary>
        /// <param name="cacheType">缓存类型标识</param>
        /// <param name="provider">缓存提供者实例</param>
        public static void Register(string cacheType, ICacheProvider provider)
        {
            Providers[cacheType] = provider;
        }

        /// <summary>
        /// 获取指定类型的缓存提供者
        /// </summary>
        /// <param name="cacheType">缓存类型标识</param>
        /// <returns>对应的缓存提供者实例；未注册时返回 null</returns>
        public static ICacheProvider GetProvider(string cacheType)
        {
            Providers.TryGetValue(cacheType, out var provider);
            return provider;
        }

        /// <summary>
        /// 获取默认缓存时间（小时）
        /// </summary>
        public static int GetDefaultHours()
        {
            return DefaultHours;
        }
    }
}
