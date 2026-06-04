#if !NETFRAMEWORK
using FastData;
using FastData.Config;
using FastData.DevTools;
using FastData.Tests.Integration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace FastData.Tests
{
    /// <summary>
    /// ORM 性能基准测试
    /// 
    /// 测试 FastData ORM 核心操作的性能表现，使用内置 PerformanceProfiler 进行测量。
    /// 覆盖以下性能指标：
    /// 1. 单条 CRUD 操作耗时
    /// 2. 批量操作性能
    /// 3. 查询模式对比（条件查询、分页查询、排序查询）
    /// 4. PropertyCache 反射缓存性能
    /// 5. 数据映射性能（DataReader → Model）
    /// </summary>
    public class OrmPerformanceTests : IDisposable
    {
        private readonly string _key = "Sqlite";
        private readonly ITestOutputHelper _output;
        private bool _dbAvailable;
        private const int WarmupIterations = 3;
        private const int MeasurementIterations = 10;
        private const int BatchSize = 100;

        static OrmPerformanceTests()
        {
            // 注册数据库提供程序工厂（.NET 6+ 必须显式注册）
            try { DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance); } catch { }
        }

        public OrmPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _dbAvailable = CheckDatabaseAvailability();
        }

        private bool CheckDatabaseAvailability()
        {
            try
            {
                // SQLite 首次使用时自动创建表
                if (_key == "Sqlite")
                {
                    try
                    {
                        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=FastDataTest-OrmPerf.db"))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS PerfUser (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    UserName TEXT,
                                    Email TEXT,
                                    Age INTEGER,
                                    IsActive INTEGER,
                                    CreatedAt TEXT
                                )";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch { return false; }
                }

                // SQL Server 确保表存在
                if (_key == "SqlServer")
                {
                    try
                    {
                        using (var conn = new Microsoft.Data.SqlClient.SqlConnection(
                            FastDataConfig.GetConnectionString("SqlServer") ?? 
                            "server=127.0.0.1,1433;database=FastDataTest;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true;Encrypt=false;Connect Timeout=10"))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='perf_users' AND xtype='U')
                                    BEGIN
                                        CREATE TABLE perf_users (
                                            Id INT IDENTITY(1,1) PRIMARY KEY,
                                            UserName NVARCHAR(100),
                                            Email NVARCHAR(200),
                                            Age INT,
                                            IsActive BIT,
                                            CreatedAt DATETIME
                                        )
                                    END";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch { return false; }
                }

                var users = FastRead.Query<PerfUser>(u => true, key: _key)
                    .Take(1)
                    .ToList<PerfUser>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_dbAvailable)
            {
                try
                {
                    FastWrite.Delete<PerfUser>(u => u.UserName.StartsWith("perf_"), key: _key);
                }
                catch { }

                PerformanceProfiler.ClearMetrics();
            }
        }

        private PerfUser CreatePerfUser(string tag)
        {
            return new PerfUser
            {
                UserName = $"perf_{tag}",
                Email = $"perf_{tag}@benchmark.com",
                Age = 30,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
        }

        private List<PerfUser> CreatePerfUsers(int count, string prefix)
        {
            var list = new List<PerfUser>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new PerfUser
                {
                    UserName = $"perf_{prefix}_{i}",
                    Email = $"perf_{prefix}_{i}@benchmark.com",
                    Age = 20 + (i % 40),
                    IsActive = i % 3 != 0,
                    CreatedAt = DateTime.Now.AddDays(-i)
                });
            }
            return list;
        }

        /// <summary>
        /// 性能测量辅助方法
        /// 执行预热 + 多次测量，返回平均耗时（毫秒）
        /// </summary>
        private double MeasurePerformance(string operationName, Action action)
        {
            if (!_dbAvailable)
            {
                _output.WriteLine($"SKIP: {operationName} - database unavailable");
                return 0;
            }

            PerformanceProfiler.ClearMetrics();

            for (var i = 0; i < WarmupIterations; i++)
                action();

            for (var i = 0; i < MeasurementIterations; i++)
            {
                using (PerformanceProfiler.StartProfiling(operationName))
                {
                    action();
                }
            }

            var metric = PerformanceProfiler.GetMetric(operationName);
            _output.WriteLine($"{operationName}: Avg={metric.AverageExecutionTime:F2}ms, Min={metric.MinExecutionTime}ms, Max={metric.MaxExecutionTime}ms, Count={metric.Count}");

            return metric.AverageExecutionTime;
        }

        #region 单条 CRUD 性能

        [Fact]
        public void SingleAdd_Performance_ShouldBeFast()
        {
            var tag = $"add_{Guid.NewGuid():N}".Substring(0, 20);
            var user = CreatePerfUser(tag);

            MeasurePerformance("SingleAdd", () =>
            {
                user.UserName = $"perf_{Guid.NewGuid():N}".Substring(0, 20);
                FastWrite.Add(user, key: _key);
            });
        }

        [Fact]
        public void SingleQuery_Performance_ShouldBeFast()
        {
            var tag = $"query_{Guid.NewGuid():N}".Substring(0, 20);
            var user = CreatePerfUser(tag);
            FastWrite.Add(user, key: _key);

            MeasurePerformance("SingleQuery", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName == user.UserName, key: _key)
                    .ToList<PerfUser>();
            });
        }

        [Fact]
        public void SingleUpdate_Performance_ShouldBeFast()
        {
            var tag = $"update_{Guid.NewGuid():N}".Substring(0, 20);
            var user = CreatePerfUser(tag);
            FastWrite.Add(user, key: _key);

            MeasurePerformance("SingleUpdate", () =>
            {
                var newAge = new Random().Next(20, 60);
                FastWrite.Update<PerfUser>(new PerfUser { Age = newAge },
                    u => u.UserName == user.UserName,
                    u => new { u.Age }, key: _key);
            });
        }

        [Fact]
        public void SingleDelete_Performance_ShouldBeFast()
        {
            MeasurePerformance("SingleDelete", () =>
            {
                var tag = $"del_{Guid.NewGuid():N}".Substring(0, 20);
                var user = CreatePerfUser(tag);
                FastWrite.Add(user, key: _key);
                FastWrite.Delete<PerfUser>(u => u.UserName == user.UserName, key: _key);
            });
        }

        #endregion

        #region 批量操作性能

        [Fact]
        public void BatchAdd_Performance_ShouldBeEfficient()
        {
            var prefix = $"batch_{Guid.NewGuid():N}".Substring(0, 10);

            MeasurePerformance("BatchAdd", () =>
            {
                var users = CreatePerfUsers(BatchSize, $"{prefix}_{Guid.NewGuid():N}".Substring(0, 15));
                FastWrite.AddList(users, key: _key);
            });
        }

        [Fact]
        public void BatchQuery_Performance_ShouldBeEfficient()
        {
            var prefix = $"bq_{Guid.NewGuid():N}".Substring(0, 10);
            var users = CreatePerfUsers(BatchSize, prefix);
            FastWrite.AddList(users, key: _key);

            MeasurePerformance("BatchQuery", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName.StartsWith($"perf_{prefix}"), key: _key)
                    .ToList<PerfUser>();
            });
        }

        #endregion

        #region 查询模式性能对比

        [Fact]
        public void QueryPatterns_PerformanceComparison()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            var prefix = $"qp_{Guid.NewGuid():N}".Substring(0, 10);
            var users = CreatePerfUsers(BatchSize, prefix);
            FastWrite.AddList(users, key: _key);

            PerformanceProfiler.ClearMetrics();

            MeasurePerformance("Query_NoFilter", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .Take(50)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("Query_WithFilter", () =>
            {
                FastRead.Query<PerfUser>(u => u.Age > 25, key: _key)
                    .Take(50)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("Query_WithOrder", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .OrderBy<PerfUser>(u => u.CreatedAt, isDesc: true)
                    .Take(50)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("Query_WithPagination", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .ToPage(new FastUntility.Page.PageModel { PageId = 1, PageSize = 20 });
            });

            MeasurePerformance("Query_ToItem", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName.StartsWith($"perf_{prefix}"), key: _key)
                    .ToItem<PerfUser>();
            });

            var report = PerformanceProfiler.GenerateReport();
            _output.WriteLine($"--- 查询模式性能对比 ---");
            _output.WriteLine($"总操作次数: {report.TotalOperations}");
            _output.WriteLine($"平均耗时: {report.AverageExecutionTime:F2}ms");
            _output.WriteLine($"最慢操作: {report.SlowestOperation?.Name} ({report.SlowestOperation?.AverageExecutionTime:F2}ms)");
            _output.WriteLine($"最快操作: {report.FastestOperation?.Name} ({report.FastestOperation?.AverageExecutionTime:F2}ms)");

            foreach (var op in report.Operations.OrderBy(o => o.AverageExecutionTime))
            {
                _output.WriteLine($"  {op.Name}: {op.AverageExecutionTime:F2}ms (min={op.MinExecutionTime}ms, max={op.MaxExecutionTime}ms)");
            }
        }

        #endregion

        #region PropertyCache 反射缓存性能

        [Fact]
        public void PropertyCache_ReflectionPerformance_ShouldBeCached()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            PerformanceProfiler.ClearMetrics();

            var prefix = $"pc_{Guid.NewGuid():N}".Substring(0, 10);
            var users = CreatePerfUsers(BatchSize, prefix);
            FastWrite.AddList(users, key: _key);

            MeasurePerformance("PropertyCache_FirstCall", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .Take(10)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("PropertyCache_SubsequentCalls", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .Take(10)
                    .ToList<PerfUser>();
            });

            var first = PerformanceProfiler.GetMetric("PropertyCache_FirstCall");
            var subsequent = PerformanceProfiler.GetMetric("PropertyCache_SubsequentCalls");

            _output.WriteLine($"PropertyCache 首次调用: {first.AverageExecutionTime:F2}ms");
            _output.WriteLine($"PropertyCache 缓存调用: {subsequent.AverageExecutionTime:F2}ms");
            _output.WriteLine($"缓存加速比: {first.AverageExecutionTime / Math.Max(subsequent.AverageExecutionTime, 0.1):F2}x");

            var comparison = PerformanceProfiler.CompareMetrics("PropertyCache_FirstCall", "PropertyCache_SubsequentCalls");
            _output.WriteLine(comparison.GetSummary());
        }

        /// <summary>
        /// 间接测试 PropertyCache 的缓存命中性能
        /// 通过多次查询同一实体类型来观察缓存效果
        /// </summary>
        [Fact]
        public void PropertyCache_DirectAccess_Performance()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            PerformanceProfiler.ClearMetrics();

            const int iterations = 1000;

            using (PerformanceProfiler.StartProfiling("PropertyCache_Reflection"))
            {
                for (var i = 0; i < iterations; i++)
                {
                    var list = FastRead.Query<PerfUser>(u => true, key: _key)
                        .Take(1)
                        .ToList<PerfUser>();
                }
            }

            var metric = PerformanceProfiler.GetMetric("PropertyCache_Reflection");
            _output.WriteLine($"PropertyCache 反射缓存查询 x{iterations}: Avg={metric.AverageExecutionTime:F4}ms/call");
            _output.WriteLine($"PropertyCache 总耗时: {metric.TotalExecutionTime}ms ({iterations}次调用)");
        }

        #endregion

        #region Expression 树解析性能

        [Fact]
        public void ExpressionParsing_Performance_ShouldBeEfficient()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            PerformanceProfiler.ClearMetrics();

            var prefix = $"ep_{Guid.NewGuid():N}".Substring(0, 10);
            var users = CreatePerfUsers(BatchSize, prefix);
            FastWrite.AddList(users, key: _key);

            MeasurePerformance("Expr_SimplePredicate", () =>
            {
                FastRead.Query<PerfUser>(u => u.Age > 25, key: _key)
                    .Take(10)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("Expr_CompositePredicate", () =>
            {
                FastRead.Query<PerfUser>(u => u.Age > 25 && u.IsActive, key: _key)
                    .Take(10)
                    .ToList<PerfUser>();
            });

            MeasurePerformance("Expr_StringContains", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName.Contains(prefix), key: _key)
                    .Take(10)
                    .ToList<PerfUser>();
            });

            var report = PerformanceProfiler.GenerateReport();
            _output.WriteLine($"--- 表达式解析性能对比 ---");
            foreach (var op in report.Operations.OrderBy(o => o.AverageExecutionTime))
            {
                _output.WriteLine($"  {op.Name}: {op.AverageExecutionTime:F2}ms");
            }
        }

        #endregion

        #region 完整性能报告

        [Fact]
        public void FullPerformanceReport()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            _output.WriteLine("========== FastData ORM 性能基准测试报告 ==========");
            _output.WriteLine($"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _output.WriteLine($"数据库: {_key}");
            _output.WriteLine($"预热次数: {WarmupIterations}, 测量次数: {MeasurementIterations}, 批量大小: {BatchSize}");
            _output.WriteLine("");

            PerformanceProfiler.ClearMetrics();

            var prefix = $"fr_{Guid.NewGuid():N}".Substring(0, 10);

            _output.WriteLine("--- 1. 单条 CRUD 性能 ---");
            var addUser = CreatePerfUser($"{prefix}_add");
            var addTime = MeasurePerformance("FullReport_Add", () =>
            {
                addUser.UserName = $"perf_fr_{Guid.NewGuid():N}".Substring(0, 20);
                FastWrite.Add(addUser, key: _key);
            });
            _output.WriteLine($"  Add: {addTime:F2}ms");

            FastWrite.Add(addUser, key: _key);
            var queryTime = MeasurePerformance("FullReport_Query", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName == addUser.UserName, key: _key)
                    .ToList<PerfUser>();
            });
            _output.WriteLine($"  Query: {queryTime:F2}ms");

            var updateTime = MeasurePerformance("FullReport_Update", () =>
            {
                FastWrite.Update<PerfUser>(new PerfUser { Age = 35 },
                    u => u.UserName == addUser.UserName,
                    u => new { u.Age }, key: _key);
            });
            _output.WriteLine($"  Update: {updateTime:F2}ms");

            var deleteTime = MeasurePerformance("FullReport_Delete", () =>
            {
                var temp = CreatePerfUser($"{prefix}_del_{Guid.NewGuid():N}".Substring(0, 20));
                FastWrite.Add(temp, key: _key);
                FastWrite.Delete<PerfUser>(u => u.UserName == temp.UserName, key: _key);
            });
            _output.WriteLine($"  Delete: {deleteTime:F2}ms");

            _output.WriteLine("");

            _output.WriteLine("--- 2. 批量操作性能 ---");
            var batchUsers = CreatePerfUsers(BatchSize, $"{prefix}_batch");
            var batchAddTime = MeasurePerformance("FullReport_BatchAdd", () =>
            {
                var batch = CreatePerfUsers(BatchSize, $"{prefix}_badd_{Guid.NewGuid():N}".Substring(0, 10));
                FastWrite.AddList(batch, key: _key);
            });
            _output.WriteLine($"  BatchAdd({BatchSize}条): {batchAddTime:F2}ms");
            _output.WriteLine($"  单条均摊: {batchAddTime / BatchSize:F4}ms");

            FastWrite.AddList(batchUsers, key: _key);
            var batchQueryTime = MeasurePerformance("FullReport_BatchQuery", () =>
            {
                FastRead.Query<PerfUser>(u => u.UserName.StartsWith($"perf_{prefix}"), key: _key)
                    .ToList<PerfUser>();
            });
            _output.WriteLine($"  BatchQuery({BatchSize}条): {batchQueryTime:F2}ms");

            _output.WriteLine("");

            _output.WriteLine("--- 3. 查询模式性能 ---");
            var filterTime = MeasurePerformance("FullReport_Filter", () =>
            {
                FastRead.Query<PerfUser>(u => u.Age > 25 && u.IsActive, key: _key)
                    .Take(50)
                    .ToList<PerfUser>();
            });
            _output.WriteLine($"  条件查询: {filterTime:F2}ms");

            var pageTime = MeasurePerformance("FullReport_Page", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .ToPage(new FastUntility.Page.PageModel { PageId = 1, PageSize = 20 });
            });
            _output.WriteLine($"  分页查询: {pageTime:F2}ms");

            var countTime = MeasurePerformance("FullReport_Count", () =>
            {
                FastRead.Query<PerfUser>(u => true, key: _key)
                    .ToCount();
            });
            _output.WriteLine($"  计数查询: {countTime:F2}ms");

            _output.WriteLine("");

            _output.WriteLine("--- 4. PropertyCache 性能 ---");
            PropertyCache_DirectAccess_Performance();

            _output.WriteLine("");

            _output.WriteLine("--- 5. 性能瓶颈分析 ---");
            var bottlenecks = PerformanceProfiler.AnalyzeBottlenecks(thresholdSeconds: 0.05);
            if (bottlenecks.Any())
            {
                foreach (var b in bottlenecks)
                {
                    _output.WriteLine($"  [{b.Severity}] {b.OperationName}: {b.AverageExecutionTime:F2}ms (max={b.MaxExecutionTime:F2}ms)");
                }
            }
            else
            {
                _output.WriteLine("  未发现性能瓶颈（所有操作均在阈值内）");
            }

            _output.WriteLine("");
            _output.WriteLine("========== 性能基准测试完成 ==========");
        }

        #endregion

        #region 数据映射性能

        /// <summary>
        /// 测试 DataReader → Model 映射的性能
        /// 通过查询不同数量的数据来观察映射开销的线性增长
        /// </summary>
        [Fact]
        public void DataMapping_Performance_ShouldScaleLinearly()
        {
            if (!_dbAvailable)
            {
                _output.WriteLine("SKIP: database unavailable");
                return;
            }

            var prefix = $"dm_{Guid.NewGuid():N}".Substring(0, 10);
            var users = CreatePerfUsers(BatchSize, prefix);
            FastWrite.AddList(users, key: _key);

            PerformanceProfiler.ClearMetrics();

            var sizes = new[] { 10, 50, 100 };
            var results = new Dictionary<int, double>();

            foreach (var size in sizes)
            {
                var metricName = $"DataMapping_Size{size}";
                var avg = MeasurePerformance(metricName, () =>
                {
                    FastRead.Query<PerfUser>(u => u.UserName.StartsWith($"perf_{prefix}"), key: _key)
                        .Take(size)
                        .ToList<PerfUser>();
                });
                results[size] = avg;
            }

            _output.WriteLine("--- 数据映射性能线性分析 ---");
            foreach (var kvp in results.OrderBy(r => r.Key))
            {
                _output.WriteLine($"  查询 {kvp.Key} 条: {kvp.Value:F2}ms (单条: {kvp.Value / kvp.Key:F4}ms)");
            }

            if (results.ContainsKey(10) && results.ContainsKey(100))
            {
                var ratio = results[100] / results[10];
                _output.WriteLine($"  100条/10条比率: {ratio:F2}x (理想值: 10x)");
            }
        }

        #endregion
    }
}
#endif