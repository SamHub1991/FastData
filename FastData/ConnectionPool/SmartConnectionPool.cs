using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastData.ConnectionPool
{
    /// <summary>
    /// 连接池配置
    /// </summary>
    public class ConnectionPoolConfig
    {
        /// <summary>
        /// 最小连接数
        /// </summary>
        public int MinPoolSize { get; set; } = 10;

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 连接生命周期（分钟）
        /// </summary>
        public int ConnectionLifetime { get; set; } = 30;

        /// <summary>
        /// 健康检查间隔（秒）
        /// </summary>
        public int HealthCheckInterval { get; set; } = 60;

        /// <summary>
        /// 泄漏检测阈值（秒）
        /// </summary>
        public int LeakDetectionThreshold { get; set; } = 300;

        /// <summary>
        /// 是否启用智能调整
        /// </summary>
        public bool EnableSmartAdjustment { get; set; } = true;

        /// <summary>
        /// 负载阈值（百分比）
        /// </summary>
        public int LoadThreshold { get; set; } = 80;

        /// <summary>
        /// 缩容阈值（百分比）
        /// </summary>
        public int ShrinkThreshold { get; set; } = 30;
    }

    /// <summary>
    /// 连接包装器
    /// </summary>
    public class PooledConnection : IDisposable
    {
        private readonly SmartConnectionPool _pool;
        private bool _disposed;

        public DbConnection Connection { get; }
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; private set; } = DateTime.UtcNow;
        public int UseCount { get; private set; }
        public bool IsInUse { get; internal set; }
        public string LastUsedBy { get; private set; }

        internal PooledConnection(DbConnection connection, SmartConnectionPool pool)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        internal void MarkUsed(string caller = null)
        {
            LastUsedAt = DateTime.UtcNow;
            UseCount++;
            IsInUse = true;
            LastUsedBy = caller;
        }

        internal void MarkReturned()
        {
            IsInUse = false;
            LastUsedBy = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _pool.ReturnConnection(this);
            }
        }
    }

    /// <summary>
    /// 连接池指标
    /// </summary>
    public class ConnectionPoolMetrics
    {
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public int WaitingRequests { get; set; }
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public long LeakedConnections { get; set; }
        public double AverageWaitTimeMs { get; set; }
        public double AverageUseTimeMs { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// 智能连接池管理器
    /// </summary>
    public class SmartConnectionPool : IDisposable
    {
        private readonly string _name;
        private readonly ConnectionPoolConfig _config;
        private readonly Func<DbConnection> _connectionFactory;
        private readonly ConcurrentBag<PooledConnection> _availableConnections;
        private readonly ConcurrentDictionary<Guid, PooledConnection> _inUseConnections;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _smartAdjustmentTimer;
        private readonly object _lock = new object();
        private bool _disposed;
        private bool _sqlLog;
        private Action<string> _logCallback;

        /// <summary>
        /// 启用连接池日志输出
        /// </summary>
        public void EnableSqlLog(Action<string> logCallback = null)
        {
            _sqlLog = true;
            if (logCallback != null)
                _logCallback = logCallback;
        }

        /// <summary>
        /// 日志输出（受_sqlLog控制，支持自定义回调）
        /// </summary>
        private void Log(string message)
        {
            if (!_sqlLog)
                return;

            _logCallback?.Invoke($"连接池 {_name}: {message}")
                ?? Console.WriteLine($"连接池 {_name}: {message}");
        }

        // 指标
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _leakedConnections;
        private long _totalWaitTimeMs;
        private DateTime _startTime;

        /// <summary>
        /// 连接池名称
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 连接池配置
        /// </summary>
        public ConnectionPoolConfig Config => _config;

        /// <summary>
        /// 当前指标
        /// </summary>
        public ConnectionPoolMetrics Metrics => GetMetrics();

        public SmartConnectionPool(string name, Func<DbConnection> connectionFactory, ConnectionPoolConfig config = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _config = config ?? new ConnectionPoolConfig();

            _availableConnections = new ConcurrentBag<PooledConnection>();
            _inUseConnections = new ConcurrentDictionary<Guid, PooledConnection>();
            _semaphore = new SemaphoreSlim(_config.MaxPoolSize, _config.MaxPoolSize);
            _startTime = DateTime.UtcNow;

            // 预创建最小连接数
            for (int i = 0; i < _config.MinPoolSize; i++)
            {
                var conn = CreateNewConnection();
                if (conn != null)
                    _availableConnections.Add(conn);
            }

            // 启动健康检查定时器
            _healthCheckTimer = new Timer(
                HealthCheckCallback,
                null,
                TimeSpan.FromSeconds(_config.HealthCheckInterval),
                TimeSpan.FromSeconds(_config.HealthCheckInterval));

            // 启动智能调整定时器
            if (_config.EnableSmartAdjustment)
            {
                _smartAdjustmentTimer = new Timer(
                    SmartAdjustmentCallback,
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        public async Task<PooledConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _totalRequests);

            try
            {
                // 等待可用连接
                if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(_config.ConnectionTimeout), cancellationToken))
                {
                    Interlocked.Increment(ref _failedRequests);
                    throw new TimeoutException($"连接池 {_name} 获取连接超时({_config.ConnectionTimeout}秒)");
                }

                stopwatch.Stop();
                Interlocked.Add(ref _totalWaitTimeMs, stopwatch.ElapsedMilliseconds);

                // 尝试从空闲连接中获取
                if (_availableConnections.TryTake(out var connection))
                {
                    // 检查连接是否过期
                    if (IsConnectionExpired(connection))
                    {
                        DestroyConnection(connection);
                        connection = CreateNewConnection();
                    }
                    else
                    {
                        // 验证连接有效性
                        if (!await ValidateConnectionAsync(connection))
                        {
                            DestroyConnection(connection);
                            connection = CreateNewConnection();
                        }
                    }
                }
                else
                {
                    // 创建新连接
                    connection = CreateNewConnection();
                }

                if (connection == null)
                {
                    _semaphore.Release();
                    Interlocked.Increment(ref _failedRequests);
                    throw new InvalidOperationException($"连接池 {_name} 无法创建新连接");
                }

                connection.MarkUsed();
                _inUseConnections.TryAdd(connection.Id, connection);
                Interlocked.Increment(ref _successfulRequests);

                return connection;
            }
            catch
            {
                Interlocked.Increment(ref _failedRequests);
                throw;
            }
        }

        /// <summary>
        /// 获取连接（同步版本）
        /// </summary>
        public PooledConnection GetConnection()
        {
            return GetConnectionAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 归还连接
        /// </summary>
        internal void ReturnConnection(PooledConnection connection)
        {
            if (connection == null) return;

            if (_inUseConnections.TryRemove(connection.Id, out var removed))
            {
                connection.MarkReturned();

                // 检查连接是否应该被销毁
                if (IsConnectionExpired(connection) || !ValidateConnection(connection))
                {
                    DestroyConnection(connection);
                }
                else
                {
                    _availableConnections.Add(connection);
                }

                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取指标
        /// </summary>
        public ConnectionPoolMetrics GetMetrics()
        {
            return new ConnectionPoolMetrics
            {
                TotalConnections = _availableConnections.Count + _inUseConnections.Count,
                ActiveConnections = _inUseConnections.Count,
                IdleConnections = _availableConnections.Count,
                WaitingRequests = Math.Max(0, _config.MaxPoolSize - _semaphore.CurrentCount),
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                LeakedConnections = _leakedConnections,
                AverageWaitTimeMs = _successfulRequests > 0 ? (double)_totalWaitTimeMs / _successfulRequests : 0,
                AverageUseTimeMs = 0,
                LastHealthCheck = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime
            };
        }

        /// <summary>
        /// 创建新连接
        /// </summary>
        private PooledConnection CreateNewConnection()
        {
            try
            {
                var connection = _connectionFactory();
                if (connection != null)
                {
                    connection.Open();
                    return new PooledConnection(connection, this);
                }
            }
            catch (Exception ex)
            {
                // 记录日志
                Log($"连接池 {_name} 创建连接失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 销毁连接
        /// </summary>
        private void DestroyConnection(PooledConnection connection)
        {
            try
            {
                connection.Connection?.Close();
                connection.Connection?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"连接池 {_name} 销毁连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查连接是否过期
        /// </summary>
        private bool IsConnectionExpired(PooledConnection connection)
        {
            return DateTime.UtcNow - connection.CreatedAt > TimeSpan.FromMinutes(_config.ConnectionLifetime);
        }

        /// <summary>
        /// 验证连接有效性（同步）
        /// </summary>
        private bool ValidateConnection(PooledConnection connection)
        {
            try
            {
                return connection.Connection?.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证连接有效性（异步）
        /// </summary>
        private async Task<bool> ValidateConnectionAsync(PooledConnection connection)
        {
            try
            {
                if (connection.Connection?.State != ConnectionState.Open)
                    return false;

                // 执行简单查询验证连接
                using var cmd = connection.Connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = 5;
                await cmd.ExecuteScalarAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 健康检查回调
        /// </summary>
        private void HealthCheckCallback(object state)
        {
            try
            {
                var expiredConnections = new List<PooledConnection>();

                // 检查空闲连接
                foreach (var connection in _availableConnections)
                {
                    if (IsConnectionExpired(connection) || !ValidateConnection(connection))
                    {
                        expiredConnections.Add(connection);
                    }
                }

                // 移除过期连接
                foreach (var expired in expiredConnections)
                {
                    if (_availableConnections.TryTake(out var removed))
                    {
                        DestroyConnection(removed);
                    }
                }

                // 检测泄漏连接
                foreach (var kvp in _inUseConnections)
                {
                    var connection = kvp.Value;
                    if (DateTime.UtcNow - connection.LastUsedAt > TimeSpan.FromSeconds(_config.LeakDetectionThreshold))
                    {
                        Interlocked.Increment(ref _leakedConnections);
                        Log($"连接池 {_name} 检测到可能的连接泄漏: {connection.Id}, 最后使用时间: {connection.LastUsedAt}");
                    }
                }

                // 确保最小连接数
                while (_availableConnections.Count + _inUseConnections.Count < _config.MinPoolSize)
                {
                    var newConn = CreateNewConnection();
                    if (newConn != null)
                        _availableConnections.Add(newConn);
                    else
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"连接池 {_name} 健康检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 智能调整回调
        /// </summary>
        private void SmartAdjustmentCallback(object state)
        {
            try
            {
                var metrics = GetMetrics();
                var loadPercentage = (double)metrics.ActiveConnections / _config.MaxPoolSize * 100;

                // 负载过高时扩容
                if (loadPercentage > _config.LoadThreshold)
                {
                    var expandCount = Math.Min(10, _config.MaxPoolSize - metrics.TotalConnections);
                    for (int i = 0; i < expandCount; i++)
                    {
                        var newConn = CreateNewConnection();
                        if (newConn != null)
                            _availableConnections.Add(newConn);
                        else
                            break;
                    }
                    Log($"连接池 {_name} 扩容: 新增 {expandCount} 个连接");
                }
                // 负载过低时缩容
                else if (loadPercentage < _config.ShrinkThreshold && metrics.TotalConnections > _config.MinPoolSize)
                {
                    var shrinkCount = Math.Min(5, metrics.IdleConnections - _config.MinPoolSize);
                    for (int i = 0; i < shrinkCount; i++)
                    {
                        if (_availableConnections.TryTake(out var conn))
                        {
                            DestroyConnection(conn);
                        }
                    }
                    Log($"连接池 {_name} 缩容: 移除 {shrinkCount} 个连接");
                }
            }
            catch (Exception ex)
            {
                Log($"连接池 {_name} 智能调整失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _healthCheckTimer?.Dispose();
                _smartAdjustmentTimer?.Dispose();

                // 销毁所有连接
                while (_availableConnections.TryTake(out var conn))
                {
                    DestroyConnection(conn);
                }

                foreach (var kvp in _inUseConnections)
                {
                    DestroyConnection(kvp.Value);
                }

                _semaphore?.Dispose();
            }
        }
    }
}
