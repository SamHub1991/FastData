using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastData.ConnectionPool
{
    /// <summary>
    /// 连接池监控器
    /// </summary>
    public class ConnectionPoolMonitor : IDisposable
    {
        private readonly ConnectionPoolFactory _factory;
        private readonly Timer _monitorTimer;
        private readonly List<ConnectionPoolSnapshot> _snapshots;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// 快照历史
        /// </summary>
        public IReadOnlyList<ConnectionPoolSnapshot> Snapshots
        {
            get
            {
                lock (_lock)
                {
                    return _snapshots.AsReadOnly();
                }
            }
        }

        public ConnectionPoolMonitor(ConnectionPoolFactory factory, int monitorIntervalSeconds = 60)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _snapshots = new List<ConnectionPoolSnapshot>();
            _monitorTimer = new Timer(
                MonitorCallback,
                null,
                TimeSpan.FromSeconds(monitorIntervalSeconds),
                TimeSpan.FromSeconds(monitorIntervalSeconds));
        }

        /// <summary>
        /// 获取当前快照
        /// </summary>
        public ConnectionPoolSnapshot TakeSnapshot()
        {
            var snapshot = new ConnectionPoolSnapshot
            {
                Timestamp = DateTime.UtcNow,
                PoolMetrics = _factory.GetAllMetrics()
            };

            lock (_lock)
            {
                _snapshots.Add(snapshot);

                // 保留最近 1000 条记录
                if (_snapshots.Count > 1000)
                {
                    _snapshots.RemoveAt(0);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public ConnectionPoolStatistics GetStatistics(TimeSpan period)
        {
            var cutoff = DateTime.UtcNow - period;
            var relevantSnapshots = new List<ConnectionPoolSnapshot>();

            lock (_lock)
            {
                foreach (var snapshot in _snapshots)
                {
                    if (snapshot.Timestamp >= cutoff)
                    {
                        relevantSnapshots.Add(snapshot);
                    }
                }
            }

            if (relevantSnapshots.Count == 0)
            {
                return new ConnectionPoolStatistics();
            }

            var stats = new ConnectionPoolStatistics
            {
                Period = period,
                SampleCount = relevantSnapshots.Count
            };

            // 计算统计信息
            long totalActive = 0;
            long totalIdle = 0;
            long maxActive = 0;
            long minActive = long.MaxValue;

            foreach (var snapshot in relevantSnapshots)
            {
                foreach (var poolMetrics in snapshot.PoolMetrics.Values)
                {
                    totalActive += poolMetrics.ActiveConnections;
                    totalIdle += poolMetrics.IdleConnections;
                    maxActive = Math.Max(maxActive, poolMetrics.ActiveConnections);
                    minActive = Math.Min(minActive, poolMetrics.ActiveConnections);
                }
            }

            stats.AverageActiveConnections = relevantSnapshots.Count > 0 
                ? (double)totalActive / relevantSnapshots.Count 
                : 0;
            stats.AverageIdleConnections = relevantSnapshots.Count > 0 
                ? (double)totalIdle / relevantSnapshots.Count 
                : 0;
            stats.MaxActiveConnections = maxActive;
            stats.MinActiveConnections = minActive == long.MaxValue ? 0 : minActive;

            return stats;
        }

        /// <summary>
        /// 监控回调
        /// </summary>
        private void MonitorCallback(object state)
        {
            try
            {
                TakeSnapshot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接池监控失败: {ex.Message}");
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
                _monitorTimer?.Dispose();
            }
        }
    }

    /// <summary>
    /// 连接池快照
    /// </summary>
    public class ConnectionPoolSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ConnectionPoolMetrics> PoolMetrics { get; set; } = new Dictionary<string, ConnectionPoolMetrics>();
    }

    /// <summary>
    /// 连接池统计信息
    /// </summary>
    public class ConnectionPoolStatistics
    {
        public TimeSpan Period { get; set; }
        public int SampleCount { get; set; }
        public double AverageActiveConnections { get; set; }
        public double AverageIdleConnections { get; set; }
        public long MaxActiveConnections { get; set; }
        public long MinActiveConnections { get; set; }
    }
}
