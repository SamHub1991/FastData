using FastData.Demo.Models;
using System;
using System.Collections.Concurrent;
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
    /// 内存缓存服务实现（Demo 用途，无外部依赖）
    /// </summary>
    public class InMemoryCacheService : ICacheService, IDisposable
    {
        private readonly ConcurrentDictionary<string, (object Value, DateTime ExpiresAt)> _store
            = new ConcurrentDictionary<string, (object, DateTime)>();

        private static readonly ConcurrentDictionary<string, long> _counters
            = new ConcurrentDictionary<string, long>();

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new()
        {
            if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
                return Task.FromResult((T)entry.Value);

            return SetAndReturnAsync(key, factory, hours);
        }

        private async Task<T> SetAndReturnAsync<T>(string key, Func<Task<T>> factory, int hours) where T : class, new()
        {
            var value = await factory();
            if (value != null)
            {
                var expiredAt = DateTime.UtcNow.AddHours(Math.Max(1, hours));
                _store[key] = (value, expiredAt);
            }
            return value;
        }

        public Task<bool> SetAsync<T>(string key, T value, int hours = 24) where T : class
        {
            var expiredAt = DateTime.UtcNow.AddHours(Math.Max(1, hours));
            _store[key] = (value, expiredAt);
            return Task.FromResult(true);
        }

        public Task<T> GetAsync<T>(string key) where T : class, new()
        {
            if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
                return Task.FromResult((T)entry.Value);
            return Task.FromResult(default(T));
        }

        public Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(_store.TryRemove(key, out var _));
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (_store.TryGetValue(key, out var entry))
                return Task.FromResult(entry.ExpiresAt > DateTime.UtcNow);
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, int value = 1)
        {
            var newValue = _counters.AddOrUpdate(key, value, (existingKey, existingValue) => existingValue + value);
            return Task.FromResult(newValue);
        }

        public Task<bool> SetExpireAsync(string key, TimeSpan expire)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                _store[key] = (entry.Value, DateTime.UtcNow.Add(expire));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public void Dispose()
        {
            _store.Clear();
            _counters.Clear();
        }
    }

    /// <summary>
    /// 用户缓存服务接口
    /// </summary>
    public interface IUserCacheService
    {
        Task<AppUser> GetUserAsync(int userId, Func<Task<AppUser>> factory);
        Task<List<AppUser>> GetActiveUsersAsync(Func<Task<List<AppUser>>> factory);
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