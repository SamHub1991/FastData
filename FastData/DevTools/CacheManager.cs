using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using FastData.Model;

namespace FastData.DevTools
{
    /// <summary>
    /// 二级缓存管理器
    /// </summary>
    public static class CacheManager
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private static readonly ConcurrentDictionary<string, HashSet<string>> _entityCacheKeys = new();

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public static T Get<T>(string key)
        {
            var item = _cache.Get(key);
            return item is T typedItem ? typedItem : default;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public static void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = expiration.HasValue
                    ? DateTimeOffset.Now.Add(expiration.Value)
                    : DateTimeOffset.Now.AddMinutes(30)
            };

            _cache.Set(key, value, policy);
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public static void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public static bool Exists(string key)
        {
            return _cache.Contains(key);
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public static void Clear()
        {
            foreach (var item in _cache)
            {
                _cache.Remove(item.Key);
            }
            _entityCacheKeys.Clear();
        }

        /// <summary>
        /// 获取或创建缓存项
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            var item = _cache.Get(key);
            if (item is T typedItem)
            {
                return typedItem;
            }

            var value = factory();
            Set(key, value, expiration);
            return value;
        }

        /// <summary>
        /// 注册实体缓存键
        /// </summary>
        public static void RegisterEntityCache<T>(string cacheKey)
        {
            var entityName = typeof(T).Name;
            if (!_entityCacheKeys.ContainsKey(entityName))
            {
                _entityCacheKeys[entityName] = new HashSet<string>();
            }
            _entityCacheKeys[entityName].Add(cacheKey);
        }

        /// <summary>
        /// 清除实体相关缓存
        /// </summary>
        public static void ClearEntityCache<T>()
        {
            var entityName = typeof(T).Name;
            if (_entityCacheKeys.TryGetValue(entityName, out var keys))
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }
                _entityCacheKeys[entityName].Clear();
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStats GetStats()
        {
            var keys = _cache.Select(kvp => kvp.Key).ToList();
            return new CacheStats
            {
                TotalKeys = keys.Count,
                MemoryLimit = _cache.CacheMemoryLimit,
                PhysicalMemoryLimit = _cache.PhysicalMemoryLimit,
                PollingInterval = _cache.PollingInterval,
                Keys = keys
            };
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStats
    {
        public int TotalKeys { get; set; }
        public long MemoryLimit { get; set; }
        public long PhysicalMemoryLimit { get; set; }
        public TimeSpan PollingInterval { get; set; }
        public List<string> Keys { get; set; } = new List<string>();
    }

    /// <summary>
    /// 查询缓存装饰器
    /// </summary>
    public static class QueryCacheDecorator
    {
        /// <summary>
        /// 带缓存的查询
        /// </summary>
        public static List<T> QueryWithCache<T>(
            System.Linq.Expressions.Expression<Func<T, bool>> expression,
            TimeSpan? cacheDuration = null,
            string dbKey = null,
            string cacheKey = null) where T : class, new()
        {
            var key = cacheKey ?? GenerateCacheKey(expression, dbKey);

            var cached = CacheManager.Get<List<T>>(key);
            if (cached != null)
            {
                return cached;
            }

            var result = FastData.Read.Read.Query<T>(dbKey).Where(expression).ToList();
            CacheManager.Set(key, result, cacheDuration);
            CacheManager.RegisterEntityCache<T>(key);

            return result;
        }

        /// <summary>
        /// 带缓存的列表查询
        /// </summary>
        public static List<T> ListWithCache<T>(
            TimeSpan? cacheDuration = null,
            string dbKey = null,
            string cacheKey = null) where T : class, new()
        {
            var key = cacheKey ?? $"List_{typeof(T).Name}_{dbKey ?? "default"}";

            var cached = CacheManager.Get<List<T>>(key);
            if (cached != null)
            {
                return cached;
            }

            var result = FastData.Read.Read.List<T>(dbKey);
            CacheManager.Set(key, result, cacheDuration);
            CacheManager.RegisterEntityCache<T>(key);

            return result;
        }

        /// <summary>
        /// 带缓存的单条查询
        /// </summary>
        public static T FirstWithCache<T>(
            System.Linq.Expressions.Expression<Func<T, bool>> expression,
            TimeSpan? cacheDuration = null,
            string dbKey = null,
            string cacheKey = null) where T : class, new()
        {
            var key = cacheKey ?? GenerateCacheKey(expression, dbKey);

            var cached = CacheManager.Get<T>(key);
            if (cached != null)
            {
                return cached;
            }

            var result = FastData.Read.Read.Query<T>(dbKey).FirstOrDefault(expression);
            if (result != null)
            {
                CacheManager.Set(key, result, cacheDuration);
                CacheManager.RegisterEntityCache<T>(key);
            }

            return result;
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private static string GenerateCacheKey<T>(System.Linq.Expressions.Expression<Func<T, bool>> expression, string dbKey)
        {
            var exprStr = expression?.ToString() ?? "all";
            return $"Query_{typeof(T).Name}_{exprStr.GetHashCode()}_{dbKey ?? "default"}";
        }
    }

    /// <summary>
    /// 缓存自动失效拦截器
    /// </summary>
    public static class CacheInvalidationInterceptor
    {
        /// <summary>
        /// 拦截添加操作并使缓存失效
        /// </summary>
        public static Result InterceptAdd<T>(T entity, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Add<T>(entity, dbKey);
            if (result.IsSuccess)
            {
                CacheManager.ClearEntityCache<T>();
            }
            return result;
        }

        /// <summary>
        /// 拦截更新操作并使缓存失效
        /// </summary>
        public static Result InterceptUpdate<T>(T entity, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Update<T>(entity, dbKey);
            if (result.IsSuccess)
            {
                CacheManager.ClearEntityCache<T>();
            }
            return result;
        }

        /// <summary>
        /// 拦截删除操作并使缓存失效
        /// </summary>
        public static Result InterceptDelete<T>(System.Linq.Expressions.Expression<Func<T, bool>> expression, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Delete<T>(expression, dbKey);
            if (result.IsSuccess)
            {
                CacheManager.ClearEntityCache<T>();
            }
            return result;
        }

        /// <summary>
        /// 拦截批量添加操作并使缓存失效
        /// </summary>
        public static Result InterceptAddRange<T>(IEnumerable<T> entities, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.AddRange(entities, dbKey);
            if (result.IsSuccess)
            {
                CacheManager.ClearEntityCache<T>();
            }
            return result;
        }
    }
}