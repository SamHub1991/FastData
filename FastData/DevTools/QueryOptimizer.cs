using System;
using FastData.Context;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastData.DevTools
{
    /// <summary>
    /// 查询优化器
    /// </summary>
    public static class QueryOptimizer
    {
        /// <summary>
        /// 优化 SQL 查询
        /// </summary>
        public static QueryOptimizationResult Optimize(string sql, OptimizationOptions options = null)
        {
            options = options ?? OptimizationOptions.Default;
            var result = new QueryOptimizationResult
            {
                OriginalSql = sql,
                OptimizedSql = sql,
                Optimizations = new List<OptimizationSuggestion>(),
                EstimatedImprovement = 0
            };

            // 检查并优化 SELECT *
            if (sql.ToUpper().Contains("SELECT *"))
            {
                result.OptimizedSql = ReplaceSelectStar(result.OptimizedSql);
                result.Optimizations.Add(new OptimizationSuggestion
                {
                    Type = OptimizationType.ColumnSelection,
                    Priority = OptimizationPriority.High,
                    Description = "SELECT * 替换为具体列名，减少数据传输",
                    EstimatedImprovement = "20-30%"
                });
            }

            // 检查 WHERE 子句
            if (!sql.ToUpper().Contains("WHERE"))
            {
                result.Optimizations.Add(new OptimizationSuggestion
                {
                    Type = OptimizationType.Filtering,
                    Priority = OptimizationPriority.Critical,
                    Description = "查询缺少 WHERE 子句，可能返回大量数据",
                    EstimatedImprovement = "50-90%"
                });
            }

            // 检查 LIKE 查询
            var likePattern = new Regex(@"LIKE\s+['""]%[\w%]+['""]", RegexOptions.IgnoreCase);
            if (likePattern.IsMatch(sql))
            {
                result.Optimizations.Add(new OptimizationSuggestion
                {
                    Type = OptimizationType.PartialMatch,
                    Priority = OptimizationPriority.Medium,
                    Description = "前缀通配符 LIKE 查询无法使用索引，考虑使用全文搜索",
                    EstimatedImprovement = "10-50%"
                });
            }

            // 检查子查询
            if (sql.Contains("(") && sql.Contains("SELECT"))
            {
                var subqueryPattern = new Regex(@"\(\s*SELECT", RegexOptions.IgnoreCase);
                if (subqueryPattern.IsMatch(sql))
                {
                    result.OptimizedSql = ReplaceSubqueries(result.OptimizedSql);
                    result.Optimizations.Add(new OptimizationSuggestion
                    {
                        Type = OptimizationType.Subquery,
                        Priority = OptimizationPriority.Medium,
                        Description = "子查询替换为 JOIN，提升性能",
                        EstimatedImprovement = "15-35%"
                    });
                }
            }

            // 检查 DISTINCT
            if (sql.ToUpper().Contains("DISTINCT"))
            {
                result.Optimizations.Add(new OptimizationSuggestion
                {
                    Type = OptimizationType.Deduplication,
                    Priority = OptimizationPriority.Low,
                    Description = "DISTINCT 操作消耗资源，考虑使用 GROUP BY 替代",
                    EstimatedImprovement = "5-15%"
                });
            }

            // 检查 ORDER BY
            var orderByMatch = Regex.Match(sql, @"ORDER BY\s+.+?(\s+DESC|\s+ASC|\s*$)", RegexOptions.IgnoreCase);
            if (orderByMatch.Success)
            {
                var orderByClause = orderByMatch.Value;
                if (orderByClause.Split(',').Length > 3)
                {
                    result.Optimizations.Add(new OptimizationSuggestion
                    {
                        Type = OptimizationType.Sorting,
                        Priority = OptimizationPriority.Low,
                        Description = "多个 ORDER BY 字段，考虑使用索引覆盖",
                        EstimatedImprovement = "5-10%"
                    });
                }
            }

            // 检查 JOIN 数量
            var joinCount = Regex.Matches(sql, @"\b(JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN)\b", RegexOptions.IgnoreCase).Count;
            if (joinCount > 5)
            {
                result.Optimizations.Add(new OptimizationSuggestion
                {
                    Type = OptimizationType.Join,
                    Priority = OptimizationPriority.Medium,
                    Description = $"包含 {joinCount} 个 JOIN，考虑查询分解",
                    EstimatedImprovement = "20-40%"
                });
            }

            // 计算总体预估改进
            result.EstimatedImprovement = CalculateEstimatedImprovement(result.Optimizations);

            return result;
        }

        /// <summary>
        /// 分析查询性能
        /// </summary>
        public static QueryPerformanceAnalysis AnalyzePerformance(string sql, double executionTimeMs, int rowsReturned = 0)
        {
            var analysis = new QueryPerformanceAnalysis
            {
                Sql = sql,
                ExecutionTimeMs = executionTimeMs,
                RowsReturned = rowsReturned
            };

            // 评估性能等级
            if (executionTimeMs < 100)
            {
                analysis.PerformanceLevel = PerformanceLevel.Excellent;
                analysis.Rating = "优秀";
            }
            else if (executionTimeMs < 500)
            {
                analysis.PerformanceLevel = PerformanceLevel.Good;
                analysis.Rating = "良好";
            }
            else if (executionTimeMs < 1000)
            {
                analysis.PerformanceLevel = PerformanceLevel.Fair;
                analysis.Rating = "一般";
            }
            else if (executionTimeMs < 5000)
            {
                analysis.PerformanceLevel = PerformanceLevel.Poor;
                analysis.Rating = "较差";
            }
            else
            {
                analysis.PerformanceLevel = PerformanceLevel.Critical;
                analysis.Rating = "严重";
            }

            // 生成建议
            analysis.Recommendations = GeneratePerformanceRecommendations(sql, executionTimeMs, rowsReturned);

            return analysis;
        }

        /// <summary>
        /// 生成索引建议
        /// </summary>
        public static List<IndexSuggestion> SuggestIndexes(string sql, string tableName = null)
        {
            var suggestions = new List<IndexSuggestion>();

            // 从 SQL 中提取表名
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = ExtractTableName(sql);
            }

            // 分析 WHERE 子句
            var whereMatch = Regex.Match(sql, @"WHERE\s+(.+?)(?:\s+GROUP BY|\s+ORDER BY|\s+LIMIT|$)", RegexOptions.IgnoreCase);
            if (whereMatch.Success)
            {
                var conditions = whereMatch.Groups[1].Value.Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var condition in conditions)
                {
                    var columnMatch = Regex.Match(condition, @"(\w+)\s*[=<>!]+");
                    if (columnMatch.Success)
                    {
                        var column = columnMatch.Groups[1].Value;
                        if (!IsFunction(column))
                        {
                            suggestions.Add(new IndexSuggestion
                            {
                                TableName = tableName,
                                ColumnName = column,
                                Type = IndexType.BTree,
                                Priority = IndexPriority.High,
                                Reason = "WHERE 子句中频繁使用"
                            });
                        }
                    }
                }
            }

            // 分析 JOIN 条件
            var joinMatches = Regex.Matches(sql, @"\b(JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN)\s+(\w+)\s+ON\s+(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in joinMatches)
            {
                var column1 = match.Groups[4].Value;
                var column2 = match.Groups[6].Value;

                suggestions.Add(new IndexSuggestion
                {
                    TableName = tableName,
                    ColumnName = column1,
                    Type = IndexType.BTree,
                    Priority = IndexPriority.High,
                    Reason = "JOIN 条件中使用"
                });
            }

            // 分析 ORDER BY
            var orderByMatch = Regex.Match(sql, @"ORDER BY\s+(\w+)", RegexOptions.IgnoreCase);
            if (orderByMatch.Success)
            {
                var column = orderByMatch.Groups[1].Value;
                if (!IsFunction(column))
                {
                    suggestions.Add(new IndexSuggestion
                    {
                        TableName = tableName,
                        ColumnName = column,
                        Type = IndexType.BTree,
                        Priority = IndexPriority.Medium,
                        Reason = "ORDER BY 子句中使用"
                    });
                }
            }

            return suggestions.Distinct().ToList();
        }

        /// <summary>
        /// 检测查询中的常见问题
        /// </summary>
        public static List<QueryIssue> DetectIssues(string sql)
        {
            var issues = new List<QueryIssue>();

            // 检查 N+1 查询问题
            var inClauseCount = Regex.Matches(sql, @"IN\s*\(", RegexOptions.IgnoreCase).Count;
            if (inClauseCount > 3)
            {
                issues.Add(new QueryIssue
                {
                    Type = IssueType.NPlusOne,
                    Severity = IssueSeverity.High,
                    Description = "可能存在 N+1 查询问题，考虑使用 JOIN",
                    Recommendation = "使用 JOIN 替代多次 IN 查询"
                });
            }

            // 检查全表扫描
            if (!sql.ToUpper().Contains("WHERE") && !sql.ToUpper().Contains("LIMIT"))
            {
                issues.Add(new QueryIssue
                {
                    Type = IssueType.FullTableScan,
                    Severity = IssueSeverity.Critical,
                    Description = "查询可能导致全表扫描",
                    Recommendation = "添加 WHERE 子句或 LIMIT 限制"
                });
            }

            // 检查大结果集
            if (!sql.ToUpper().Contains("LIMIT") && !sql.ToUpper().Contains("TOP"))
            {
                issues.Add(new QueryIssue
                {
                    Type = IssueType.LargeResultset,
                    Severity = IssueSeverity.Medium,
                    Description = "查询可能返回大量数据",
                    Recommendation = "添加 LIMIT 或 TOP 限制结果集大小"
                });
            }

            // 检查未使用的子查询
            var subqueryPattern = new Regex(@"\(\s*SELECT\s+.+?\s+FROM\s+.+?\)", RegexOptions.IgnoreCase);
            var subqueries = subqueryPattern.Matches(sql);
            foreach (Match match in subqueries)
            {
                var subquery = match.Value;
                if (!subquery.Contains("WHERE") && !subquery.Contains("GROUP BY"))
                {
                    issues.Add(new QueryIssue
                    {
                        Type = IssueType.InefficientSubquery,
                        Severity = IssueSeverity.Medium,
                        Description = "子查询可能效率低下",
                        Recommendation = "考虑使用 JOIN 或 EXISTS 替代"
                    });
                }
            }

            return issues;
        }

        /// <summary>
        /// 生成查询执行计划建议
        /// </summary>
        public static ExecutionPlanSuggestions SuggestExecutionPlan(string sql)
        {
            var suggestions = new ExecutionPlanSuggestions();

            // 分析查询复杂度
            var complexity = AnalyzeQueryComplexity(sql);
            suggestions.Complexity = complexity;

            // 建议并行度
            if (complexity == QueryComplexity.High || complexity == QueryComplexity.VeryHigh)
            {
                suggestions.SuggestedParallelism = true;
                suggestions.RecommendedParallelismLevel = Math.Min(Environment.ProcessorCount, 4);
            }

            // 建议缓存策略
            if (sql.ToUpper().Contains("SELECT") && !sql.ToUpper().Contains("INSERT") && !sql.ToUpper().Contains("UPDATE") && !sql.ToUpper().Contains("DELETE"))
            {
                suggestions.SuggestedCacheStrategy = CacheStrategy.ReadCache;
            }

            // 建议分批处理
            if (sql.ToUpper().Contains("UPDATE") || sql.ToUpper().Contains("DELETE"))
            {
                var whereMatch = Regex.Match(sql, @"WHERE\s+(.+?)(?:\s+LIMIT|$)", RegexOptions.IgnoreCase);
                if (!whereMatch.Success)
                {
                    suggestions.SuggestedBatchProcessing = true;
                    suggestions.RecommendedBatchSize = 1000;
                }
            }

            return suggestions;
        }

        #region 私有辅助方法

        private static string ReplaceSelectStar(string sql)
        {
            var tableMatch = Regex.Match(sql, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            if (tableMatch.Success)
            {
                var tableName = tableMatch.Groups[1].Value;
                // 实际应用中应该查询表结构，这里只是示例
                return sql.Replace("SELECT *", $"SELECT id, name, created_at");
            }
            return sql;
        }

        private static string ReplaceSubqueries(string sql)
        {
            // 简单示例：将 IN (SELECT ...) 替换为 JOIN
            return Regex.Replace(sql, @"IN\s*\(\s*SELECT\s+(\w+)\s+FROM\s+(\w+)\)", "JOIN $2 ON $1 = $2.id", RegexOptions.IgnoreCase);
        }

        private static string ExtractTableName(string sql)
        {
            var match = Regex.Match(sql, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "unknown";
        }

        private static bool IsFunction(string column)
        {
            var functions = new[] { "COUNT", "SUM", "AVG", "MAX", "MIN", "UPPER", "LOWER", "SUBSTRING", "DATE_FORMAT" };
            return functions.Any(f => column.ToUpper().StartsWith(f));
        }

        private static double CalculateEstimatedImprovement(List<OptimizationSuggestion> optimizations)
        {
            if (!optimizations.Any()) return 0;

            var totalImprovement = 0.0;
            foreach (var opt in optimizations)
            {
                var range = opt.EstimatedImprovement.Split('-');
                if (range.Length == 2)
                {
                    var min = double.Parse(range[0].TrimEnd('%'));
                    var max = double.Parse(range[1].TrimEnd('%'));
                    totalImprovement += (min + max) / 2;
                }
                else if (range.Length == 1)
                {
                    totalImprovement += double.Parse(range[0].TrimEnd('%'));
                }
            }

            return Math.Min(totalImprovement, 100);
        }

        private static List<PerformanceRecommendation> GeneratePerformanceRecommendations(string sql, double executionTimeMs, int rowsReturned)
        {
            var recommendations = new List<PerformanceRecommendation>();

            if (executionTimeMs > 1000)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.Optimization,
                    Priority = RecommendationPriority.High,
                    Description = "查询执行时间过长，建议优化",
                    Actions = new[] { "添加索引", "优化 WHERE 条件", "减少返回列数" }
                });
            }

            if (rowsReturned > 10000)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.Pagination,
                    Priority = RecommendationPriority.Medium,
                    Description = "返回数据量过大，建议分页",
                    Actions = new[] { "添加 LIMIT", "实现分页查询" }
                });
            }

            if (sql.ToUpper().Contains("SELECT *"))
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.Optimization,
                    Priority = RecommendationPriority.Low,
                    Description = "避免使用 SELECT *",
                    Actions = new[] { "指定具体列名", "减少数据传输" }
                });
            }

            return recommendations;
        }

        private static QueryComplexity AnalyzeQueryComplexity(string sql)
        {
            var score = 0;

            // JOIN 数量
            score += Regex.Matches(sql, @"\bJOIN\b", RegexOptions.IgnoreCase).Count * 2;

            // 子查询数量
            score += Regex.Matches(sql, @"\(\s*SELECT", RegexOptions.IgnoreCase).Count * 3;

            // WHERE 条件复杂度
            var whereMatch = Regex.Match(sql, @"WHERE\s+(.+?)(?:\s+GROUP BY|\s+ORDER BY|$)", RegexOptions.IgnoreCase);
            if (whereMatch.Success)
            {
                var conditions = whereMatch.Groups[1].Value.Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries);
                score += conditions.Length;
            }

            // 聚合函数
            score += Regex.Matches(sql, @"\b(COUNT|SUM|AVG|MAX|MIN)\b", RegexOptions.IgnoreCase).Count * 2;

            // GROUP BY
            if (sql.ToUpper().Contains("GROUP BY"))
            {
                score += 3;
            }

            if (score < 5)
                return QueryComplexity.Low;
            else if (score < 10)
                return QueryComplexity.Medium;
            else if (score < 20)
                return QueryComplexity.High;
            else
                return QueryComplexity.VeryHigh;
        }

        #endregion
    }

    #region 结果类

    /// <summary>
    /// 查询优化结果
    /// </summary>
    public class QueryOptimizationResult
    {
        public string OriginalSql { get; set; }
        public string OptimizedSql { get; set; }
        public List<OptimizationSuggestion> Optimizations { get; set; }
        public double EstimatedImprovement { get; set; }
    }

    /// <summary>
    /// 优化建议
    /// </summary>
    public class OptimizationSuggestion
    {
        public OptimizationType Type { get; set; }
        public OptimizationPriority Priority { get; set; }
        public string Description { get; set; }
        public string EstimatedImprovement { get; set; }
    }

    /// <summary>
    /// 优化类型
    /// </summary>
    public enum OptimizationType
    {
        ColumnSelection,
        Filtering,
        Join,
        Subquery,
        Sorting,
        PartialMatch,
        Deduplication
    }

    /// <summary>
    /// 优化优先级
    /// </summary>
    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 查询性能分析
    /// </summary>
    public class QueryPerformanceAnalysis
    {
        public string Sql { get; set; }
        public double ExecutionTimeMs { get; set; }
        public int RowsReturned { get; set; }
        public PerformanceLevel PerformanceLevel { get; set; }
        public string Rating { get; set; }
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
    }

    /// <summary>
    /// 性能级别
    /// </summary>
    public enum PerformanceLevel
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }

    /// <summary>
    /// 性能建议
    /// </summary>
    public class PerformanceRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Description { get; set; }
        public string[] Actions { get; set; }
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum RecommendationType
    {
        Optimization,
        Pagination,
        Indexing,
        Caching
    }

    /// <summary>
    /// 建议优先级
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// 索引建议优先级
    /// </summary>
    public enum IndexPriority
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
        public IndexPriority Priority { get; set; }
        public string Reason { get; set; }

        public override bool Equals(object obj)
        {
            return obj is IndexSuggestion other &&
                   TableName == other.TableName &&
                   ColumnName == other.ColumnName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableName, ColumnName);
        }
    }

    /// <summary>
    /// 查询问题
    /// </summary>
    public class QueryIssue
    {
        public IssueType Type { get; set; }
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// 问题类型
    /// </summary>
    public enum IssueType
    {
        NPlusOne,
        FullTableScan,
        LargeResultset,
        InefficientSubquery
    }

    /// <summary>
    /// 问题严重程度
    /// </summary>
    public enum IssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 执行计划建议
    /// </summary>
    public class ExecutionPlanSuggestions
    {
        public QueryComplexity Complexity { get; set; }
        public bool SuggestedParallelism { get; set; }
        public int RecommendedParallelismLevel { get; set; }
        public CacheStrategy SuggestedCacheStrategy { get; set; }
        public bool SuggestedBatchProcessing { get; set; }
        public int RecommendedBatchSize { get; set; }
    }

    /// <summary>
    /// 查询复杂度
    /// </summary>
    public enum QueryComplexity
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// 缓存策略
    /// </summary>
    public enum CacheStrategy
    {
        None,
        ReadCache,
        WriteCache,
        ReadWriteCache
    }

    /// <summary>
    /// 优化选项
    /// </summary>
    public class OptimizationOptions
    {
        public bool OptimizeColumns { get; set; } = true;
        public bool OptimizeJoins { get; set; } = true;
        public bool OptimizeSubqueries { get; set; } = true;
        public bool OptimizeSorting { get; set; } = true;

        public static OptimizationOptions Default => new OptimizationOptions();
    }

    #endregion
}