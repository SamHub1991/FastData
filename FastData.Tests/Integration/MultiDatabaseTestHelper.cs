using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using FastData.Config;
using FastData.Context;
using Microsoft.Data.Sqlite;
using NewLife.Caching;

namespace FastData.Tests.Integration
{
    internal static class MultiDatabaseTestHelper
    {
        internal static readonly string[] RelationalDatabases = { "SqlServer", "MySql", "PostgreSql" };
        internal static readonly string[] RelationalAndSqliteDatabases = { "SqlServer", "MySql", "PostgreSql", "Sqlite" };

        internal static void RegisterProviders()
        {
            try { DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("Microsoft.Data.Sqlite", Microsoft.Data.Sqlite.SqliteFactory.Instance); } catch { }
        }

        internal static IEnumerable<object[]> GetRelationalDatabases()
        {
            foreach (var db in RelationalDatabases)
                yield return new object[] { db };
        }

        internal static IEnumerable<object[]> GetRelationalAndSqliteDatabases()
        {
            foreach (var db in RelationalAndSqliteDatabases)
                yield return new object[] { db };
        }

        internal static bool ShouldRunDbIntegration()
        {
            return string.Equals(Environment.GetEnvironmentVariable("FASTDATA_RUN_DB_INTEGRATION"), "true", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool CanOpenDatabase(string dbName)
        {
            if (!ShouldRunDbIntegration()) return false;

            try
            {
                EnsureSqlitePerfUsers(dbName);
                using (var db = new DataContext(dbName))
                {
                    db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{dbName} 不可用，跳过测试：{ex.Message}");
                return false;
            }
        }

        internal static bool IsRedisAvailable()
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

        internal static bool WaitForCount(string dbName, string userName, int expected, int timeoutSeconds = 20)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var count = FastRead.Use(dbName).Query<PerfUser>(u => u.UserName == userName).ToCount();
                if (count == expected)
                    return true;

                Thread.Sleep(250);
            }

            return false;
        }

        internal static string NewTag(string prefix, int maxLength = 30)
        {
            var value = $"{prefix}_{Guid.NewGuid():N}";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        internal static PerfUser CreateUser(string userName, int age, bool isActive = true)
        {
            return new PerfUser
            {
                UserName = userName,
                Email = $"{userName}@example.com",
                Age = age,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };
        }

        private static void EnsureSqlitePerfUsers(string dbName)
        {
            if (!string.Equals(dbName, "Sqlite", StringComparison.OrdinalIgnoreCase))
                return;

            using (var conn = new SqliteConnection(FastDataConfig.GetConnectionString(dbName)))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS perf_users (
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
    }
}
