using System;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// 缓存
    /// </summary>
    internal static class DbCache
    {
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="hours">缓存时间（小时）</param>
        public static void Set(string cacheType, string key, string value, int hours = 24)
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            if (provider != null)
                provider.Set(key, value, hours);
        }

        /// <summary>
        /// 设置泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存实体对象</param>
        /// <param name="hours">缓存时间（小时），默认 24 小时（1 天）</param>
        public static void Set<T>(string cacheType, string key, T value, int hours = 24) where T : class, new()
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            if (provider != null)
                provider.Set<T>(key, value, hours);
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值；缓存不存在或类型未注册时返回 null</returns>
        public static string Get(string cacheType, string key)
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            return provider?.Get(key);
        }

        /// <summary>
        /// 获取泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        /// <returns>缓存实体对象；缓存不存在或类型未注册时返回 default(T)</returns>
        public static T Get<T>(string cacheType, string key) where T : class, new()
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            return provider != null ? provider.Get<T>(key) : default(T);
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        public static void Remove(string cacheType, string key)
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            if (provider != null)
                provider.Remove(key);
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="key">缓存键</param>
        /// <returns>缓存存在返回 true，否则返回 false</returns>
        public static bool Exists(string cacheType, string key)
        {
            var provider = CacheProviderFactory.GetProvider(cacheType);
            return provider != null && provider.Exists(key);
        }
    }
}
