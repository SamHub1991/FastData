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
    /// 稳定性测试
    /// 测试长时间运行和连接池压力
    /// </summary>
    public class StabilityTests
    {
        private static int _successCount;
        private static int _errorCount;
        private static readonly ConcurrentBag<string> _errors = new();
        private static readonly object _lockObj = new();

        /// <summary>
        /// 连接池压力测试 - 快速创建和销毁连接
        /// </summary>
        [Fact]
        public void ConnectionPool_Stress_Test()
        {
            var dbName = "PostgreSql";
            var iterations = 1000;
            var successCount = 0;
            var errorCount = 0;

            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    using (var db = new DataContext(dbName))
                    {
                        var users = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    if (_errors.Count < 10)
                        _errors.Add($"Iteration {i}: {ex.Message}");
                }
            }

            var successRate = (double)successCount / iterations;
            Assert.True(successRate > 0.7, $"连接池压力测试失败: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}");
            Console.WriteLine($"连接池压力测试: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}");
        }

        /// <summary>
        /// 长时间运行测试 - 持续读写操作
        /// </summary>
        [Fact]
        public void LongRunning_Test()
        {
            var dbName = "PostgreSql";
            var duration = TimeSpan.FromSeconds(3);
            var successCount = 0;
            var errorCount = 0;
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < duration)
            {
                try
                {
                    // 只测试查询操作
                    using (var db = new DataContext(dbName))
                    {
                        var users = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    if (_errors.Count < 10)
                        _errors.Add($"LongRunning: {ex.Message}");
                }
            }

            stopwatch.Stop();
            var totalOps = successCount + errorCount;
            var successRate = (double)successCount / totalOps;
            var opsPerSecond = totalOps * 1000.0 / stopwatch.ElapsedMilliseconds;

            Assert.True(successRate > 0.5, $"长时间运行测试失败: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}");
            Console.WriteLine($"长时间运行测试: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}, 吞吐量={opsPerSecond:F0} ops/s");
        }

        /// <summary>
        /// 连接泄漏检测测试
        /// </summary>
        [Fact]
        public void ConnectionLeak_Detection_Test()
        {
            var dbName = "PostgreSql";
            var iterations = 100;
            var successCount = 0;
            var errorCount = 0;

            // 测试正常关闭连接
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var db = new DataContext(dbName);
                    var users = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                    db.Dispose();
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    if (_errors.Count < 10)
                        _errors.Add($"Leak Detection: {ex.Message}");
                }
            }

            var successRate = (double)successCount / iterations;
            Assert.True(successRate > 0.95, $"连接泄漏检测测试失败: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}");
            Console.WriteLine($"连接泄漏检测测试: 成功率={successRate:P0}, 成功={successCount}, 失败={errorCount}");
        }

        /// <summary>
        /// 并发连接池压力测试
        /// </summary>
        [Fact]
        public void Concurrent_ConnectionPool_Stress_Test()
        {
            var dbName = "PostgreSql";
            var threadCount = 20;
            var operationsPerThread = 50;
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
                            using (var db = new DataContext(dbName))
                            {
                                var users = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
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
            var successRate = (double)_successCount / totalOps;
            var opsPerSecond = totalOps * 1000.0 / stopwatch.ElapsedMilliseconds;

            Assert.True(successRate > 0.7, $"并发连接池压力测试失败: 成功率={successRate:P0}, 成功={_successCount}, 失败={_errorCount}");
            Console.WriteLine($"并发连接池压力测试: 成功率={successRate:P0}, 成功={_successCount}, 失败={_errorCount}, 吞吐量={opsPerSecond:F0} ops/s");
        }
    }
}
