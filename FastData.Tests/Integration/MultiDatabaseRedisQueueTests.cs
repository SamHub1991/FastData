#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using FastData.Queue;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;
using Newtonsoft.Json;
using Xunit;

namespace FastData.Tests.Integration
{
    public class MultiDatabaseRedisQueueTests : IDisposable
    {
        private static readonly string[] Databases = { "SqlServer", "MySql", "PostgreSql" };

        static MultiDatabaseRedisQueueTests()
        {
            try { DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance); } catch { }
        }

        public MultiDatabaseRedisQueueTests()
        {
            WriteBehindRegistry.Clear();
            WriteBehindExecutor.Shutdown();
        }

        public void Dispose()
        {
            WriteBehindRegistry.Clear();
            WriteBehindExecutor.Shutdown();
        }

        [Fact]
        public void RedisQueueFlush_SyncsWriteOperationsToMultipleDatabases()
        {
            if (!ShouldRunDbIntegration() || !IsRedisAvailable()) return;

            var topic = "queue_sync_" + Guid.NewGuid().ToString("N");
            var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7, Timeout = 15000 };
            using var mqService = new MessageQueueIntegrationService(redis);
            using var flushService = new QueueFlushService(mqService);

            WriteBehindRegistry.Register("perf_users", new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = topic,
                EnableAutoRecovery = true,
                EnableFallback = true,
                RecoveryIntervalSeconds = 1
            });

            var userNames = new Dictionary<string, string>();
            foreach (var dbName in Databases)
            {
                var userName = $"redis_queue_{dbName}_{Guid.NewGuid():N}".Substring(0, 30);
                userNames[dbName] = userName;
                FastWrite.Delete<PerfUser>(u => u.UserName == userName, key: dbName);

                var operation = new WriteOperation
                {
                    OperationType = WriteOperationType.Add,
                    TableName = "perf_users",
                    EntityType = typeof(PerfUser).AssemblyQualifiedName,
                    DatabaseKey = dbName,
                    Data = JsonConvert.SerializeObject(new PerfUser
                    {
                        UserName = userName,
                        Email = $"{userName}@example.com",
                        Age = 37,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    })
                };

                Assert.True(mqService.PublishSingle(topic, operation, MessageQueueType.ReliableQueue), $"{dbName} publish failed");
            }

            flushService.Start();

            foreach (var kvp in userNames)
            {
                Assert.True(WaitForCount(kvp.Key, kvp.Value, expected: 1), $"{kvp.Key} queued write was not flushed to database");
            }

            foreach (var kvp in userNames)
                FastWrite.Delete<PerfUser>(u => u.UserName == kvp.Value, key: kvp.Key);
        }

        [Fact]
        public void QueueBuilder_WithRedis_ChainsAndWritesAcrossDatabases()
        {
            if (!ShouldRunDbIntegration() || !IsRedisAvailable()) return;

            var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7, Timeout = 15000 };
            var topic = "queue_builder_" + Guid.NewGuid().ToString("N");
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = topic,
                EnableFallback = true,
                EnableAutoRecovery = true
            };

            foreach (var dbName in Databases)
            {
                var userName = $"redis_chain_{dbName}_{Guid.NewGuid():N}".Substring(0, 30);
                FastWrite.Delete<PerfUser>(u => u.UserName == userName, key: dbName);

                var result = FastWrite.QueueBuilder(dbName)
                    .WithRedis(redis)
                    .WithQueue(config)
                    .Add(new PerfUser
                    {
                        UserName = userName,
                        Email = $"{userName}@example.com",
                        Age = 38,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    })
                    .Execute();

                Assert.True(result.Success, $"{dbName} chained Redis queue write failed: {result.Message}");
                Assert.Equal(1, FastRead.Use(dbName).Query<PerfUser>(u => u.UserName == userName).ToCount());

                FastWrite.Delete<PerfUser>(u => u.UserName == userName, key: dbName);
            }
        }

        private static bool WaitForCount(string dbName, string userName, int expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline)
            {
                var count = FastRead.Use(dbName).Query<PerfUser>(u => u.UserName == userName).ToCount();
                if (count == expected)
                    return true;

                Thread.Sleep(250);
            }

            return false;
        }

        private static bool ShouldRunDbIntegration()
        {
            return string.Equals(Environment.GetEnvironmentVariable("FASTDATA_RUN_DB_INTEGRATION"), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRedisAvailable()
        {
            try
            {
                var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7, Timeout = 2000 };
                var key = "fastdata:test:redis:" + Guid.NewGuid().ToString("N");
                redis.Set(key, "ok", 5);
                return string.Equals(redis.Get<string>(key), "ok", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
