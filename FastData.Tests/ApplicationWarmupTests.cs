#if (NET8_0_OR_GREATER || NETCOREAPP)
using FastData;
using FastData.Config;
using FastData.Context;
using FastData.DevTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace FastData.Tests
{
    /// <summary>
    /// 应用启动预热测试
    /// </summary>
    public class ApplicationWarmupTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public ApplicationWarmupTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试实体模型类
        /// </summary>
        public class WarmupTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// 第二个预热测试实体模型类。
        /// </summary>
        public class WarmupSecondTestEntity
        {
            public int Id { get; set; }
            public string Code { get; set; }
        }

        [Fact]
        public void WarmupType_ShouldCacheProperties_Successfully()
        {
            // 执行预热
            var result = ApplicationWarmup.WarmupType<WarmupTestEntity>("Sqlite");

            // 输出调试信息
            _output.WriteLine($"IsSuccess: {result.IsSuccess}");
            _output.WriteLine($"CachedEntityCount: {result.CachedEntityCount}");
            foreach (var msg in result.Messages)
                _output.WriteLine($"Message: {msg}");

            // 验证结果
            Assert.True(result.IsSuccess, $"预热失败：{string.Join("; ", result.Messages)}");
            Assert.Equal(1, result.CachedEntityCount);
            Assert.Contains(result.Messages, m => m.Contains("PropertyCache"));
            _output.WriteLine($"预热完成：{string.Join("\n", result.Messages)}");
        }

        [Fact]
        public void WarmupMultipleTypes_ShouldCacheAll_Successfully()
        {
            // 批量预热
            var types = new List<Type>
            {
                typeof(WarmupTestEntity),
                typeof(WarmupSecondTestEntity)
            };

            var result = ApplicationWarmup.WarmupTypes(types, "Sqlite");

            // 验证结果
            Assert.True(result.IsSuccess, $"预热失败：{string.Join("; ", result.Messages)}");
            Assert.Equal(2, result.CachedEntityCount);
            _output.WriteLine($"预热完成：耗时 {result.Duration.TotalMilliseconds}ms");
        }

        [Fact]
        public void FirstDelete_AfterWarmup_ShouldCompleteReliably()
        {
            const string key = "Sqlite";
            var config = FastDataConfig.GetConfig(key);
            if (config == null)
            {
                _output.WriteLine("跳过测试：数据库配置不存在");
                return;
            }

            // 预热
            var warmupResult = ApplicationWarmup.WarmupType<WarmupTestEntity>(key);
            Assert.True(warmupResult.IsSuccess);
            _output.WriteLine($"预热完成：{warmupResult.Duration.TotalMilliseconds}ms");

            // 使用 DataContext 进行测试
            using (var db = new DataContext(key))
            {
                EnsureWarmupTestTable(db);

                // 首次删除（已预热）
                var sw1 = Stopwatch.StartNew();
                var result1 = db.Delete<WarmupTestEntity>(x => x.Id == 900001);
                sw1.Stop();

                // 第二次删除（缓存命中）
                var sw2 = Stopwatch.StartNew();
                var result2 = db.Delete<WarmupTestEntity>(x => x.Id == 900002);
                sw2.Stop();

                _output.WriteLine($"首次删除耗时：{sw1.ElapsedMilliseconds}ms");
                _output.WriteLine($"第二次删除耗时：{sw2.ElapsedMilliseconds}ms");

                Assert.True(result1.WriteReturn.IsSuccess, result1.WriteReturn.Message);
                Assert.True(result2.WriteReturn.IsSuccess, result2.WriteReturn.Message);
                Assert.Equal(0, Db.Use(key).Count<WarmupTestEntity>());
            }
        }

        [Fact]
        public void WarmupConfig_ShouldSupportCustomization()
        {
            // 自定义预热配置
            var config = new ApplicationWarmup.WarmupConfig
            {
                EntityTypes = { typeof(WarmupTestEntity) },
                DbKey = "Sqlite",
                WarmupPropertyCache = true,
                WarmupPrimaryKeyCache = false,
                WarmupTableMetadata = false
            };

            var result = ApplicationWarmup.Execute(config);

            Assert.True(result.IsSuccess, $"预热失败：{string.Join("; ", result.Messages)}");
            Assert.Equal(1, result.CachedEntityCount);
            Assert.Equal(0, result.CachedPrimaryKeyCount);
            _output.WriteLine($"自定义预热完成：{string.Join("\n", result.Messages)}");
        }

        [Fact]
        public void WarmupType_WithoutDbKey_ShouldSkipPrimaryKeyWarmup()
        {
            var result = ApplicationWarmup.WarmupType<WarmupTestEntity>();

            Assert.True(result.IsSuccess, $"预热失败：{string.Join("; ", result.Messages)}");
            Assert.Equal(1, result.CachedEntityCount);
            Assert.Equal(0, result.CachedPrimaryKeyCount);
            Assert.Contains(result.Messages, m => m.Contains("跳过主键缓存预热"));
        }

        public void Dispose()
        {
        }

        private static void EnsureWarmupTestTable(DataContext db)
        {
            if (db.conn.State == System.Data.ConnectionState.Closed)
                db.conn.Open();

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "CREATE TABLE IF NOT EXISTS WarmupTestEntity (Id INTEGER PRIMARY KEY, Name TEXT, CreatedAt TEXT)";
            db.cmd.ExecuteNonQuery();
            db.cmd.CommandText = "DELETE FROM WarmupTestEntity";
            db.cmd.ExecuteNonQuery();
            db.cmd.CommandText = "INSERT INTO WarmupTestEntity (Id, Name, CreatedAt) VALUES (900001, 'warmup-1', '2026-01-01'), (900002, 'warmup-2', '2026-01-01')";
            db.cmd.ExecuteNonQuery();
        }
    }
}
#endif
