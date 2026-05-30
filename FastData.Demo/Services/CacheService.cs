using FastData.Demo.Models;
using FastRedis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Services
{
    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或设置缓存
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据工厂方法</param>
        /// <param name="hours">过期时间（小时）</param>
        /// <returns>数据</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new();

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">数据</param>
        /// <param name="hours">过期时间（小时）</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync<T>(string key, T value, int hours = 24) where T : class;

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>数据</returns>
        Task<T> GetAsync<T>(string key) where T : class, new();

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 递增缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">递增值</param>
        /// <returns>递增后的值</returns>
        Task<long> IncrementAsync(string key, int value = 1);

        /// <summary>
        /// 设置缓存过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="expire">过期时间</param>
        /// <returns>是否成功</returns>
        Task<bool> SetExpireAsync(string key, TimeSpan expire);
    }

    /// <summary>
    /// 缓存服务实现（使用 NewLife.Redis）
    /// </summary>
    public class CacheService : ICacheService
    {
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new()
        {
            // 尝试从缓存获取
            try
            {
                var cached = RedisInfo.Get<T>(key);
                if (cached != null && !EqualityComparer<T>.Default.Equals(cached, default))
                    return cached;
            }
            catch
            {
                // Redis not available, fall through to factory
            }

            // 从工厂方法获取
            var value = await factory();
            if (value != null)
            {
                try
                {
                    RedisInfo.Set(key, value, hours);
                }
                catch
                {
                    // Redis not available, skip caching
                }
            }

            return value;
        }

        public async Task<bool> SetAsync<T>(string key, T value, int hours = 24) where T : class
        {
            return await Task.FromResult(RedisInfo.Set(key, value, hours));
        }

        public async Task<T> GetAsync<T>(string key) where T : class, new()
        {
            return await Task.FromResult(RedisInfo.Get<T>(key));
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await Task.FromResult(RedisInfo.Remove(key));
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.FromResult(RedisInfo.Exists(key));
        }

        public async Task<long> IncrementAsync(string key, int value = 1)
        {
            try
            {
                return await Task.FromResult(RedisInfo.Increment(key, value));
            }
            catch
            {
                return await Task.FromResult(0L);
            }
        }

        public async Task<bool> SetExpireAsync(string key, TimeSpan expire)
        {
            return await Task.FromResult(RedisInfo.SetExpire(key, expire));
        }
    }

    /// <summary>
    /// 用户缓存服务
    /// </summary>
    public interface IUserCacheService
    {
        /// <summary>
        /// 获取用户缓存
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="factory">数据工厂方法</param>
        /// <returns>用户信息</returns>
        Task<AppUser> GetUserAsync(int userId, Func<Task<AppUser>> factory);

        /// <summary>
        /// 获取活跃用户缓存
        /// </summary>
        /// <param name="factory">数据工厂方法</param>
        /// <returns>用户列表</returns>
        Task<List<AppUser>> GetActiveUsersAsync(Func<Task<List<AppUser>>> factory);

        /// <summary>
        /// 删除用户缓存
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <returns>任务</returns>
        Task RemoveUserAsync(int userId);

        /// <summary>
        /// 递增用户查看次数
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <returns>任务</returns>
        Task IncrementViewCountAsync(int userId);
    }

    /// <summary>
    /// 用户缓存服务实现
    /// </summary>
    public class UserCacheService : IUserCacheService
    {
        private readonly ICacheService _cacheService;

        public UserCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<AppUser> GetUserAsync(int userId, Func<Task<AppUser>> factory)
        {
            var key = $"user:{userId}";
            return await _cacheService.GetOrSetAsync(key, factory, hours: 2);
        }

        public async Task<List<AppUser>> GetActiveUsersAsync(Func<Task<List<AppUser>>> factory)
        {
            var key = "users:active";
            return await _cacheService.GetOrSetAsync(key, factory, hours: 1);
        }

        public async Task RemoveUserAsync(int userId)
        {
            await _cacheService.RemoveAsync($"user:{userId}");
            await _cacheService.RemoveAsync("users:active");
        }

        public async Task IncrementViewCountAsync(int userId)
        {
            await _cacheService.IncrementAsync($"user:{userId}:views");
        }
    }
}
