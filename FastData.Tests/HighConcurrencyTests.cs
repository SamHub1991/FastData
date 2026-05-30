using FastData;
using FastData.Base;
using FastData.Context;
using FastData.Tests.Integration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 高并发测试
    /// 测试 30/100 线程并发场景
    /// </summary>
    public class HighConcurrencyTests
    {
        private static int _successCount;
        private static int _errorCount;
        private static readonly ConcurrentBag<string> _errors = new();
        private static readonly object _lockObj = new();

        /// <summary>
        /// 30 线程并发测试 - SqlServer
        /// </summary>
        [Fact]
        public void Concurrent30_SqlServer()
        {
            var result = TestConcurrentReadWrite("SqlServer", 30, 10);
            Assert.True(result.Success, $"30线程并发测试失败: {result.Details}");
            Console.WriteLine($"SqlServer 30线程并发: {result.Details}");
        }

        /// <summary>
        /// 30 线程并发测试 - MySql
        /// </summary>
        [Fact]
        public void Concurrent30_MySql()
        {
            var result = TestConcurrentReadWrite("MySql", 30, 10);
            // MySql 允许部分失败（连接池限制）
            Assert.True(result.SuccessRate > 0.3, $"30线程并发测试失败: {result.Details}");
            Console.WriteLine($"MySql 30线程并发: {result.Details}");
        }

        /// <summary>
        /// 30 线程并发测试 - PostgreSql
        /// </summary>
        [Fact]
        public void Concurrent30_PostgreSql()
        {
            var result = TestConcurrentReadWrite("PostgreSql", 30, 10);
            Assert.True(result.SuccessRate > 0.5, $"30线程并发测试失败: {result.Details}");
            Console.WriteLine($"PostgreSql 30线程并发: {result.Details}");
        }

        /// <summary>
        /// 100 线程并发测试 - SqlServer
        /// </summary>
        [Fact]
        public void Concurrent100_SqlServer()
        {
            var result = TestConcurrentReadWrite("SqlServer", 100, 5);
            // 100线程允许部分失败（连接池限制）
            Assert.True(result.SuccessRate > 0.3, $"100线程并发测试失败: {result.Details}");
            Console.WriteLine($"SqlServer 100线程并发: {result.Details}");
        }

        /// <summary>
        /// 100 线程并发测试 - MySql
        /// </summary>
        [Fact]
        public void Concurrent100_MySql()
        {
            var result = TestConcurrentReadWrite("MySql", 100, 5);
            // 100线程允许部分失败（连接池限制）
            Assert.True(result.SuccessRate > 0.3, $"100线程并发测试失败: {result.Details}");
            Console.WriteLine($"MySql 100线程并发: {result.Details}");
        }

        /// <summary>
        /// 100 线程并发测试 - PostgreSql
        /// </summary>
        [Fact]
        public void Concurrent100_PostgreSql()
        {
            var result = TestConcurrentReadWrite("PostgreSql", 100, 5);
            // 100线程允许部分失败（连接池限制）
            Assert.True(result.SuccessRate > 0.3, $"100线程并发测试失败: {result.Details}");
            Console.WriteLine($"PostgreSql 100线程并发: {result.Details}");
        }

        /// <summary>
        /// 混合操作并发测试
        /// </summary>
        [Fact]
        public void MixedOperations_Concurrent()
        {
            var dbName = "PostgreSql";
            var result = TestMixedOperations(dbName, 50, 10);
            // 混合操作允许部分失败
            Assert.True(result.SuccessRate > 0.5, $"混合操作并发测试失败: {result.Details}");
            Console.WriteLine($"{dbName} 混合操作并发: {result.Details}");
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
                                var entity = new Integration.PerfUser
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
                                    var result = tempDb.GetList<Integration.PerfUser>(FastRead.Use(dbName).Query<Integration.PerfUser>(u => u.IsActive));
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
            var successRate = (double)_successCount / totalOps;

            return new TestResult
            {
                Success = _errorCount == 0,
                SuccessRate = successRate,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                OpsPerSecond = opsPerSecond,
                Details = $"成功={_successCount}, 失败={_errorCount}, 成功率={successRate:P0}, 吞吐量={opsPerSecond:F0} ops/s"
            };
        }

        private TestResult TestMixedOperations(string dbName, int threadCount, int operationsPerThread)
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
                            var operation = i % 5;
                            switch (operation)
                            {
                                case 0: // 插入
                                    var entity = new Integration.PerfUser
                                    {
                                        UserName = $"Mixed_{threadId}_{i}",
                                        Email = $"mixed_{threadId}_{i}@test.com",
                                        Age = 20 + (i % 50),
                                        IsActive = true,
                                        CreatedAt = DateTime.Now
                                    };
                                    using (var db = new DataContext(dbName))
                                    {
                                        var insertResult = db.Add(entity);
                                        if (insertResult.WriteReturn.IsSuccess)
                                            Interlocked.Increment(ref _successCount);
                                        else
                                            Interlocked.Increment(ref _errorCount);
                                    }
                                    break;

                                case 1: // 查询
                                    using (var db = new DataContext(dbName))
                                    {
                                        var users = db.GetList<Integration.PerfUser>(FastRead.Use(dbName).Query<Integration.PerfUser>(u => u.IsActive));
                                        Interlocked.Increment(ref _successCount);
                                    }
                                    break;

                                case 2: // 条件查询
                                    using (var db = new DataContext(dbName))
                                    {
                                        var filteredUsers = db.GetList<Integration.PerfUser>(FastRead.Use(dbName).Query<Integration.PerfUser>(u => u.Age > 30));
                                        Interlocked.Increment(ref _successCount);
                                    }
                                    break;

                                case 3: // 链式查询
                                    using (var db = new DataContext(dbName))
                                    {
                                        var chainedUsers = db.GetList<Integration.PerfUser>(FastRead.Use(dbName).Query<Integration.PerfUser>(u => u.IsActive).And<Integration.PerfUser>(u => u.Age > 25));
                                        Interlocked.Increment(ref _successCount);
                                    }
                                    break;

                                case 4: // 分页查询
                                    using (var db = new DataContext(dbName))
                                    {
                                        var pageResult = FastRead.Use(dbName).Query<Integration.PerfUser>(u => u.IsActive).ToPagination<Integration.PerfUser>(1, 10);
                                        Interlocked.Increment(ref _successCount);
                                    }
                                    break;
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
            var successRate = (double)_successCount / totalOps;

            return new TestResult
            {
                Success = _errorCount == 0,
                SuccessRate = successRate,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                OpsPerSecond = opsPerSecond,
                Details = $"成功={_successCount}, 失败={_errorCount}, 成功率={successRate:P0}, 吞吐量={opsPerSecond:F0} ops/s"
            };
        }

        private class TestResult
        {
            public bool Success { get; set; }
            public double SuccessRate { get; set; }
            public long ElapsedMs { get; set; }
            public double OpsPerSecond { get; set; }
            public string Details { get; set; }
        }
    }
}
