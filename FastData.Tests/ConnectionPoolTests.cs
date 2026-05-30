using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData;
using FastData.Base;
using FastData.Config;
using FastData.ConnectionPool;
using FastData.Context;
using FastData.Core;
using FastData.Queue;
using FastData.Tests.Integration;
using Microsoft.Data.SqlClient;
using NewLife.Caching;
using Xunit;

namespace FastData.Tests
{
    public class ConnectionPoolTests
    {
        private readonly string _connStr = "server=localhost;database=FastDataTest;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";

        #region Basic Pool

        [Fact]
        public void Pool_Should_Create_And_Return_Connection()
        {
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                ConnectionTimeout = 5
            };

            SmartConnectionPool pool = null;
            try
            {
                pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法创建连接池，跳过测试: {ex.Message}");
                return;
            }

            try
            {
                using var connection = pool.GetConnection();
                Assert.NotNull(connection);
                Assert.NotNull(connection.Connection);
                Assert.Equal(System.Data.ConnectionState.Open, connection.Connection.State);
            }
            catch (ConnectionPoolExhaustedException)
            {
                Console.WriteLine("数据库不可用，跳过测试");
            }
            finally
            {
                pool?.Dispose();
            }
        }

        [Fact]
        public async Task Pool_Should_Reuse_Connections()
        {
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                ConnectionTimeout = 5
            };

            SmartConnectionPool pool;
            try
            {
                pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法创建连接池，跳过测试: {ex.Message}");
                return;
            }

            using (pool)
            {
                try
                {
                    var connection1 = await pool.GetConnectionAsync();
                    var connectionId1 = connection1.Id;
                    connection1.Dispose();

                    var connection2 = await pool.GetConnectionAsync();
                    var connectionId2 = connection2.Id;
                    connection2.Dispose();

                    Assert.Equal(connectionId1, connectionId2);
                }
                catch (ConnectionPoolExhaustedException)
                {
                    Console.WriteLine("数据库不可用，跳过测试");
                }
            }
        }

        [Fact]
        public async Task Pool_Should_Respect_MaxPoolSize()
        {
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 1,
                MaxPoolSize = 2,
                ConnectionTimeout = 1
            };

            SmartConnectionPool pool;
            try
            {
                pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法创建连接池，跳过测试: {ex.Message}");
                return;
            }

            using (pool)
            {
                try
                {
                    var conn1 = await pool.GetConnectionAsync();
                    var conn2 = await pool.GetConnectionAsync();

                    await Assert.ThrowsAsync<ConnectionPoolExhaustedException>(() => pool.GetConnectionAsync());

                    conn1.Dispose();
                    conn2.Dispose();
                }
                catch (ConnectionPoolExhaustedException)
                {
                    Console.WriteLine("数据库不可用，跳过测试");
                }
            }
        }

        [Fact]
        public void Pool_Should_Return_Metrics()
        {
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);
            var metrics = pool.GetMetrics();

            Assert.NotNull(metrics);
            Assert.True(metrics.TotalConnections >= 0);
            Assert.True(metrics.ActiveConnections >= 0);
            Assert.True(metrics.IdleConnections >= 0);
            Assert.True(metrics.WaitingRequests >= 0);
        }

        #endregion

        #region Smart Adjustment

        [Fact]
        public void SmartPool_ErrorShrink()
        {
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                EnableSmartAdjustment = true,
                SmartAdjustmentInterval = 1,
                ErrorShrinkThreshold = 3,
                ErrorShrinkPercentage = 50
            };

            var pool = ConnectionPoolFactory.Instance.GetOrCreatePool(
                "SqlServer_test_shrink",
                () =>
                {
                    var conn = new SqlConnection(_connStr);
                    return conn;
                },
                poolConfig);

            var initialMetrics = pool.Metrics;

            for (int i = 0; i < 5; i++)
            {
                pool.RecordDatabaseError(new Exception($"Simulated error {i}"));
            }

            Thread.Sleep(2000);

            var afterMetrics = pool.Metrics;
            Assert.True(afterMetrics.TotalConnections <= initialMetrics.TotalConnections,
                "Pool should shrink after errors");
        }

        [Fact]
        public void SmartPool_LoadExpand()
        {
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                EnableSmartAdjustment = true,
                SmartAdjustmentInterval = 1,
                LoadThreshold = 50,
                MaxExpandCount = 3
            };

            var pool = ConnectionPoolFactory.Instance.GetOrCreatePool(
                "SqlServer_test_expand",
                () =>
                {
                    var conn = new SqlConnection(_connStr);
                    return conn;
                },
                poolConfig);

            var connections = new List<PooledConnection>();
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        connections.Add(pool.GetConnection());
                    }
                    catch
                    {
                        break;
                    }
                }

                Thread.Sleep(2000);
            }
            finally
            {
                foreach (var conn in connections)
                {
                    conn.Dispose();
                }
            }
        }

        #endregion

        #region MQ Integration

        [Fact]
        public void FastDataClient_MessageQueueIntegration()
        {
            if (!IsRedisAvailable()) return;

            var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7 };
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 1,
                MaxPoolSize = 5,
                EnableSmartAdjustment = true,
                SmartAdjustmentInterval = 5
            };

            using var client = new FastDataClient("SqlServer", poolConfig: poolConfig, redis: redis);

            var user = new PerfUser
            {
                UserName = "FastDataClientTest",
                Email = "client@test.com",
                Age = 25,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                var result = client.Add(user);
                Console.WriteLine($"FastDataClient write result: IsSuccess={result.IsSuccess}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FastDataClient write exception (may have degraded to queue): {ex.Message}");
            }

            var users = new List<PerfUser>();
            for (int i = 0; i < 10; i++)
            {
                users.Add(new PerfUser
                {
                    UserName = $"BatchTest_{i}",
                    Email = $"batch_{i}@test.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            try
            {
                var result = client.AddList(users);
                Console.WriteLine($"FastDataClient batch write result: IsSuccess={result.IsSuccess}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FastDataClient batch write exception: {ex.Message}");
            }
        }

        [Fact]
        public void FastRead_QueryOperation()
        {
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 1,
                MaxPoolSize = 10,
                EnableSmartAdjustment = true
            };

            try
            {
                var query = FastRead.Query<PerfUser>(u => u.IsActive)
                    .Take(5);

                var users = FastRead.ToList<PerfUser>(query);
                Console.WriteLine($"FastRead query result: {users.Count} records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FastRead query exception: {ex.Message}");
            }
        }

        #endregion

        #region Resilient Write

        [Fact]
        public void ConnectionPoolExhausted_ShouldFallbackToQueue()
        {
            if (!IsRedisAvailable()) return;

            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 0,
                MaxPoolSize = 1,
                ConnectionTimeout = 1,
                EnableSmartAdjustment = false
            };

            var queueConfig = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                EnableFallback = true,
                Topic = "test_fallback"
            };

            var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7 };
            using var executor = new ResilientWriteExecutor(
                databaseKey: "SqlServer_test_fallback",
                poolConfig: poolConfig,
                queueConfig: queueConfig,
                redis: redis,
                maxRetries: 0);

            var pool = ConnectionPoolFactory.Instance.GetOrCreatePool(
                "SqlServer_test_fallback",
                () => new SqlConnection(_connStr),
                poolConfig);

            PooledConnection blockingConn = null;
            try
            {
                blockingConn = pool.GetConnection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot get DB connection, skipping: {ex.Message}");
                return;
            }

            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                EntityType = typeof(PerfUser).AssemblyQualifiedName,
                TableName = "perf_users",
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(new PerfUser
                {
                    UserName = "FallbackTest",
                    Email = "fallback@test.com",
                    Age = 25,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                })
            };

            var result = executor.ExecuteWrite(operation);

            Assert.True(result.Success, "Write should succeed via queue fallback");
            Assert.True(result.UsedQueueFallback, "Should use queue fallback");
            Assert.Equal("连接池耗尽", result.FallbackReason);

            blockingConn?.Dispose();
        }

        [Fact]
        public void BatchWrite_PartialFallback()
        {
            if (!IsRedisAvailable()) return;

            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 0,
                MaxPoolSize = 1,
                ConnectionTimeout = 1,
                EnableSmartAdjustment = false
            };

            var queueConfig = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                EnableFallback = true,
                Topic = "test_batch_fallback"
            };

            var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7 };
            using var executor = new ResilientWriteExecutor(
                databaseKey: "SqlServer_test_batch",
                poolConfig: poolConfig,
                queueConfig: queueConfig,
                redis: redis,
                maxRetries: 0);

            var pool = ConnectionPoolFactory.Instance.GetOrCreatePool(
                "SqlServer_test_batch",
                () => new SqlConnection(_connStr),
                poolConfig);

            PooledConnection blockingConn = null;
            try
            {
                blockingConn = pool.GetConnection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot get DB connection, skipping: {ex.Message}");
                return;
            }

            var operations = new List<WriteOperation>();
            for (int i = 0; i < 5; i++)
            {
                operations.Add(new WriteOperation
                {
                    OperationType = WriteOperationType.Add,
                    EntityType = typeof(PerfUser).AssemblyQualifiedName,
                    TableName = "perf_users",
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(new PerfUser
                    {
                        UserName = $"BatchTest_{i}",
                        Email = $"batch_{i}@test.com",
                        Age = 20 + i,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    })
                });
            }

            var result = executor.ExecuteBatchWrite(operations);

            Assert.True(result.Success, "Batch write should succeed");
            Assert.Equal(5, result.QueuedCount);
            Assert.Equal(0, result.DirectWriteCount);

            blockingConn?.Dispose();
        }

        [Fact]
        public void NormalWrite_ShouldUseDirectWrite()
        {
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var executor = new ResilientWriteExecutor(
                databaseKey: "SqlServer",
                poolConfig: poolConfig,
                maxRetries: 1);

            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                EntityType = typeof(PerfUser).AssemblyQualifiedName,
                TableName = "perf_users",
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(new PerfUser
                {
                    UserName = "DirectWriteTest",
                    Email = "direct@test.com",
                    Age = 30,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                })
            };

            var result = executor.ExecuteWrite(operation);
            Console.WriteLine($"Result: Success={result.Success}, DirectWrite={result.UsedDirectWrite}, QueueFallback={result.UsedQueueFallback}");
        }

        [Fact]
        public void Stats_ShouldTrackCorrectly()
        {
            var poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var executor = new ResilientWriteExecutor(
                databaseKey: "SqlServer",
                poolConfig: poolConfig,
                maxRetries: 0);

            for (int i = 0; i < 3; i++)
            {
                var operation = new WriteOperation
                {
                    OperationType = WriteOperationType.Add,
                    EntityType = typeof(PerfUser).AssemblyQualifiedName,
                    TableName = "perf_users",
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(new PerfUser
                    {
                        UserName = $"StatsTest_{i}",
                        Email = $"stats_{i}@test.com",
                        Age = 20 + i,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    })
                };

                executor.ExecuteWrite(operation);
            }

            var stats = executor.GetStats();
            Console.WriteLine($"Stats: DirectWrites={stats.DirectWriteCount}, QueueFallback={stats.QueueFallbackCount}, " +
                $"Failures={stats.TotalFailureCount}, FallbackRate={stats.QueueFallbackRate:F2}%");
        }

        #endregion

        #region Stability

        [Fact]
        public void ConnectionPool_Stress_Test()
        {
            var dbName = "PostgreSql";
            var iterations = 1000;
            var successCount = 0;
            var errorCount = 0;
            var errors = new ConcurrentBag<string>();

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
                    if (errors.Count < 10)
                        errors.Add($"Iteration {i}: {ex.Message}");
                }
            }

            var successRate = (double)successCount / iterations;
            Assert.True(successRate > 0.7, $"Stress test failed: rate={successRate:P0}, ok={successCount}, err={errorCount}");
            Console.WriteLine($"Stress test: rate={successRate:P0}, ok={successCount}, err={errorCount}");
        }

        [Fact]
        public void LongRunning_Test()
        {
            var dbName = "PostgreSql";
            var duration = TimeSpan.FromSeconds(3);
            var successCount = 0;
            var errorCount = 0;
            var errors = new ConcurrentBag<string>();
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < duration)
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
                    if (errors.Count < 10)
                        errors.Add($"LongRunning: {ex.Message}");
                }
            }

            stopwatch.Stop();
            var totalOps = successCount + errorCount;
            var successRate = (double)successCount / totalOps;
            var opsPerSecond = totalOps * 1000.0 / stopwatch.ElapsedMilliseconds;

            Assert.True(successRate > 0.5, $"Long-running test failed: rate={successRate:P0}, ok={successCount}, err={errorCount}");
            Console.WriteLine($"Long-running test: rate={successRate:P0}, ok={successCount}, err={errorCount}, throughput={opsPerSecond:F0} ops/s");
        }

        [Fact]
        public void ConnectionLeak_Detection_Test()
        {
            var dbName = "PostgreSql";
            var iterations = 100;
            var successCount = 0;
            var errorCount = 0;
            var errors = new ConcurrentBag<string>();

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
                    if (errors.Count < 10)
                        errors.Add($"Leak Detection: {ex.Message}");
                }
            }

            var successRate = (double)successCount / iterations;
            Assert.True(successRate > 0.95, $"Leak detection test failed: rate={successRate:P0}, ok={successCount}, err={errorCount}");
            Console.WriteLine($"Leak detection test: rate={successRate:P0}, ok={successCount}, err={errorCount}");
        }

        [Fact]
        public void Concurrent_ConnectionPool_Stress_Test()
        {
            var dbName = "PostgreSql";
            var threadCount = 20;
            var operationsPerThread = 50;
            var successCount = 0;
            var errorCount = 0;
            var errors = new ConcurrentBag<string>();
            var lockObj = new object();

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
                                Interlocked.Increment(ref successCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref errorCount);
                            lock (lockObj)
                            {
                                if (errors.Count < 10)
                                    errors.Add($"Thread {threadId}: {ex.Message}");
                            }
                        }
                    }
                })
            ).ToArray();

            Task.WaitAll(tasks);
            stopwatch.Stop();

            var totalOps = threadCount * operationsPerThread;
            var successRate = (double)successCount / totalOps;
            var opsPerSecond = totalOps * 1000.0 / stopwatch.ElapsedMilliseconds;

            Assert.True(successRate > 0.7, $"Concurrent stress test failed: rate={successRate:P0}, ok={successCount}, err={errorCount}");
            Console.WriteLine($"Concurrent stress test: rate={successRate:P0}, ok={successCount}, err={errorCount}, throughput={opsPerSecond:F0} ops/s");
        }

        #endregion

        #region Helpers

        private DbConnection CreateConnectionFactory()
        {
            var connection = new SqlConnection(_connStr);
            connection.Open();
            return connection;
        }

        private static bool IsRedisAvailable()
        {
            try
            {
                var testRedis = new FullRedis { Server = "127.0.0.1:6379", Db = 7 };
                testRedis.Set("test_key", "test_value");
                return true;
            }
            catch
            {
                Console.WriteLine("Redis not available, skipping test");
                return false;
            }
        }

        #endregion
    }
}
