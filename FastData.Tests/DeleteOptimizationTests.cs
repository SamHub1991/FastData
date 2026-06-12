#if (NET8_0_OR_GREATER || NETCOREAPP)
using FastData;
using FastData.Config;
using FastData.Context;
using FastData.DevTools;
using FastData.Base;
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
    /// Delete 操作优化验证测试
    /// </summary>
    public class DeleteOptimizationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public DeleteOptimizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试实体
        /// </summary>
        public class DeleteTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public void Delete_WithExpression_ShouldUseCache()
        {
            const string key = "Sqlite";
            
            // 预热
            var warmupResult = ApplicationWarmup.WarmupType<DeleteTestEntity>(key);
            Assert.True(warmupResult.IsSuccess);

            using (var db = new DataContext(key))
            {
                EnsureDeleteTestTable(db);

                // 首次删除（缓存建立）
                var sw1 = Stopwatch.StartNew();
                var result1 = db.Delete<DeleteTestEntity>(x => x.Id == 999991);
                sw1.Stop();
                _output.WriteLine($"首次删除耗时：{sw1.ElapsedMilliseconds}ms");
                Assert.True(result1.WriteReturn.IsSuccess, result1.WriteReturn.Message);

                // 第二次删除（结构相同）
                var sw2 = Stopwatch.StartNew();
                var result2 = db.Delete<DeleteTestEntity>(x => x.Id == 999992);
                sw2.Stop();
                _output.WriteLine($"第二次删除耗时：{sw2.ElapsedMilliseconds}ms");
                Assert.True(result2.WriteReturn.IsSuccess, result2.WriteReturn.Message);

                // 第三次删除（结构相同）
                var sw3 = Stopwatch.StartNew();
                var result3 = db.Delete<DeleteTestEntity>(x => x.Id == 999993);
                sw3.Stop();
                _output.WriteLine($"第三次删除耗时：{sw3.ElapsedMilliseconds}ms");
                Assert.True(result3.WriteReturn.IsSuccess, result3.WriteReturn.Message);

                Assert.Equal(0, Db.Use(key).Count<DeleteTestEntity>(x => x.Id >= 999991 && x.Id <= 999993));
            }
        }

        [Fact]
        public void Delete_MultipleStructures_ShouldCacheEach()
        {
            const string key = "Sqlite";
            
            // 预热
            var warmupResult = ApplicationWarmup.WarmupType<DeleteTestEntity>(key);
            Assert.True(warmupResult.IsSuccess);

            using (var db = new DataContext(key))
            {
                EnsureDeleteTestTable(db);

                // 不同结构的表达式
                var sw1 = Stopwatch.StartNew();
                var result1 = db.Delete<DeleteTestEntity>(x => x.Id == 1);
                sw1.Stop();
                Assert.True(result1.WriteReturn.IsSuccess, result1.WriteReturn.Message);

                var sw2 = Stopwatch.StartNew();
                var result2 = db.Delete<DeleteTestEntity>(x => x.Name == "test");
                sw2.Stop();
                Assert.True(result2.WriteReturn.IsSuccess, result2.WriteReturn.Message);

                var sw3 = Stopwatch.StartNew();
                var result3 = db.Delete<DeleteTestEntity>(x => x.IsActive == true);
                sw3.Stop();
                Assert.True(result3.WriteReturn.IsSuccess, result3.WriteReturn.Message);

                var sw4 = Stopwatch.StartNew();
                var result4 = db.Delete<DeleteTestEntity>(x => x.Id == 2);
                sw4.Stop();
                Assert.True(result4.WriteReturn.IsSuccess, result4.WriteReturn.Message);

                _output.WriteLine($"Id==1: {sw1.ElapsedMilliseconds}ms");
                _output.WriteLine($"Name==test: {sw2.ElapsedMilliseconds}ms");
                _output.WriteLine($"IsActive==true: {sw3.ElapsedMilliseconds}ms");
                _output.WriteLine($"Id==2 (cached): {sw4.ElapsedMilliseconds}ms");

                Assert.Equal(0, Db.Use(key).Count<DeleteTestEntity>(x => x.Id == 1 || x.Id == 2 || x.Name == "test" || x.IsActive == true));
            }
        }

        [Fact]
        public void Delete_AfterWarmup_ShouldBeConsistent()
        {
            const string key = "Sqlite";
            
            // 预热
            var warmupResult = ApplicationWarmup.WarmupType<DeleteTestEntity>(key);
            Assert.True(warmupResult.IsSuccess);

            using (var db = new DataContext(key))
            {
                EnsureDeleteTestTable(db);

                var times = new List<long>();
                
                // 连续删除 10 次
                for (int i = 0; i < 10; i++)
                {
                    var id = 10000 + i;
                    var sw = Stopwatch.StartNew();
                    var result = db.Delete<DeleteTestEntity>(x => x.Id == id);
                    sw.Stop();
                    Assert.True(result.WriteReturn.IsSuccess, result.WriteReturn.Message);
                    times.Add(sw.ElapsedMilliseconds);
                }

                var avg = times.Average();
                var min = times.Min();
                var max = times.Max();

                _output.WriteLine($"Delete 性能统计：min={min}ms, max={max}ms, avg={avg:F2}ms");

                Assert.Equal(0, Db.Use(key).Count<DeleteTestEntity>(x => x.Id >= 10000 && x.Id < 10010));
            }
        }

        [Fact]
        public void GetPrimaryKeys_ShouldRestoreCommandState()
        {
            const string key = "Sqlite";

            using (var db = new DataContext(key))
            {
                EnsureDeleteTestTable(db);

                var originalCommandText = "select @Id";
                var parameter = db.cmd.CreateParameter();
                parameter.ParameterName = "Id";
                parameter.Value = 123;
                db.cmd.CommandText = originalCommandText;
                db.cmd.Parameters.Clear();
                db.cmd.Parameters.Add(parameter);
                if (db.conn.State == System.Data.ConnectionState.Closed)
                    db.conn.Open();

                var tableName = TableNameHelper.GetTableName<DeleteTestEntity>(db.config);
                BaseModel.GetPrimaryKeys(db.config, db.cmd, tableName);

                Assert.Equal(originalCommandText, db.cmd.CommandText);
                Assert.Single(db.cmd.Parameters);
                Assert.Equal("Id", ((DbParameter)db.cmd.Parameters[0]).ParameterName);
                Assert.Equal(123, ((DbParameter)db.cmd.Parameters[0]).Value);

                BaseModel.GetPrimaryKeys(db.config, db.cmd, tableName);

                Assert.Equal(originalCommandText, db.cmd.CommandText);
                Assert.Single(db.cmd.Parameters);
                Assert.Equal("Id", ((DbParameter)db.cmd.Parameters[0]).ParameterName);
                Assert.Equal(123, ((DbParameter)db.cmd.Parameters[0]).Value);
            }
        }

        public void Dispose()
        {
        }

        private static void EnsureDeleteTestTable(DataContext db)
        {
            if (db.conn.State == System.Data.ConnectionState.Closed)
                db.conn.Open();

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "CREATE TABLE IF NOT EXISTS DeleteTestEntity (Id INTEGER PRIMARY KEY, Name TEXT, IsActive INTEGER)";
            db.cmd.ExecuteNonQuery();
            db.cmd.CommandText = "DELETE FROM DeleteTestEntity";
            db.cmd.ExecuteNonQuery();

            db.cmd.CommandText = "INSERT INTO DeleteTestEntity (Id, Name, IsActive) VALUES (999991, 'cached-1', 1), (999992, 'cached-2', 1), (999993, 'cached-3', 1), (1, 'id-1', 0), (2, 'id-2', 0), (3, 'test', 0), (4, 'active', 1)";
            db.cmd.ExecuteNonQuery();

            for (int i = 0; i < 10; i++)
            {
                db.cmd.CommandText = string.Format("INSERT INTO DeleteTestEntity (Id, Name, IsActive) VALUES ({0}, 'bulk-{1}', 1)", 10000 + i, i);
                db.cmd.ExecuteNonQuery();
            }
        }
    }
}
#endif
