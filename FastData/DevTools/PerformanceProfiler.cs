using System;
using FastData.Context;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace FastData.DevTools
{
    /// <summary>
    /// 性能分析器
    /// </summary>
    public static class PerformanceProfiler
    {
        private static readonly Dictionary<string, PerformanceMetric> _metrics = new Dictionary<string, PerformanceMetric>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 开始性能分析
        /// </summary>
        public static IDisposable StartProfiling(string operationName)
        {
            return new PerformanceScope(operationName);
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        public static PerformanceMetric GetMetric(string operationName)
        {
            lock (_lock)
            {
                return _metrics.TryGetValue(operationName, out var metric)
                    ? metric.Clone()
                    : new PerformanceMetric { Name = operationName };
            }
        }

        /// <summary>
        /// 获取所有性能指标
        /// </summary>
        public static List<PerformanceMetric> GetAllMetrics()
        {
            lock (_lock)
            {
                return _metrics.Values.Select(m => m.Clone()).ToList();
            }
        }

        /// <summary>
        /// 清除所有性能指标
        /// </summary>
        public static void ClearMetrics()
        {
            lock (_lock)
            {
                _metrics.Clear();
            }
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public static PerformanceReport GenerateReport()
        {
            lock (_lock)
            {
                var metrics = _metrics.Values.ToList();
                return new PerformanceReport
                {
                    GeneratedAt = DateTime.Now,
                    TotalOperations = metrics.Sum(m => m.Count),
                    AverageExecutionTime = metrics.Any() ? metrics.Average(m => m.AverageExecutionTime) : 0,
                    SlowestOperation = metrics.OrderByDescending(m => m.MaxExecutionTime).FirstOrDefault(),
                    FastestOperation = metrics.OrderBy(m => m.MinExecutionTime).FirstOrDefault(),
                    MostFrequentOperation = metrics.OrderByDescending(m => m.Count).FirstOrDefault(),
                    Operations = metrics
                };
            }
        }

        /// <summary>
        /// 分析性能瓶颈
        /// </summary>
        public static List<PerformanceBottleneck> AnalyzeBottlenecks(double thresholdSeconds = 1.0)
        {
            lock (_lock)
            {
                return _metrics.Values
                    .Where(m => m.AverageExecutionTime > thresholdSeconds * 1000)
                    .Select(m => new PerformanceBottleneck
                    {
                        OperationName = m.Name,
                        AverageExecutionTime = m.AverageExecutionTime,
                        MaxExecutionTime = m.MaxExecutionTime,
                        Count = m.Count,
                        Severity = m.AverageExecutionTime > thresholdSeconds * 5000 ? BottleneckSeverity.Critical
                                  : m.AverageExecutionTime > thresholdSeconds * 2000 ? BottleneckSeverity.High
                                  : BottleneckSeverity.Medium
                    })
                    .OrderByDescending(b => b.AverageExecutionTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 比较性能指标
        /// </summary>
        public static PerformanceComparison CompareMetrics(string operationName1, string operationName2)
        {
            var metric1 = GetMetric(operationName1);
            var metric2 = GetMetric(operationName2);

            return new PerformanceComparison
            {
                Operation1Name = operationName1,
                Operation2Name = operationName2,
                Operation1AvgTime = metric1.AverageExecutionTime,
                Operation2AvgTime = metric2.AverageExecutionTime,
                PerformanceRatio = metric2.AverageExecutionTime > 0
                    ? metric1.AverageExecutionTime / metric2.AverageExecutionTime
                    : 0,
                Difference = metric1.AverageExecutionTime - metric2.AverageExecutionTime
            };
        }

        /// <summary>
        /// 记录指标
        /// </summary>
        internal static void RecordMetric(string operationName, long elapsedMs)
        {
            lock (_lock)
            {
                if (!_metrics.TryGetValue(operationName, out var metric))
                {
                    metric = new PerformanceMetric { Name = operationName };
                    _metrics[operationName] = metric;
                }

                metric.Record(elapsedMs);
            }
        }

        /// <summary>
        /// 性能作用域
        /// </summary>
        private class PerformanceScope : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed;

            public PerformanceScope(string operationName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _stopwatch.Stop();
                    RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds);
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 性能指标数据模型
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// 获取或设置操作名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置操作执行次数
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// 获取或设置操作总执行时间（毫秒）
        /// </summary>
        public long TotalExecutionTime { get; set; }

        /// <summary>
        /// 获取操作平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime => Count > 0 ? TotalExecutionTime / (double)Count : 0;

        /// <summary>
        /// 获取或设置操作最小执行时间（毫秒）
        /// </summary>
        public long MinExecutionTime { get; set; } = long.MaxValue;

        /// <summary>
        /// 获取或设置操作最大执行时间（毫秒）
        /// </summary>
        public long MaxExecutionTime { get; set; }

        /// <summary>
        /// 获取或设置最后一次执行的时间戳
        /// </summary>
        public DateTime LastExecutedAt { get; set; }

        /// <summary>
        /// 记录一次操作的执行时间
        /// </summary>
        /// <param name="elapsedMs">操作执行耗时（毫秒）</param>
        internal void Record(long elapsedMs)
        {
            Count++;
            TotalExecutionTime += elapsedMs;
            MinExecutionTime = Math.Min(MinExecutionTime, elapsedMs);
            MaxExecutionTime = Math.Max(MaxExecutionTime, elapsedMs);
            LastExecutedAt = DateTime.Now;
        }

        /// <summary>
        /// 创建当前性能指标的深拷贝
        /// </summary>
        /// <returns>返回一个新的 PerformanceMetric 实例，包含当前指标的所有数据</returns>
        public PerformanceMetric Clone()
        {
            return new PerformanceMetric
            {
                Name = Name,
                Count = Count,
                TotalExecutionTime = TotalExecutionTime,
                MinExecutionTime = MinExecutionTime,
                MaxExecutionTime = MaxExecutionTime,
                LastExecutedAt = LastExecutedAt
            };
        }
    }

    /// <summary>
    /// 性能报告数据模型，汇总所有性能指标的统计信息
    /// </summary>
    public class PerformanceReport
    {
        /// <summary>
        /// 获取或设置报告生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// 获取或设置所有操作的总执行次数
        /// </summary>
        public long TotalOperations { get; set; }

        /// <summary>
        /// 获取或设置所有操作的平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 获取或设置执行最慢的操作的性能指标
        /// </summary>
        public PerformanceMetric SlowestOperation { get; set; }

        /// <summary>
        /// 获取或设置执行最快的操作的性能指标
        /// </summary>
        public PerformanceMetric FastestOperation { get; set; }

        /// <summary>
        /// 获取或设置执行频率最高的操作的性能指标
        /// </summary>
        public PerformanceMetric MostFrequentOperation { get; set; }

        /// <summary>
        /// 获取或设置所有操作的性能指标列表
        /// </summary>
        public List<PerformanceMetric> Operations { get; set; } = new List<PerformanceMetric>();
    }

    /// <summary>
    /// 性能瓶颈数据模型，用于识别和描述性能问题
    /// </summary>
    public class PerformanceBottleneck
    {
        /// <summary>
        /// 获取或设置存在性能瓶颈的操作名称
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// 获取或设置该操作的平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 获取或设置该操作的最大执行时间（毫秒）
        /// </summary>
        public double MaxExecutionTime { get; set; }

        /// <summary>
        /// 获取或设置该操作的执行次数
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// 获取或设置瓶颈严重程度
        /// </summary>
        public BottleneckSeverity Severity { get; set; }
    }

    /// <summary>
    /// 性能瓶颈严重程度等级
    /// </summary>
    public enum BottleneckSeverity
    {
        /// <summary>
        /// 低严重程度，性能影响较小，可延后优化
        /// </summary>
        Low,

        /// <summary>
        /// 中等严重程度，建议在近期迭代中优化
        /// </summary>
        Medium,

        /// <summary>
        /// 高严重程度，应尽快进行性能优化
        /// </summary>
        High,

        /// <summary>
        /// 临界严重程度，需要立即处理以避免系统性能恶化
        /// </summary>
        Critical
    }

    /// <summary>
    /// 性能比较数据模型，用于对比两个操作的性能差异
    /// </summary>
    public class PerformanceComparison
    {
        /// <summary>
        /// 获取或设置第一个操作的名称
        /// </summary>
        public string Operation1Name { get; set; }

        /// <summary>
        /// 获取或设置第二个操作的名称
        /// </summary>
        public string Operation2Name { get; set; }

        /// <summary>
        /// 获取或设置第一个操作的平均执行时间（毫秒）
        /// </summary>
        public double Operation1AvgTime { get; set; }

        /// <summary>
        /// 获取或设置第二个操作的平均执行时间（毫秒）
        /// </summary>
        public double Operation2AvgTime { get; set; }

        /// <summary>
        /// 获取或设置两个操作的性能比率（操作1平均时间 / 操作2平均时间）
        /// </summary>
        public double PerformanceRatio { get; set; }

        /// <summary>
        /// 获取或设置两个操作的平均执行时间差值（毫秒）
        /// </summary>
        public double Difference { get; set; }

        /// <summary>
        /// 获取性能比较结果的文本摘要
        /// </summary>
        /// <returns>返回描述两个操作性能差异的中文文本</returns>
        public string GetSummary()
        {
            if (PerformanceRatio > 1)
            {
                return string.Format("{0} 比 {1} 慢 {2:F2}x ({3:F2} ms)", Operation1Name, Operation2Name, PerformanceRatio, Difference);
            }
            else if (PerformanceRatio < 1)
            {
                return string.Format("{0} 比 {1} 快 {2:F2}x ({3:F2} ms)", Operation1Name, Operation2Name, (1 / PerformanceRatio), Math.Abs(Difference));
            }
            else
            {
                return string.Format("{0} 和 {1} 性能相同", Operation1Name, Operation2Name);
            }
        }
    }

    /// <summary>
    /// 数据库索引类型枚举
    /// </summary>
    public enum IndexType
    {
        /// <summary>
        /// B-Tree 索引，适用于范围查询和排序场景
        /// </summary>
        BTree,

        /// <summary>
        /// 哈希索引，适用于精确等值查询场景
        /// </summary>
        Hash,

        /// <summary>
        /// 全文索引，适用于文本内容检索场景
        /// </summary>
        FullText,

        /// <summary>
        /// 空间索引，适用于地理空间数据查询场景
        /// </summary>
        Spatial
    }
}