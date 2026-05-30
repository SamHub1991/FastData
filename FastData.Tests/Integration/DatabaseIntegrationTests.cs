using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData.Base;
using FastData.Config;
using FastData.Context;
using FastData.Model;
using FastData.Property;
using FastUntility.Page;
using Xunit;

namespace FastData.Tests.Integration
{
    [Table(Name = "perf_users")]
    public class PerfUser
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Table(DbTableNames = "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users")]
    public class MultiDbUser
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    [Table(Name = "default_users", DbTableNames = "SqlServer.Users,MySql.user_info")]
    public class MixedDbUser
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public long ElapsedMs { get; set; }
        public double OpsPerSecond { get; set; }
        public string Details { get; set; }
    }

    public class DatabaseIntegrationTests
    {
        private static int _successCount = 0;
        private static int _errorCount = 0;
        private static List<string> _errors = new List<string>();
        private static readonly object _lockObj = new object();

        public DatabaseIntegrationTests()
        {
            // 注册数据库工厂
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
        }

        [Theory]
        [InlineData("SqlServer")]
        [InlineData("MySql")]
        [InlineData("PostgreSql")]
        public void FullIntegrationTest(string dbName)
        {
            if (!IsDatabaseAvailable(dbName))
            {
                Console.WriteLine($"{dbName} 不可用，跳过测试");
                return;
            }

            var results = new Dictionary<string, TestResult>();

            // 单线程测试
            results["单条插入"] = TestSingleInsert(dbName, 10);
            results["查询"] = TestQuery(dbName);
            results["删除"] = TestDelete(dbName);
            results["链式查询"] = TestChainQuery(dbName);
            results["分页查询"] = TestPagination(dbName);

            // 30线程并发测试
            results["并发读写"] = TestConcurrentReadWrite(dbName, 30, 10);

            // 打印汇总
            PrintSummary(dbName, results);
        }

        private bool IsDatabaseAvailable(string dbName)
        {
            try
            {
                using var db = new DataContext(dbName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private TestResult TestSingleInsert(string dbName, int count)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = 0;
            var failed = 0;
            string lastError = null;

            try
            {
                var entity = new PerfUser
                {
                    UserName = "TestUser_0",
                    Email = "user0@test.com",
                    Age = 25,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                using var db = new DataContext(dbName);
                var result = db.Add(entity);
                
                if (result.WriteReturn.IsSuccess)
                    success = 1;
                else
                {
                    failed = 1;
                    lastError = result.WriteReturn.Message;
                }
            }
            catch (Exception ex)
            {
                failed = 1;
                lastError = ex.Message;
            }

            stopwatch.Stop();

            return new TestResult
            {
                Success = failed == 0,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Details = failed == 0 ? "成功" : lastError
            };
        }

        private TestResult TestQuery(string dbName)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    var entity = new PerfUser
                    {
                        UserName = $"QueryTest_{i}",
                        Email = $"query{i}@test.com",
                        Age = 20 + i,
                        IsActive = i % 2 == 0,
                        CreatedAt = DateTime.Now
                    };
                    using var db = new DataContext(dbName);
                    db.Add(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  插入测试数据失败: {ex.Message}");
            }

            var stopwatch = Stopwatch.StartNew();
            List<PerfUser> list;
            using (var tempDb = new DataContext(dbName))
            {
                var result = tempDb.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                list = result.List;
            }
            stopwatch.Stop();

            return new TestResult
            {
                Success = true,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Details = $"返回 {list.Count} 条"
            };
        }

        private TestResult TestDelete(string dbName)
        {
            var entity = new PerfUser
            {
                UserName = "DeleteTest",
                Email = "delete@test.com",
                Age = 25,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            using (var db = new DataContext(dbName))
            {
                db.Add(entity);
            }

            var stopwatch = Stopwatch.StartNew();
            using (var db = new DataContext(dbName))
            {
                var deleteResult = db.Delete<PerfUser>(u => u.UserName == "DeleteTest");
                stopwatch.Stop();

                return new TestResult
                {
                    Success = deleteResult.WriteReturn.IsSuccess,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = deleteResult.WriteReturn.IsSuccess ? "成功" : deleteResult.WriteReturn.Message
                };
            }
        }

        private TestResult TestChainQuery(string dbName)
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    var entity = new PerfUser
                    {
                        UserName = $"ChainTest_{i}",
                        Email = $"chain{i}@test.com",
                        Age = 20 + i,
                        IsActive = i % 2 == 0,
                        CreatedAt = DateTime.Now
                    };
                    using var db = new DataContext(dbName);
                    db.Add(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  插入测试数据失败: {ex.Message}");
            }

            var stopwatch = Stopwatch.StartNew();
            var query = FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive)
                .And<PerfUser>(u => u.Age > 25);

            var list = FastRead.ToList<PerfUser>(query);
            stopwatch.Stop();

            return new TestResult
            {
                Success = true,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Details = $"返回 {list.Count} 条"
            };
        }

        private TestResult TestPagination(string dbName)
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    var entity = new PerfUser
                    {
                        UserName = $"PageTest_{i}",
                        Email = $"page{i}@test.com",
                        Age = 20 + i,
                        IsActive = i % 2 == 0,
                        CreatedAt = DateTime.Now
                    };
                    using var db = new DataContext(dbName);
                    db.Add(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  插入测试数据失败: {ex.Message}");
            }

            var stopwatch = Stopwatch.StartNew();
            var query = FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive);
            var pageResult = query.ToPage<PerfUser>(new PageModel { PageId = 1, PageSize = 10 });
            stopwatch.Stop();

            return new TestResult
            {
                Success = true,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Details = $"返回 {pageResult.list.Count} 条，总计 {pageResult.pModel.TotalRecord} 条"
            };
        }

        private TestResult TestConcurrentReadWrite(string dbName, int threadCount, int operationsPerThread)
        {
            _successCount = 0;
            _errorCount = 0;
            _errors.Clear();

            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
                Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        try
                        {
                            if (i % 3 == 0)
                            {
                                var entity = new PerfUser
                                {
                                    UserName = $"Concurrent_{threadId}_{i}",
                                    Email = $"concurrent_{threadId}_{i}@test.com",
                                    Age = 20 + (i % 50),
                                    IsActive = true,
                                    CreatedAt = DateTime.Now
                                };

                                using var db = new DataContext(dbName);
                                var result = db.Add(entity);
                                if (result.WriteReturn.IsSuccess)
                                    Interlocked.Increment(ref _successCount);
                                else
                                    Interlocked.Increment(ref _errorCount);
                            }
                            else
                            {
                                using (var tempDb = new DataContext(dbName))
                                {
                                    var result = tempDb.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                                }
                                Interlocked.Increment(ref _successCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref _errorCount);
                            lock (_lockObj)
                            {
                                if (_errors.Count < 10)
                                    _errors.Add($"Thread {threadId}: {ex.Message}");
                            }
                        }
                    }
                })
            ).ToArray();

            Task.WaitAll(tasks);
            stopwatch.Stop();

            var totalOps = threadCount * operationsPerThread;
            var opsPerSecond = totalOps * 1000.0 / stopwatch.ElapsedMilliseconds;

            return new TestResult
            {
                Success = _errorCount == 0,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                OpsPerSecond = opsPerSecond,
                Details = $"成功={_successCount}, 失败={_errorCount}, 吞吐量={opsPerSecond:F0} ops/s"
            };
        }

        private void PrintSummary(string dbName, Dictionary<string, TestResult> results)
        {
            Console.WriteLine($"\n{dbName}:");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"测试项",-15} {"结果",-8} {"耗时(ms)",-12} {"吞吐量",-15} {"详情"}");
            Console.WriteLine(new string('-', 80));

            foreach (var test in results)
            {
                var result = test.Value;
                Console.WriteLine($"{test.Key,-15} {(result.Success ? "PASS" : "FAIL"),-8} {result.ElapsedMs,-12} {result.OpsPerSecond.ToString("F0") + " ops/s",-15} {result.Details}");
            }

            var passCount = results.Count(v => v.Value.Success);
            var totalCount = results.Count;
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"总计: {passCount}/{totalCount} 通过");
        }

        [Fact]
        public void TableAttribute_DbTableNames_IsSet()
        {
            // 测试 TableAttribute 的 DbTableNames 属性
            var attr = typeof(MultiDbUser).GetCustomAttributes(typeof(TableAttribute), false)
                .FirstOrDefault() as TableAttribute;

            Assert.NotNull(attr);
            Assert.Equal("SqlServer.Users,MySql.user_info,PostgreSQL.tb_users", attr.DbTableNames);
            Assert.Null(attr.Name);
        }

        [Fact]
        public void TableAttribute_MixedMapping_HasBothProperties()
        {
            // 测试混合模式：同时有 Name 和 DbTableNames
            var attr = typeof(MixedDbUser).GetCustomAttributes(typeof(TableAttribute), false)
                .FirstOrDefault() as TableAttribute;

            Assert.NotNull(attr);
            Assert.Equal("default_users", attr.Name);
            Assert.Equal("SqlServer.Users,MySql.user_info", attr.DbTableNames);
        }

        [Fact]
        public void TableAttribute_PerfUser_HasOnlyName()
        {
            // 测试只有 Name 属性
            var attr = typeof(PerfUser).GetCustomAttributes(typeof(TableAttribute), false)
                .FirstOrDefault() as TableAttribute;

            Assert.NotNull(attr);
            Assert.Equal("perf_users", attr.Name);
            Assert.Null(attr.DbTableNames);
        }
    }
}
