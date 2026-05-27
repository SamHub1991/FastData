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
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new();
        Task<bool> SetAsync<T>(string key, T value, int hours = 24) where T : class;
        Task<T> GetAsync<T>(string key) where T : class, new();
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, int value = 1);
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
            var cached = RedisInfo.Get<T>(key);
            if (cached != null && !EqualityComparer<T>.Default.Equals(cached, default))
                return cached;

            // 从工厂方法获取
            var value = await factory();
            if (value != null)
            {
                RedisInfo.Set(key, value, hours);
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
            return await Task.FromResult(RedisInfo.Increment(key, value));
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
        Task<User> GetUserAsync(int userId, Func<Task<User>> factory);
        Task<List<User>> GetActiveUsersAsync(Func<Task<List<User>>> factory);
        Task RemoveUserAsync(int userId);
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

        public async Task<User> GetUserAsync(int userId, Func<Task<User>> factory)
        {
            var key = $"user:{userId}";
            return await _cacheService.GetOrSetAsync(key, factory, hours: 2);
        }

        public async Task<List<User>> GetActiveUsersAsync(Func<Task<List<User>>> factory)
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
