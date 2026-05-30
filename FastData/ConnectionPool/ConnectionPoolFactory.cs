using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

namespace FastData.ConnectionPool
{
    /// <summary>
    /// 连接池工厂
    /// </summary>
    public class ConnectionPoolFactory : IDisposable
    {
        private static readonly Lazy<ConnectionPoolFactory> _instance = 
            new Lazy<ConnectionPoolFactory>(() => new ConnectionPoolFactory());

        private readonly ConcurrentDictionary<string, SmartConnectionPool> _pools;
        private bool _disposed;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static ConnectionPoolFactory Instance => _instance.Value;

        private ConnectionPoolFactory()
        {
            _pools = new ConcurrentDictionary<string, SmartConnectionPool>();
        }

        /// <summary>
        /// 获取或创建连接池
        /// </summary>
        /// <param name="name">连接池名称</param>
        /// <param name="connectionFactory">连接工厂</param>
        /// <param name="config">连接池配置</param>
        /// <returns>智能连接池</returns>
        public SmartConnectionPool GetOrCreatePool(string name, Func<DbConnection> connectionFactory, ConnectionPoolConfig config = null)
        {
            return _pools.GetOrAdd(name, key => new SmartConnectionPool(key, connectionFactory, config));
        }

        /// <summary>
        /// 获取连接池
        /// </summary>
        public SmartConnectionPool GetPool(string name)
        {
            _pools.TryGetValue(name, out var pool);
            return pool;
        }

        /// <summary>
        /// 移除连接池
        /// </summary>
        public bool RemovePool(string name)
        {
            if (_pools.TryRemove(name, out var pool))
            {
                pool.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 关闭连接池
        /// </summary>
        public void ClosePool(string name)
        {
            if (_pools.TryRemove(name, out var pool))
            {
                pool.Dispose();
            }
        }

        /// <summary>
        /// 关闭所有连接池
        /// </summary>
        public void CloseAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }
            _pools.Clear();
        }

        /// <summary>
        /// 获取所有连接池指标
        /// </summary>
        public Dictionary<string, ConnectionPoolMetrics> GetAllMetrics()
        {
            var metrics = new Dictionary<string, ConnectionPoolMetrics>();
            foreach (var kvp in _pools)
            {
                metrics[kvp.Key] = kvp.Value.GetMetrics();
            }
            return metrics;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var pool in _pools.Values)
                {
                    pool.Dispose();
                }
                _pools.Clear();
            }
        }
    }
}
