using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastData.Monitoring
{
    /// <summary>
    /// 性能监控器 - 跟踪和记录数据库操作性能
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationMetrics> _metrics = new ConcurrentDictionary<string, OperationMetrics>();
        private readonly int _maxMetricsHistory;

        public PerformanceMonitor(int maxMetricsHistory = 1000)
        {
            _maxMetricsHistory = maxMetricsHistory;
        }

        /// <summary>
        /// 最近 SQL 记录最大保留条数
        /// </summary>
        private const int MaxRecentSqlCount = 100;

        /// <summary>
        /// 记录操作执行
        /// </summary>
        public void RecordOperation(string operationType, string sql, TimeSpan duration, bool success)
        {
            var key = operationType;
            var metrics = _metrics.GetOrAdd(key, _ => new OperationMetrics { OperationType = operationType });

            // 使用 Interlocked 无锁更新计数器（热路径，高频调用）
            Interlocked.Increment(ref metrics.TotalOperations);
            if (success)
                Interlocked.Increment(ref metrics.SuccessfulOperations);
            else
                Interlocked.Increment(ref metrics.FailedOperations);

            // 仅对持续时间统计加锁（低频更新区域）
            lock (metrics.DurationLock)
            {
                metrics.TotalDuration += duration.TotalMilliseconds;

                if (duration.TotalMilliseconds > metrics.MaxDuration)
                    metrics.MaxDuration = duration.TotalMilliseconds;

                if (duration.TotalMilliseconds < metrics.MinDuration || metrics.MinDuration == 0)
                    metrics.MinDuration = duration.TotalMilliseconds;

                metrics.LastExecutionTime = DateTime.UtcNow;
                metrics.AverageDuration = metrics.TotalDuration / metrics.TotalOperations;
            }

            // 使用 ConcurrentQueue 无锁记录最近 SQL
            metrics.RecentSqls.Enqueue(new SqlExecution
            {
                Sql = sql,
                Duration = duration,
                Success = success,
                Timestamp = DateTime.UtcNow
            });

            // 超出限制时通过 TryDequeue 循环裁剪，无需加锁
            while (metrics.RecentSqls.Count > MaxRecentSqlCount)
                metrics.RecentSqls.TryDequeue(out _);
        }

        /// <summary>
        /// 获取操作指标
        /// </summary>
        public OperationMetrics GetMetrics(string operationType)
        {
            return _metrics.TryGetValue(operationType, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// 获取所有指标
        /// </summary>
        public IEnumerable<OperationMetrics> GetAllMetrics()
        {
            return _metrics.Values.ToList();
        }

        /// <summary>
        /// 获取性能统计摘要
        /// </summary>
        public PerformanceSummary GetSummary()
        {
            var allMetrics = GetAllMetrics().ToList();

            return new PerformanceSummary
            {
                TotalOperations = allMetrics.Sum(m => m.TotalOperations),
                TotalSuccessfulOperations = allMetrics.Sum(m => m.SuccessfulOperations),
                TotalFailedOperations = allMetrics.Sum(m => m.FailedOperations),
                AverageDuration = allMetrics.Any() ? allMetrics.Average(m => m.AverageDuration) : 0,
                MaxDuration = allMetrics.Any() ? allMetrics.Max(m => m.MaxDuration) : 0,
                MinDuration = allMetrics.Any() ? allMetrics.Min(m => m.MinDuration) : 0,
                OperationTypes = allMetrics.Select(m => m.OperationType).ToList(),
                LastExecutionTime = allMetrics.Any() ? allMetrics.Max(m => m.LastExecutionTime) : (DateTime?)null
            };
        }

        /// <summary>
        /// 清除所有指标
        /// </summary>
        public void Clear()
        {
            _metrics.Clear();
        }

        /// <summary>
        /// 获取慢查询列表（执行时间超过阈值）
        /// </summary>
        public List<SlowQuery> GetSlowQueries(double thresholdMs = 1000)
        {
            var slowQueries = new List<SlowQuery>();

            foreach (var metrics in _metrics.Values)
            {
                // ConcurrentQueue 支持线程安全枚举，无需加锁
                var sqlExecutions = metrics.RecentSqls
                    .Where(s => s.Duration.TotalMilliseconds > thresholdMs)
                    .ToList();

                foreach (var sql in sqlExecutions)
                {
                    slowQueries.Add(new SlowQuery
                    {
                        OperationType = metrics.OperationType,
                        Sql = sql.Sql,
                        Duration = sql.Duration,
                        Timestamp = sql.Timestamp
                    });
                }
            }

            return slowQueries.OrderBy(q => q.Timestamp).ToList();
        }
    }

    /// <summary>
    /// 操作指标
    /// </summary>
    public class OperationMetrics
    {
        /// <summary>
        /// 持续时间统计专用锁（仅用于 min/max/average/total 等低频更新）
        /// </summary>
        internal object DurationLock { get; } = new object();

        public string OperationType { get; set; }

        /// <summary>
        /// 总操作数（使用 Interlocked 无锁更新）
        /// </summary>
        public long TotalOperations;

        /// <summary>
        /// 成功操作数（使用 Interlocked 无锁更新）
        /// </summary>
        public long SuccessfulOperations;

        /// <summary>
        /// 失败操作数（使用 Interlocked 无锁更新）
        /// </summary>
        public long FailedOperations;

        public double TotalDuration { get; set; }
        public double AverageDuration { get; set; }
        public double MaxDuration { get; set; }
        public double MinDuration { get; set; }
        public DateTime LastExecutionTime { get; set; }

        /// <summary>
        /// 最近执行的 SQL 记录（使用 ConcurrentQueue 支持无锁并发读写）
        /// </summary>
        public ConcurrentQueue<SqlExecution> RecentSqls { get; set; } = new ConcurrentQueue<SqlExecution>();

        public double SuccessRate => TotalOperations > 0
            ? (double)SuccessfulOperations / TotalOperations * 100
            : 0;

        public double FailureRate => TotalOperations > 0
            ? (double)FailedOperations / TotalOperations * 100
            : 0;
    }

    /// <summary>
    /// SQL 执行记录
    /// </summary>
    public class SqlExecution
    {
        public string Sql { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 慢查询
    /// </summary>
    public class SlowQuery
    {
        public string OperationType { get; set; }
        public string Sql { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 性能统计摘要
    /// </summary>
    public class PerformanceSummary
    {
        public long TotalOperations { get; set; }
        public long TotalSuccessfulOperations { get; set; }
        public long TotalFailedOperations { get; set; }
        public double AverageDuration { get; set; }
        public double MaxDuration { get; set; }
        public double MinDuration { get; set; }
        public List<string> OperationTypes { get; set; } = new List<string>();
        public DateTime? LastExecutionTime { get; set; }

        public double SuccessRate => TotalOperations > 0 
            ? (double)TotalSuccessfulOperations / TotalOperations * 100 
            : 0;

        public double FailureRate => TotalOperations > 0 
            ? (double)TotalFailedOperations / TotalOperations * 100 
            : 0;
    }

    /// <summary>
    /// 性能监控辅助类
    /// </summary>
    public static class PerformanceHelper
    {
        private static PerformanceMonitor _monitor = new PerformanceMonitor();

        /// <summary>
        /// 设置监控器
        /// </summary>
        public static void SetMonitor(PerformanceMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        /// <summary>
        /// 获取当前监控器
        /// </summary>
        public static PerformanceMonitor GetMonitor()
        {
            return _monitor;
        }

        /// <summary>
        /// 监控操作执行
        /// </summary>
        public static T MonitorOperation<T>(string operationType, string sql, Func<T> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = action();
                _monitor.RecordOperation(operationType, sql, stopwatch.Elapsed, success: true);
                return result;
            }
            catch
            {
                _monitor.RecordOperation(operationType, sql, stopwatch.Elapsed, success: false);
                throw;
            }
        }

        /// <summary>
        /// 异步监控操作执行
        /// </summary>
        public static async Task<T> MonitorOperationAsync<T>(string operationType, string sql, Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await action();
                _monitor.RecordOperation(operationType, sql, stopwatch.Elapsed, success: true);
                return result;
            }
            catch
            {
                _monitor.RecordOperation(operationType, sql, stopwatch.Elapsed, success: false);
                throw;
            }
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public static string GetPerformanceReport()
        {
            var summary = _monitor.GetSummary();
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== FastData 性能监控报告 ===");
            report.AppendLine(string.Format("生成时间: {0:yyyy-MM-dd HH:mm:ss} UTC", DateTime.UtcNow));
            report.AppendLine();
            report.AppendLine("总体统计:");
            report.AppendLine(string.Format("  总操作数: {0:N0}", summary.TotalOperations));
            report.AppendLine(string.Format("  成功操作: {0:N0}", summary.TotalSuccessfulOperations));
            report.AppendLine(string.Format("  失败操作: {0:N0}", summary.TotalFailedOperations));
            report.AppendLine(string.Format("  成功率: {0:F2}%", summary.SuccessRate));
            report.AppendLine(string.Format("  平均耗时: {0:F2} ms", summary.AverageDuration));
            report.AppendLine(string.Format("  最大耗时: {0:F2} ms", summary.MaxDuration));
            report.AppendLine(string.Format("  最小耗时: {0:F2} ms", summary.MinDuration));
            report.AppendLine(string.Format("  最后执行: {0:yyyy-MM-dd HH:mm:ss}", summary.LastExecutionTime));

            report.AppendLine();
            report.AppendLine("操作类型统计:");
            foreach (var metrics in _monitor.GetAllMetrics())
            {
                report.AppendLine(string.Format("  {0}:", metrics.OperationType));
                report.AppendLine(string.Format("    总数: {0:N0}", metrics.TotalOperations));
                report.AppendLine(string.Format("    成功率: {0:F2}%", metrics.SuccessRate));
                report.AppendLine(string.Format("    平均耗时: {0:F2} ms", metrics.AverageDuration));
            }

            var slowQueries = _monitor.GetSlowQueries(1000);
            if (slowQueries.Any())
            {
                report.AppendLine();
                report.AppendLine(string.Format("慢查询列表 (阈值: 1000ms, 共 {0} 个):", slowQueries.Count));
                foreach (var query in slowQueries.Take(10))
                {
                    report.AppendLine(string.Format("  {0:HH:mm:ss} - {1} - {2:F2}ms", query.Timestamp, query.OperationType, query.Duration.TotalMilliseconds));
                    report.AppendLine(string.Format("    SQL: {0}...", query.Sql.Substring(0, Math.Min(query.Sql.Length, 100))));
                }
            }

            return report.ToString();
        }
    }
}