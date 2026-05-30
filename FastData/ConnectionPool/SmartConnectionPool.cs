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
        /// 最小连接数（-1 表示根据环境自动计算）
        /// </summary>
        public int MinPoolSize { get; set; } = -1;

        /// <summary>
        /// 最大连接数（-1 表示根据环境自动计算）
        /// </summary>
        public int MaxPoolSize { get; set; } = -1;

        /// <summary>
        /// 是否根据环境自动调整连接池大小
        /// </summary>
        public bool AutoAdjustByEnvironment { get; set; } = true;

        /// <summary>
        /// 根据系统资源自动计算连接池配置
        /// </summary>
        public void AdjustByEnvironment()
        {
            if (!AutoAdjustByEnvironment)
                return;

            // 获取系统资源信息
            var processorCount = Environment.ProcessorCount;
            var memoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);

            // 根据 CPU 核心数计算池大小
            // 公式：CPU 密集型 = 核心数 + 1，IO 密集型 = 核心数 * 2
            // 数据库属于 IO 密集型，但有连接开销，取核心数 * 1.5
            var cpuBasedMax = (int)(processorCount * 1.5);

            // 根据内存计算池大小（每连接约占用 1-2MB）
            // 保守估计，预留 10% 内存给连接池
            var memoryBasedMax = (int)(memoryMB * 0.1 / 2);

            // 如果 MinPoolSize 未设置，设为 MaxPoolSize 的 10%
            if (MinPoolSize < 0)
            {
                MinPoolSize = Math.Max(2, cpuBasedMax / 10);
            }

            // 如果 MaxPoolSize 未设置，取 CPU 和内存计算的较小值
            if (MaxPoolSize < 0)
            {
                MaxPoolSize = Math.Min(cpuBasedMax, memoryBasedMax);
                // 限制范围：最小 10，最大 200
                MaxPoolSize = Math.Max(10, Math.Min(200, MaxPoolSize));
            }

            // 确保 MinPoolSize 不超过 MaxPoolSize
            MinPoolSize = Math.Min(MinPoolSize, MaxPoolSize);
        }

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

        /// <summary>
        /// 连接创建最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 连接创建重试基础延迟（毫秒）
        /// </summary>
        public int RetryBaseDelayMs { get; set; } = 50;

        /// <summary>
        /// 连接验证命令超时（秒）
        /// </summary>
        public int ValidationCommandTimeout { get; set; } = 5;

        /// <summary>
        /// 智能调整间隔（秒）
        /// </summary>
        public int SmartAdjustmentInterval { get; set; } = 30;

        /// <summary>
        /// 每次最大扩容数量
        /// </summary>
        public int MaxExpandCount { get; set; } = 10;

        /// <summary>
        /// 每次最大缩容数量
        /// </summary>
        public int MaxShrinkCount { get; set; } = 5;

        /// <summary>
        /// 熔断器配置
        /// </summary>
        public CircuitBreakerConfig CircuitBreaker { get; set; } = new CircuitBreakerConfig();
    }

    /// <summary>
    /// 熔断器状态
    /// </summary>
    public enum CircuitState
    {
        Closed,      // 正常状态
        Open,        // 熔断状态
        HalfOpen     // 半开状态（测试恢复）
    }

    /// <summary>
    /// 熔断器配置
    /// </summary>
    public class CircuitBreakerConfig
    {
        /// <summary>
        /// 是否启用熔断器
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 连续失败阈值（达到此值触发熔断）
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// 熔断时长（秒，之后进入半开状态）
        /// </summary>
        public int CircuitOpenDurationSec { get; set; } = 30;

        /// <summary>
        /// 半开状态下允许的最大测试请求数
        /// </summary>
        public int HalfOpenMaxRequests { get; set; } = 3;
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
        private readonly object _adjustmentLock = new object();
        private readonly object _circuitLock = new object();
        private bool _disposed;
        private bool _sqlLog;
        private Action<string> _logCallback;

        // 熔断器字段
        private CircuitState _circuitState = CircuitState.Closed;
        private int _consecutiveFailures;
        private DateTime _circuitOpenedAt;
        private int _halfOpenRequests;

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

            if (_logCallback != null)
                _logCallback.Invoke($"连接池 {_name}: {message}");
            else
                Console.WriteLine($"连接池 {_name}: {message}");
        }

        // 指标
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _leakedConnections;
        private long _totalWaitTimeMs;
        private DateTime _startTime;

        // 原子计数器（避免频繁 ConcurrentBag.Count 遍历）
        private int _totalCount;
        private int _activeCount;

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
            
            // 根据环境自动调整连接池大小
            _config.AdjustByEnvironment();
            
            _semaphore = new SemaphoreSlim(_config.MaxPoolSize, _config.MaxPoolSize);
            _startTime = DateTime.UtcNow;

            // 预创建最小连接数并验证有效性（连接预热）
            for (int i = 0; i < _config.MinPoolSize; i++)
            {
                var conn = CreateNewConnection();
                if (conn != null)
                {
                    // 验证连接有效性
                    if (ValidateConnection(conn))
                    {
                        _availableConnections.Add(conn);
                    }
                    else
                    {
                        // 预热失败，销毁并重试
                        Log($"连接池 {_name} 预热连接失败，重试创建");
                        DestroyConnection(conn);
                        var retryConn = CreateNewConnection();
                        if (retryConn != null)
                            _availableConnections.Add(retryConn);
                    }
                }
            }
            _totalCount = _availableConnections.Count;

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
                    TimeSpan.FromSeconds(_config.SmartAdjustmentInterval),
                    TimeSpan.FromSeconds(_config.SmartAdjustmentInterval));
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
                Interlocked.Increment(ref _activeCount);
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
        /// 获取连接（同步版本，避免 GetAwaiter().GetResult() 死锁）
        /// </summary>
        public PooledConnection GetConnection()
        {
            // 熔断器检查
            if (!CanExecuteRequest())
            {
                Interlocked.Increment(ref _failedRequests);
                throw new CircuitBreakerOpenException($"连接池 {_name} 熔断器打开，拒绝请求");
            }

            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _totalRequests);

            try
            {
                // 等待可用连接（同步等待）
                if (!_semaphore.Wait(TimeSpan.FromSeconds(_config.ConnectionTimeout)))
                {
                    Interlocked.Increment(ref _failedRequests);
                    RecordFailure();
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
                        if (!ValidateConnection(connection))
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
                Interlocked.Increment(ref _activeCount);
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
        /// 归还连接
        /// </summary>
        internal void ReturnConnection(PooledConnection connection)
        {
            if (connection == null) return;

            if (_inUseConnections.TryRemove(connection.Id, out var removed))
            {
                connection.MarkReturned();
                Interlocked.Decrement(ref _activeCount);

                // 检查连接是否应该被销毁
                if (IsConnectionExpired(connection) || !ValidateConnection(connection))
                {
                    DestroyConnection(connection);
                    RecordFailure(); // 连接验证失败也算一次失败
                }
                else
                {
                    _availableConnections.Add(connection);
                    RecordSuccess(); // 连接正常归还，记录成功
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
                TotalConnections = _totalCount,
                ActiveConnections = _activeCount,
                IdleConnections = Math.Max(0, _totalCount - _activeCount),
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
        /// 创建新连接（带指数退避重试）
        /// </summary>
        private PooledConnection CreateNewConnection()
        {
            for (int attempt = 0; attempt <= _config.MaxRetries; attempt++)
            {
                try
                {
                    var connection = _connectionFactory();
                    if (connection != null)
                    {
                        connection.Open();
                        Interlocked.Increment(ref _totalCount);
                        return new PooledConnection(connection, this);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == _config.MaxRetries)
                    {
                        Log($"连接池 {_name} 创建连接失败（已重试 {_config.MaxRetries} 次）: {ex.Message}");
                        return null;
                    }

                    var delay = _config.RetryBaseDelayMs * (1 << attempt);
                    Log($"连接池 {_name} 创建连接失败（第 {attempt + 1} 次），{delay}ms 后重试: {ex.Message}");
                    Thread.Sleep(delay);
                }
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
                Interlocked.Decrement(ref _totalCount);
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
        /// 验证连接有效性（同步，执行 SELECT 1 验证数据库可达）
        /// </summary>
        private bool ValidateConnection(PooledConnection connection)
        {
            try
            {
                if (connection.Connection?.State != ConnectionState.Open)
                    return false;

                using var cmd = connection.Connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = _config.ValidationCommandTimeout;
                cmd.ExecuteScalar();
                return true;
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
                cmd.CommandTimeout = _config.ValidationCommandTimeout;
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
                while (_totalCount < _config.MinPoolSize)
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
        /// 智能调整回调（加锁防并发执行）
        /// </summary>
        private void SmartAdjustmentCallback(object state)
        {
            if (!Monitor.TryEnter(_adjustmentLock))
                return; // 已有调整在执行，跳过本次

            try
            {
                var metrics = GetMetrics();
                var loadPercentage = (double)metrics.ActiveConnections / _config.MaxPoolSize * 100;

                // 负载过高时扩容
                if (loadPercentage > _config.LoadThreshold)
                {
                    var expandCount = Math.Min(_config.MaxExpandCount, _config.MaxPoolSize - metrics.TotalConnections);
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
                    var shrinkCount = Math.Min(_config.MaxShrinkCount, metrics.IdleConnections - _config.MinPoolSize);
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
            finally
            {
                Monitor.Exit(_adjustmentLock);
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

        #region Circuit Breaker Methods

        /// <summary>
        /// 检查是否可以执行请求（熔断器检查）
        /// </summary>
        private bool CanExecuteRequest()
        {
            if (!_config.CircuitBreaker.Enabled)
                return true;

            lock (_circuitLock)
            {
                switch (_circuitState)
                {
                    case CircuitState.Closed:
                        return true;

                    case CircuitState.Open:
                        // 检查是否已过熔断时长
                        if (DateTime.UtcNow >= _circuitOpenedAt.AddSeconds(_config.CircuitBreaker.CircuitOpenDurationSec))
                        {
                            TryTransitionToHalfOpen();
                            return true;
                        }
                        return false;

                    case CircuitState.HalfOpen:
                        // 半开状态下只允许少量测试请求
                        return _halfOpenRequests < _config.CircuitBreaker.HalfOpenMaxRequests;

                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// 记录成功
        /// </summary>
        private void RecordSuccess()
        {
            if (!_config.CircuitBreaker.Enabled)
                return;

            lock (_circuitLock)
            {
                if (_circuitState == CircuitState.HalfOpen)
                {
                    // 半开状态下成功，恢复到关闭状态
                    Log($"熔断器 {_name} 恢复正常状态（半开->关闭）");
                    _circuitState = CircuitState.Closed;
                    _consecutiveFailures = 0;
                    _halfOpenRequests = 0;
                }
                else if (_circuitState == CircuitState.Closed)
                {
                    // 关闭状态下重置失败计数
                    _consecutiveFailures = 0;
                }
            }
        }

        /// <summary>
        /// 记录失败
        /// </summary>
        private void RecordFailure()
        {
            if (!_config.CircuitBreaker.Enabled)
                return;

            lock (_circuitLock)
            {
                _consecutiveFailures++;

                if (_circuitState == CircuitState.HalfOpen)
                {
                    // 半开状态下失败，重新打开熔断器
                    Log($"熔断器 {_name} 触发熔断（半开->打开，连续失败 {_consecutiveFailures} 次）");
                    _circuitState = CircuitState.Open;
                    _circuitOpenedAt = DateTime.UtcNow;
                    _halfOpenRequests = 0;
                }
                else if (_circuitState == CircuitState.Closed && 
                         _consecutiveFailures >= _config.CircuitBreaker.FailureThreshold)
                {
                    // 关闭状态下达到失败阈值，打开熔断器
                    Log($"熔断器 {_name} 触发熔断（关闭->打开，连续失败 {_consecutiveFailures} 次）");
                    _circuitState = CircuitState.Open;
                    _circuitOpenedAt = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// 尝试转换到半开状态
        /// </summary>
        private void TryTransitionToHalfOpen()
        {
            Log($"熔断器 {_name} 进入半开状态（打开->半开）");
            _circuitState = CircuitState.HalfOpen;
            _halfOpenRequests = 0;
            _consecutiveFailures = 0;
        }

        #endregion
    }

    /// <summary>
    /// 熔断器打开异常
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
