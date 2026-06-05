using System;
using FastData.Context;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FastData.Model;

namespace FastData.DevTools
{
    /// <summary>
    /// API 测试工具
    /// </summary>
    public static class ApiTester
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>
        /// 测试 API 端点
        /// </summary>
        public static async Task<ApiTestResult> TestEndpoint(string url, HttpMethod method = null, string body = null, Dictionary<string, string> headers = null)
        {
            var result = new ApiTestResult
            {
                Url = url,
                Method = method?.Method ?? "GET",
                StartTime = DateTime.Now
            };

            try
            {
                var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.StatusCode = (int)response.StatusCode;
                result.IsSuccess = response.IsSuccessStatusCode;
                result.Content = await response.Content.ReadAsStringAsync();

                result.Headers = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    result.Headers[header.Key] = string.Join(", ", header.Value);
                }
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Error = ex.Message;
                result.IsSuccess = false;
            }

            return result;
        }

        /// <summary>
        /// 批量测试 API 端点
        /// </summary>
        public static async Task<List<ApiTestResult>> TestEndpoints(List<ApiTestCase> testCases, bool parallel = true)
        {
            if (parallel)
            {
                var tasks = testCases.Select(tc => TestEndpoint(tc.Url, tc.Method, tc.Body, tc.Headers));
                return (await Task.WhenAll(tasks)).ToList();
            }
            else
            {
                var results = new List<ApiTestResult>();
                foreach (var testCase in testCases)
                {
                    var result = await TestEndpoint(testCase.Url, testCase.Method, testCase.Body, testCase.Headers);
                    results.Add(result);
                }
                return results;
            }
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public static async Task<LoadTestResult> LoadTest(string url, int requestCount, int concurrency = 1)
        {
            var result = new LoadTestResult
            {
                Url = url,
                RequestCount = requestCount,
                Concurrency = concurrency,
                StartTime = DateTime.Now
            };

            var results = new List<ApiTestResult>();
            var semaphore = new System.Threading.SemaphoreSlim(concurrency);

            var tasks = Enumerable.Range(0, requestCount).Select(async i =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var testResult = await TestEndpoint(url);
                    lock (results)
                    {
                        results.Add(testResult);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;
            result.SuccessCount = results.Count(r => r.IsSuccess);
            result.FailureCount = results.Count(r => !r.IsSuccess);
            result.AverageResponseTime = results.Average(r => r.Duration.TotalMilliseconds);
            result.MinResponseTime = results.Min(r => r.Duration.TotalMilliseconds);
            result.MaxResponseTime = results.Max(r => r.Duration.TotalMilliseconds);
            result.RequestsPerSecond = result.SuccessCount / result.Duration.TotalSeconds;

            return result;
        }

        /// <summary>
        /// 生成测试报告
        /// </summary>
        public static ApiTestReport GenerateReport(List<ApiTestResult> results)
        {
            return new ApiTestReport
            {
                TotalTests = results.Count,
                SuccessCount = results.Count(r => r.IsSuccess),
                FailureCount = results.Count(r => !r.IsSuccess),
                AverageResponseTime = results.Where(r => r.IsSuccess).Average(r => r.Duration.TotalMilliseconds),
                MinResponseTime = results.Where(r => r.IsSuccess).Min(r => r.Duration.TotalMilliseconds),
                MaxResponseTime = results.Where(r => r.IsSuccess).Max(r => r.Duration.TotalMilliseconds),
                SuccessRate = (double)results.Count(r => r.IsSuccess) / results.Count * 100,
                StatusCodeDistribution = results.GroupBy(r => r.StatusCode)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 比较两个测试结果
        /// </summary>
        public static ApiTestComparison CompareResults(List<ApiTestResult> results1, List<ApiTestResult> results2, string label1, string label2)
        {
            var report1 = GenerateReport(results1);
            var report2 = GenerateReport(results2);

            return new ApiTestComparison
            {
                Label1 = label1,
                Label2 = label2,
                SuccessCount1 = report1.SuccessCount,
                SuccessCount2 = report2.SuccessCount,
                AverageResponseTime1 = report1.AverageResponseTime,
                AverageResponseTime2 = report2.AverageResponseTime,
                SuccessRate1 = report1.SuccessRate,
                SuccessRate2 = report2.SuccessRate,
                PerformanceImprovement = report1.AverageResponseTime - report2.AverageResponseTime,
                PerformanceImprovementPercentage = (report1.AverageResponseTime - report2.AverageResponseTime) / report1.AverageResponseTime * 100
            };
        }
    }

    /// <summary>
    /// 数据库监控工具
    /// </summary>
    public static class DatabaseMonitor
    {
        private static readonly Dictionary<string, MonitoringSession> _sessions = new Dictionary<string, MonitoringSession>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 开始监控会话
        /// </summary>
        public static MonitoringSession StartMonitoring(string dbKey, MonitoringOptions options = null)
        {
            options = options ?? MonitoringOptions.Default;

            var session = new MonitoringSession
            {
                Id = Guid.NewGuid().ToString(),
                DbKey = dbKey,
                StartTime = DateTime.Now,
                Options = options,
                Queries = new List<QueryMetrics>()
            };

            lock (_lock)
            {
                _sessions[session.Id] = session;
            }

            return session;
        }

        /// <summary>
        /// 记录查询
        /// </summary>
        public static void RecordQuery(string sessionId, string sql, long durationMs, int rowsAffected = 0)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    session.Queries.Add(new QueryMetrics
                    {
                        Sql = sql,
                        DurationMs = durationMs,
                        RowsAffected = rowsAffected,
                        Timestamp = DateTime.Now
                    });

                    session.TotalQueries++;
                    session.TotalDuration += durationMs;

                    if (durationMs > session.MaxQueryDuration)
                    {
                        session.MaxQueryDuration = durationMs;
                        session.SlowestQuery = sql;
                    }

                    if (durationMs < session.MinQueryDuration || session.MinQueryDuration == 0)
                    {
                        session.MinQueryDuration = durationMs;
                    }
                }
            }
        }

        /// <summary>
        /// 停止监控会话
        /// </summary>
        public static MonitoringReport StopMonitoring(string sessionId)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    session.EndTime = DateTime.Now;
                    _sessions.Remove(sessionId);

                    return GenerateMonitoringReport(session);
                }
            }

            return null;
        }

        /// <summary>
        /// 获取监控会话
        /// </summary>
        public static MonitoringSession GetSession(string sessionId)
        {
            lock (_lock)
            {
                return _sessions.TryGetValue(sessionId, out var session) ? session : null;
            }
        }

        /// <summary>
        /// 获取所有活动会话
        /// </summary>
        public static List<MonitoringSession> GetActiveSessions()
        {
            lock (_lock)
            {
                return _sessions.Values.ToList();
            }
        }

        /// <summary>
        /// 生成监控报告
        /// </summary>
        public static MonitoringReport GenerateMonitoringReport(MonitoringSession session)
        {
            var queries = session.Queries;
            var duration = session.EndTime - session.StartTime;

            return new MonitoringReport
            {
                SessionId = session.Id,
                DbKey = session.DbKey,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = duration,
                TotalQueries = session.TotalQueries,
                TotalDuration = session.TotalDuration,
                AverageQueryDuration = session.TotalQueries > 0 ? session.TotalDuration / (double)session.TotalQueries : 0,
                MinQueryDuration = session.MinQueryDuration,
                MaxQueryDuration = session.MaxQueryDuration,
                SlowestQuery = session.SlowestQuery,
                SlowQueries = queries.Where(q => q.DurationMs > session.Options.SlowQueryThresholdMs).ToList(),
                QueriesPerSecond = duration.TotalSeconds > 0 ? session.TotalQueries / duration.TotalSeconds : 0,
                ErrorQueries = queries.Where(q => q.DurationMs < 0).ToList()
            };
        }

        /// <summary>
        /// 实时监控
        /// </summary>
        public static async Task<MonitoringReport> MonitorRealTime(string dbKey, TimeSpan duration, MonitoringOptions options = null)
        {
            var session = StartMonitoring(dbKey, options);

            await Task.Delay(duration);

            return StopMonitoring(session.Id);
        }

        /// <summary>
        /// 检测性能异常
        /// </summary>
        public static List<PerformanceAnomaly> DetectAnomalies(MonitoringReport report)
        {
            var anomalies = new List<PerformanceAnomaly>();

            // 检测慢查询
            if (report.SlowQueries.Any())
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    Type = AnomalyType.SlowQuery,
                    Severity = AnomalySeverity.High,
                    Description = string.Format("检测到 {0} 个慢查询", report.SlowQueries.Count),
                    Details = report.SlowQueries.Select(q => q.Sql).ToList()
                });
            }

            // 检测平均查询时间异常
            if (report.AverageQueryDuration > 1000)
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    Type = AnomalyType.HighLatency,
                    Severity = AnomalySeverity.Medium,
                    Description = string.Format("平均查询时间过长: {0:F2} ms", report.AverageQueryDuration),
                    Details = new List<string> { "阈值: 1000 ms" }
                });
            }

            // 检测查询频率异常
            if (report.QueriesPerSecond > 1000)
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    Type = AnomalyType.HighFrequency,
                    Severity = AnomalySeverity.Medium,
                    Description = string.Format("查询频率过高: {0:F2} QPS", report.QueriesPerSecond),
                    Details = new List<string> { "阈值: 1000 QPS" }
                });
            }

            // 检测错误查询
            if (report.ErrorQueries.Any())
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    Type = AnomalyType.QueryError,
                    Severity = AnomalySeverity.Critical,
                    Description = string.Format("检测到 {0} 个错误查询", report.ErrorQueries.Count),
                    Details = report.ErrorQueries.Select(q => q.Sql).ToList()
                });
            }

            return anomalies;
        }
    }

    #region 数据模型

    /// <summary>
    /// API 测试结果
    /// </summary>
    public class ApiTestResult
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    /// <summary>
    /// API 测试用例
    /// </summary>
    public class ApiTestCase
    {
        public string Url { get; set; }
        public HttpMethod Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string ExpectedStatusCode { get; set; }
    }

    /// <summary>
    /// 负载测试结果
    /// </summary>
    public class LoadTestResult
    {
        public string Url { get; set; }
        public int RequestCount { get; set; }
        public int Concurrency { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
    }

    /// <summary>
    /// API 测试报告
    /// </summary>
    public class ApiTestReport
    {
        public int TotalTests { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<int, int> StatusCodeDistribution { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// API 测试比较
    /// </summary>
    public class ApiTestComparison
    {
        public string Label1 { get; set; }
        public string Label2 { get; set; }
        public int SuccessCount1 { get; set; }
        public int SuccessCount2 { get; set; }
        public double AverageResponseTime1 { get; set; }
        public double AverageResponseTime2 { get; set; }
        public double SuccessRate1 { get; set; }
        public double SuccessRate2 { get; set; }
        public double PerformanceImprovement { get; set; }
        public double PerformanceImprovementPercentage { get; set; }
    }

    /// <summary>
    /// 监控会话
    /// </summary>
    public class MonitoringSession
    {
        public string Id { get; set; }
        public string DbKey { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public MonitoringOptions Options { get; set; }
        public List<QueryMetrics> Queries { get; set; }
        public int TotalQueries { get; set; }
        public long TotalDuration { get; set; }
        public double MinQueryDuration { get; set; }
        public long MaxQueryDuration { get; set; }
        public string SlowestQuery { get; set; }
    }

    /// <summary>
    /// 监控选项
    /// </summary>
    public class MonitoringOptions
    {
        public int SlowQueryThresholdMs { get; set; } = 1000;
        public bool RecordFailedQueries { get; set; } = true;
        public int MaxQueriesToRecord { get; set; } = 10000;

        public static MonitoringOptions Default => new MonitoringOptions();
    }

    /// <summary>
    /// 查询指标
    /// </summary>
    public class QueryMetrics
    {
        public string Sql { get; set; }
        public long DurationMs { get; set; }
        public int RowsAffected { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 监控报告
    /// </summary>
    public class MonitoringReport
    {
        public string SessionId { get; set; }
        public string DbKey { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalQueries { get; set; }
        public long TotalDuration { get; set; }
        public double AverageQueryDuration { get; set; }
        public double MinQueryDuration { get; set; }
        public long MaxQueryDuration { get; set; }
        public string SlowestQuery { get; set; }
        public List<QueryMetrics> SlowQueries { get; set; }
        public double QueriesPerSecond { get; set; }
        public List<QueryMetrics> ErrorQueries { get; set; }
    }

    /// <summary>
    /// 性能异常
    /// </summary>
    public class PerformanceAnomaly
    {
        public AnomalyType Type { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; }
        public List<string> Details { get; set; }
    }

    /// <summary>
    /// 异常类型
    /// </summary>
    public enum AnomalyType
    {
        SlowQuery,
        HighLatency,
        HighFrequency,
        QueryError,
        ConnectionLeak
    }

    /// <summary>
    /// 异常严重程度
    /// </summary>
    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}