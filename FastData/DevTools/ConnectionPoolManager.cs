using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData.Config;
using FastData.Context;

namespace FastData.DevTools
{
    /// <summary>
    /// 数据库连接池管理器
    /// </summary>
    public static class ConnectionPoolManager
    {
        private static readonly ConcurrentDictionary<string, ConnectionPool> _pools = new ConcurrentDictionary<string, ConnectionPool>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 创建连接池
        /// </summary>
        public static ConnectionPool CreatePool(string dbKey, ConnectionPoolOptions options = null)
        {
            if (_pools.ContainsKey(dbKey))
            {
                return _pools[dbKey];
            }

            lock (_lock)
            {
                if (_pools.ContainsKey(dbKey))
                {
                    return _pools[dbKey];
                }

                options = options ?? ConnectionPoolOptions.Default;
                var config = DataConfig.GetConfig(dbKey);
                if (config == null)
                {
                    throw new ArgumentException($"找不到数据库配置: {dbKey}");
                }

                var pool = new ConnectionPool(dbKey, config, options);
                _pools.TryAdd(dbKey, pool);
                return pool;
            }
        }

        /// <summary>
        /// 获取连接池
        /// </summary>
        public static ConnectionPool GetPool(string dbKey)
        {
            if (_pools.TryGetValue(dbKey, out var pool))
            {
                return pool;
            }
            return CreatePool(dbKey);
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        public static PooledConnection GetConnection(string dbKey)
        {
            var pool = GetPool(dbKey);
            return pool.GetConnection();
        }

        /// <summary>
        /// 异步获取连接
        /// </summary>
        public static async Task<PooledConnection> GetConnectionAsync(string dbKey)
        {
            var pool = GetPool(dbKey);
            return await pool.GetConnectionAsync();
        }

        /// <summary>
        /// 获取连接池统计
        /// </summary>
        public static ConnectionPoolStats GetStats(string dbKey)
        {
            var pool = GetPool(dbKey);
            return pool.GetStats();
        }

        /// <summary>
        /// 清理连接池
        /// </summary>
        public static void Cleanup(string dbKey)
        {
            if (_pools.TryGetValue(dbKey, out var pool))
            {
                pool.Cleanup();
            }
        }

        /// <summary>
        /// 清理所有连接池
        /// </summary>
        public static void CleanupAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Cleanup();
            }
        }

        /// <summary>
        /// 重置连接池
        /// </summary>
        public static void Reset(string dbKey)
        {
            if (_pools.TryRemove(dbKey, out var pool))
            {
                pool.Dispose();
            }
        }

        /// <summary>
        /// 重置所有连接池
        /// </summary>
        public static void ResetAll()
        {
            foreach (var key in _pools.Keys.ToList())
            {
                Reset(key);
            }
        }
    }

    /// <summary>
    /// 连接池
    /// </summary>
    public class ConnectionPool : IDisposable
    {
        private readonly string _dbKey;
        internal readonly object _config;
        private readonly ConnectionPoolOptions _options;
        private readonly ConcurrentBag<PooledConnection> _idleConnections = new ConcurrentBag<PooledConnection>();
        private readonly ConcurrentBag<PooledConnection> _activeConnections = new ConcurrentBag<PooledConnection>();
        private int _totalConnections;
        private bool _disposed;

        internal ConnectionPool(string dbKey, object config, ConnectionPoolOptions options)
        {
            _dbKey = dbKey;
            _config = config;
            _options = options;
        }

        public PooledConnection GetConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionPool));

            // 尝试从空闲连接获取
            if (_idleConnections.TryTake(out var connection))
            {
                if (connection.IsHealthy())
                {
                    _activeConnections.Add(connection);
                    return connection;
                }
                else
                {
                    connection.Dispose();
                    Interlocked.Decrement(ref _totalConnections);
                }
            }

            // 创建新连接
            if (_totalConnections < _options.MaxPoolSize || _options.MaxPoolSize <= 0)
            {
                connection = CreateNewConnection();
                _activeConnections.Add(connection);
                return connection;
            }

            // 等待空闲连接
            throw new TimeoutException($"连接池已满，无法获取连接 (MaxPoolSize: {_options.MaxPoolSize})");
        }

        public async Task<PooledConnection> GetConnectionAsync()
        {
            return await Task.Run(() => GetConnection());
        }

        private PooledConnection CreateNewConnection()
        {
            var db = new DataContext(_dbKey);
            var connection = new PooledConnection(db, this);
            Interlocked.Increment(ref _totalConnections);
            return connection;
        }

        internal void ReturnConnection(PooledConnection connection)
        {
            if (connection.IsHealthy() && !_disposed)
            {
                _activeConnections.TryTake(out _);
                _idleConnections.Add(connection);
            }
            else
            {
                _activeConnections.TryTake(out _);
                connection.Dispose();
                Interlocked.Decrement(ref _totalConnections);
            }
        }

        public ConnectionPoolStats GetStats()
        {
            return new ConnectionPoolStats
            {
                DbKey = _dbKey,
                TotalConnections = _totalConnections,
                ActiveConnections = _activeConnections.Count,
                IdleConnections = _idleConnections.Count,
                MaxPoolSize = _options.MaxPoolSize,
                MinPoolSize = _options.MinPoolSize
            };
        }

        public void Cleanup()
        {
            while (_idleConnections.TryTake(out var connection))
            {
                connection.Dispose();
                Interlocked.Decrement(ref _totalConnections);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            Cleanup();

            while (_activeConnections.TryTake(out var connection))
            {
                connection.Dispose();
                Interlocked.Decrement(ref _totalConnections);
            }
        }
    }

    /// <summary>
    /// 池化连接
    /// </summary>
    public class PooledConnection : IDisposable
    {
        private readonly DataContext _context;
        private readonly ConnectionPool _pool;
        private bool _disposed;

        public PooledConnection(DataContext context, ConnectionPool pool)
        {
            _context = context;
            _pool = pool;
            CreatedAt = DateTime.Now;
        }

        public DateTime CreatedAt { get; }
        public DataContext Context => _context;
        public IDbConnection Connection => _context.conn;
        public IDbCommand Command => _context.cmd;

        public bool IsHealthy()
        {
            try
            {
                if (Connection == null || Connection.State != ConnectionState.Open)
                    return false;

                return !(Connection is IDbConnection conn && DateTime.Now - CreatedAt > TimeSpan.FromMinutes(30));
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (_pool != null && !_disposed)
            {
                _pool.ReturnConnection(this);
            }
            else
            {
                _context?.Dispose();
            }
        }
    }

    /// <summary>
    /// 连接池选项
    /// </summary>
    public class ConnectionPoolOptions
    {
        public int MinPoolSize { get; set; } = 5;
        public int MaxPoolSize { get; set; } = 100;
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
        public bool EnableStatistics { get; set; } = true;

        public static ConnectionPoolOptions Default => new ConnectionPoolOptions();
    }

    /// <summary>
    /// 连接池统计
    /// </summary>
    public class ConnectionPoolStats
    {
        public string DbKey { get; set; }
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public int MaxPoolSize { get; set; }
        public int MinPoolSize { get; set; }

        public double UsagePercentage => MaxPoolSize > 0 ? (double)TotalConnections / MaxPoolSize * 100 : 0;

        public override string ToString()
        {
            return $"DbKey: {DbKey}, Active: {ActiveConnections}, Idle: {IdleConnections}, Total: {TotalConnections}/{MaxPoolSize} ({UsagePercentage:F1}%)";
        }
    }
}