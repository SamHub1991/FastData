using System;
using FastData.Context;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastData.DevTools
{
    /// <summary>
    /// 分布式锁管理器
    /// </summary>
    public static class DistributedLockManager
    {
        private static readonly ConcurrentDictionary<string, LockInfo> _locks = new ConcurrentDictionary<string, LockInfo>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 尝试获取锁
        /// </summary>
        public static bool TryAcquire(string lockKey, TimeSpan timeout, out LockHandle lockHandle, string lockValue = null)
        {
            lockHandle = null;
            lockValue = lockValue ?? Guid.NewGuid().ToString();

            var deadline = DateTime.Now.Add(timeout);

            while (DateTime.Now < deadline)
            {
                if (_locks.TryAdd(lockKey, new LockInfo
                {
                    LockKey = lockKey,
                    LockValue = lockValue,
                    AcquiredAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(30) // 默认30分钟过期
                }))
                {
                    lockHandle = new LockHandle(lockKey, lockValue);
                    return true;
                }

                // 检查锁是否过期
                if (_locks.TryGetValue(lockKey, out var existingLock))
                {
                    if (existingLock.ExpiresAt < DateTime.Now)
                    {
                        _locks.TryRemove(lockKey, out _);
                        continue;
                    }
                }

                Thread.Sleep(100);
            }

            return false;
        }

        /// <summary>
        /// 异步尝试获取锁
        /// </summary>
        public static async Task<(bool acquired, LockHandle lockHandle)> TryAcquireAsync(string lockKey, TimeSpan timeout, string lockValue = null)
        {
            return await Task.Run(() =>
            {
                var acquired = TryAcquire(lockKey, timeout, out var lockHandle, lockValue);
                return (acquired, lockHandle);
            });
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        public static bool Release(LockHandle lockHandle)
        {
            if (_locks.TryGetValue(lockHandle.LockKey, out var lockInfo) && lockInfo.LockValue == lockHandle.LockValue)
            {
                return _locks.TryRemove(lockHandle.LockKey, out _);
            }
            return false;
        }

        /// <summary>
        /// 延长锁的有效期
        /// </summary>
        public static bool Renew(LockHandle lockHandle, TimeSpan additionalTime)
        {
            if (_locks.TryGetValue(lockHandle.LockKey, out var lockInfo) && lockInfo.LockValue == lockHandle.LockValue)
            {
                lockInfo.ExpiresAt = DateTime.Now.Add(additionalTime);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查锁是否存在
        /// </summary>
        public static bool IsLocked(string lockKey)
        {
            return _locks.ContainsKey(lockKey);
        }

        /// <summary>
        /// 获取锁信息
        /// </summary>
        public static LockInfo GetLockInfo(string lockKey)
        {
            return _locks.TryGetValue(lockKey, out var lockInfo) ? lockInfo : null;
        }

        /// <summary>
        /// 清理过期锁
        /// </summary>
        public static int CleanupExpiredLocks()
        {
            var expiredKeys = _locks.Where(kvp => kvp.Value.ExpiresAt < DateTime.Now)
                                  .Select(kvp => kvp.Key)
                                  .ToList();

            foreach (var key in expiredKeys)
            {
                _locks.TryRemove(key, out _);
            }

            return expiredKeys.Count;
        }

        /// <summary>
        /// 强制释放锁
        /// </summary>
        public static bool ForceRelease(string lockKey)
        {
            return _locks.TryRemove(lockKey, out _);
        }

        /// <summary>
        /// 获取所有锁信息
        /// </summary>
        public static List<LockInfo> GetAllLocks()
        {
            return _locks.Values.ToList();
        }

        /// <summary>
        /// 使用锁执行操作
        /// </summary>
        public static T ExecuteWithLock<T>(string lockKey, Func<T> action, TimeSpan timeout, string lockValue = null)
        {
            if (TryAcquire(lockKey, timeout, out var lockHandle, lockValue))
            {
                try
                {
                    return action();
                }
                finally
                {
                    Release(lockHandle);
                }
            }

            throw new TimeoutException(string.Format("无法在 {0} 内获取锁: {1}", timeout, lockKey));
        }

        /// <summary>
        /// 异步使用锁执行操作
        /// </summary>
        public static async Task<T> ExecuteWithLockAsync<T>(string lockKey, Func<Task<T>> action, TimeSpan timeout, string lockValue = null)
        {
            var (acquired, lockHandle) = await TryAcquireAsync(lockKey, timeout, lockValue);

            if (acquired)
            {
                try
                {
                    return await action();
                }
                finally
                {
                    Release(lockHandle);
                }
            }

            throw new TimeoutException(string.Format("无法在 {0} 内获取锁: {1}", timeout, lockKey));
        }

        /// <summary>
        /// 获取锁统计信息
        /// </summary>
        public static LockStatistics GetStatistics()
        {
            var locks = _locks.Values.ToList();
            return new LockStatistics
            {
                TotalLocks = locks.Count,
                ActiveLocks = locks.Count(l => l.ExpiresAt > DateTime.Now),
                ExpiredLocks = locks.Count(l => l.ExpiresAt <= DateTime.Now),
                AverageLockDuration = locks.Any() ? locks.Average(l => (l.ExpiresAt - l.AcquiredAt).TotalMinutes) : 0
            };
        }
    }

    /// <summary>
    /// 锁句柄
    /// </summary>
    public class LockHandle : IDisposable
    {
        public string LockKey { get; set; }
        public string LockValue { get; set; }
        private bool _disposed;

        public LockHandle(string lockKey, string lockValue)
        {
            LockKey = lockKey;
            LockValue = lockValue;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DistributedLockManager.Release(this);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 锁信息
    /// </summary>
    public class LockInfo
    {
        public string LockKey { get; set; }
        public string LockValue { get; set; }
        public DateTime AcquiredAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// 锁统计信息
    /// </summary>
    public class LockStatistics
    {
        public int TotalLocks { get; set; }
        public int ActiveLocks { get; set; }
        public int ExpiredLocks { get; set; }
        public double AverageLockDuration { get; set; }
    }

    /// <summary>
    /// 锁作用域
    /// </summary>
    public class LockScope : IDisposable
    {
        private readonly LockHandle _lockHandle;

        public LockScope(LockHandle lockHandle)
        {
            _lockHandle = lockHandle;
        }

        public void Dispose()
        {
            _lockHandle?.Dispose();
        }
    }

    /// <summary>
    /// 分布式锁辅助类
    /// </summary>
    public static class DistributedLockHelper
    {
        /// <summary>
        /// 创建锁作用域
        /// </summary>
        public static LockScope CreateScope(string lockKey, TimeSpan timeout, string lockValue = null)
        {
            if (DistributedLockManager.TryAcquire(lockKey, timeout, out var lockHandle, lockValue))
            {
                return new LockScope(lockHandle);
            }

            throw new TimeoutException(string.Format("无法在 {0} 内获取锁: {1}", timeout, lockKey));
        }

        /// <summary>
        /// 异步创建锁作用域
        /// </summary>
        public static async Task<LockScope> CreateScopeAsync(string lockKey, TimeSpan timeout, string lockValue = null)
        {
            var (acquired, lockHandle) = await DistributedLockManager.TryAcquireAsync(lockKey, timeout, lockValue);

            if (acquired)
            {
                return new LockScope(lockHandle);
            }

            throw new TimeoutException(string.Format("无法在 {0} 内获取锁: {1}", timeout, lockKey));
        }

        /// <summary>
        /// 使用锁作用域执行操作
        /// </summary>
        public static T UsingLock<T>(string lockKey, Func<T> action, TimeSpan timeout, string lockValue = null)
        {
            using (CreateScope(lockKey, timeout, lockValue))
            {
                return action();
            }
        }

        /// <summary>
        /// 异步使用锁作用域执行操作
        /// </summary>
        public static async Task<T> UsingLockAsync<T>(string lockKey, Func<Task<T>> action, TimeSpan timeout, string lockValue = null)
        {
            using (await CreateScopeAsync(lockKey, timeout, lockValue))
            {
                return await action();
            }
        }

        /// <summary>
        /// 尝试使用锁作用域执行操作
        /// </summary>
        public static bool TryUsingLock<T>(string lockKey, Func<T> action, TimeSpan timeout, out T result, string lockValue = null)
        {
            result = default;

            if (DistributedLockManager.TryAcquire(lockKey, timeout, out var lockHandle, lockValue))
            {
                try
                {
                    result = action();
                    return true;
                }
                finally
                {
                    DistributedLockManager.Release(lockHandle);
                }
            }

            return false;
        }

        /// <summary>
        /// 读写锁（简化版）
        /// </summary>
        public static class ReadWriteLock
        {
            private static readonly ConcurrentDictionary<string, object> _readLocks = new ConcurrentDictionary<string, object>();
            private static readonly ConcurrentDictionary<string, object> _writeLocks = new ConcurrentDictionary<string, object>();

            public static IDisposable AcquireReadLock(string resourceKey)
            {
                var readLock = _readLocks.GetOrAdd(resourceKey, new object());
                Monitor.Enter(readLock);
                return new ReadWriteLockScope(readLock, isReadLock: true);
            }

            public static IDisposable AcquireWriteLock(string resourceKey)
            {
                var writeLock = _writeLocks.GetOrAdd(resourceKey, new object());
                Monitor.Enter(writeLock);
                return new ReadWriteLockScope(writeLock, isReadLock: false);
            }

            private class ReadWriteLockScope : IDisposable
            {
                private readonly object _lock;
                private readonly bool _isReadLock;
                private bool _disposed;

                public ReadWriteLockScope(object @lock, bool isReadLock)
                {
                    _lock = @lock;
                    _isReadLock = isReadLock;
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        Monitor.Exit(_lock);
                        _disposed = true;
                    }
                }
            }
        }
    }
}