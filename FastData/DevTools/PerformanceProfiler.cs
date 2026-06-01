using System;
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
    /// 性能指标
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public long TotalExecutionTime { get; set; }
        public double AverageExecutionTime => Count > 0 ? TotalExecutionTime / (double)Count : 0;
        public long MinExecutionTime { get; set; } = long.MaxValue;
        public long MaxExecutionTime { get; set; }
        public DateTime LastExecutedAt { get; set; }

        internal void Record(long elapsedMs)
        {
            Count++;
            TotalExecutionTime += elapsedMs;
            MinExecutionTime = Math.Min(MinExecutionTime, elapsedMs);
            MaxExecutionTime = Math.Max(MaxExecutionTime, elapsedMs);
            LastExecutedAt = DateTime.Now;
        }

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
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public long TotalOperations { get; set; }
        public double AverageExecutionTime { get; set; }
        public PerformanceMetric SlowestOperation { get; set; }
        public PerformanceMetric FastestOperation { get; set; }
        public PerformanceMetric MostFrequentOperation { get; set; }
        public List<PerformanceMetric> Operations { get; set; } = new List<PerformanceMetric>();
    }

    /// <summary>
    /// 性能瓶颈
    /// </summary>
    public class PerformanceBottleneck
    {
        public string OperationName { get; set; }
        public double AverageExecutionTime { get; set; }
        public double MaxExecutionTime { get; set; }
        public long Count { get; set; }
        public BottleneckSeverity Severity { get; set; }
    }

    /// <summary>
    /// 瓶颈严重程度
    /// </summary>
    public enum BottleneckSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 性能比较
    /// </summary>
    public class PerformanceComparison
    {
        public string Operation1Name { get; set; }
        public string Operation2Name { get; set; }
        public double Operation1AvgTime { get; set; }
        public double Operation2AvgTime { get; set; }
        public double PerformanceRatio { get; set; }
        public double Difference { get; set; }

        public string GetSummary()
        {
            if (PerformanceRatio > 1)
            {
                return $"{Operation1Name} 比 {Operation2Name} 慢 {PerformanceRatio:F2}x ({Difference:F2} ms)";
            }
            else if (PerformanceRatio < 1)
            {
                return $"{Operation1Name} 比 {Operation2Name} 快 {(1 / PerformanceRatio):F2}x ({Math.Abs(Difference):F2} ms)";
            }
            else
            {
                return $"{Operation1Name} 和 {Operation2Name} 性能相同";
            }
        }
    }

    /// <summary>
    /// 查询优化建议
    /// </summary>
    public static class QueryOptimizer
    {
        /// <summary>
        /// 分析查询并生成优化建议
        /// </summary>
        public static List<OptimizationSuggestion> AnalyzeQuery(string sql, double executionTimeMs)
        {
            var suggestions = new List<OptimizationSuggestion>();

            // 检查 SELECT *
            if (sql.ToUpper().Contains("SELECT *"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.High,
                    Description = "避免使用 SELECT *，只查询需要的列",
                    EstimatedImprovement = "20-30%"
                });
            }

            // 检查 WHERE 子句
            if (!sql.ToUpper().Contains("WHERE"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.High,
                    Description = "查询缺少 WHERE 子句，可能返回大量数据",
                    EstimatedImprovement = "50-90%"
                });
            }

            // 检查 LIKE 查询
            if (sql.ToUpper().Contains("LIKE '%"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Medium,
                    Description = "前缀通配符 LIKE 查询无法使用索引，考虑使用全文搜索",
                    EstimatedImprovement = "10-50%"
                });
            }

            // 检查执行时间
            if (executionTimeMs > 1000)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Critical,
                    Description = $"查询执行时间过长 ({executionTimeMs:F2} ms)，需要优化",
                    EstimatedImprovement = "50-80%"
                });
            }

            // 检查 JOIN
            var joinCount = sql.ToUpper().Split(new[] { "JOIN" }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
            if (joinCount > 3)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Medium,
                    Description = $"查询包含 {joinCount} 个 JOIN，考虑简化查询或使用索引",
                    EstimatedImprovement = "20-40%"
                });
            }

            // 检查子查询
            if (sql.ToUpper().Contains("(SELECT"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Medium,
                    Description = "包含子查询，考虑使用 JOIN 或 EXISTS 代替",
                    EstimatedImprovement = "15-35%"
                });
            }

            return suggestions;
        }

        /// <summary>
        /// 生成索引建议
        /// </summary>
        public static List<IndexSuggestion> SuggestIndexes(string tableName, List<string> frequentQueries)
        {
            var suggestions = new List<IndexSuggestion>();

            foreach (var query in frequentQueries)
            {
                // 分析 WHERE 子句中的列
                var whereMatch = System.Text.RegularExpressions.Regex.Match(query, @"WHERE\s+(.+?)(?:\s+GROUP BY|\s+ORDER BY|\s+LIMIT|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (whereMatch.Success)
                {
                    var conditions = whereMatch.Groups[1].Value.Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var condition in conditions)
                    {
                        var columnMatch = System.Text.RegularExpressions.Regex.Match(condition, @"(\w+)\s*[=<>!]+");
                        if (columnMatch.Success)
                        {
                            var column = columnMatch.Groups[1].Value;
                            suggestions.Add(new IndexSuggestion
                            {
                                TableName = tableName,
                                ColumnName = column,
                                Type = IndexType.BTree,
                                Reason = "频繁在 WHERE 子句中使用"
                            });
                        }
                    }
                }

                // 分析 ORDER BY 子句中的列
                var orderByMatch = System.Text.RegularExpressions.Regex.Match(query, @"ORDER BY\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (orderByMatch.Success)
                {
                    var column = orderByMatch.Groups[1].Value;
                    suggestions.Add(new IndexSuggestion
                    {
                        TableName = tableName,
                        ColumnName = column,
                        Type = IndexType.BTree,
                        Reason = "频繁在 ORDER BY 子句中使用"
                    });
                }
            }

            return suggestions.Distinct().ToList();
        }
    }

    /// <summary>
    /// 优化建议
    /// </summary>
    public class OptimizationSuggestion
    {
        public SuggestionType Type { get; set; }
        public SuggestionPriority Priority { get; set; }
        public string Description { get; set; }
        public string EstimatedImprovement { get; set; }
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum SuggestionType
    {
        Performance,
        Security,
        Maintainability
    }

    /// <summary>
    /// 建议优先级
    /// </summary>
    public enum SuggestionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 索引建议
    /// </summary>
    public class IndexSuggestion
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public IndexType Type { get; set; }
        public string Reason { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is IndexSuggestion other)
            {
                return TableName == other.TableName && ColumnName == other.ColumnName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableName, ColumnName);
        }
    }

    /// <summary>
    /// 索引类型
    /// </summary>
    public enum IndexType
    {
        BTree,
        Hash,
        FullText,
        Spatial
    }
}