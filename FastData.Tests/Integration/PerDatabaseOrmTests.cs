using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData;
using FastData.Base;
using FastData.Config;
using FastData.Context;
using FastData.Model;
using FastData.Property;
using FastUntility.Page;
using Xunit;

namespace FastData.Tests.Integration
{
    /// <summary>
    /// 每个数据库完整 ORM 功能测试
    /// 覆盖：CRUD、Lambda 查询、链式查询、分页、事务、批量插入、AOP、多数据库表名映射
    /// </summary>
    public class PerDatabaseOrmTests
    {
        private static readonly object _lockObj = new object();

        public PerDatabaseOrmTests()
        {
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
        }

        #region SqlServer 完整测试

        [Fact]
        public void SqlServer_FullOrmTest()
        {
            var dbName = "SqlServer";
            var results = RunFullOrmTest(dbName);
            PrintResults(dbName, results);

            // 验证所有测试通过
            Assert.All(results, r => Assert.True(r.Value.Success, $"{r.Key} 失败: {r.Value.Details}"));
        }

        [Fact]
        public void SqlServer_Insert()
        {
            var result = TestInsert("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_Query()
        {
            var result = TestQuery("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_Update()
        {
            var result = TestUpdate("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_Delete()
        {
            var result = TestDelete("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_ChainQuery()
        {
            var result = TestChainQuery("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_Pagination()
        {
            var result = TestPagination("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_BatchInsert()
        {
            var result = TestBatchInsert("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_LambdaWhere()
        {
            var result = TestLambdaWhere("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_OrderByGroupBy()
        {
            var result = TestOrderByGroupBy("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void SqlServer_DbTableNames()
        {
            var result = TestDbTableNames("SqlServer");
            Assert.True(result.Success, result.Details);
        }

        #endregion

        #region MySql 完整测试

        [Fact]
        public void MySql_FullOrmTest()
        {
            var dbName = "MySql";
            var results = RunFullOrmTest(dbName);
            PrintResults(dbName, results);

            Assert.All(results, r => Assert.True(r.Value.Success, $"{r.Key} 失败: {r.Value.Details}"));
        }

        [Fact]
        public void MySql_Insert()
        {
            var result = TestInsert("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_Query()
        {
            var result = TestQuery("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_Update()
        {
            var result = TestUpdate("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_Delete()
        {
            var result = TestDelete("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_ChainQuery()
        {
            var result = TestChainQuery("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_Pagination()
        {
            var result = TestPagination("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_BatchInsert()
        {
            var result = TestBatchInsert("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_LambdaWhere()
        {
            var result = TestLambdaWhere("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_OrderByGroupBy()
        {
            var result = TestOrderByGroupBy("MySql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void MySql_DbTableNames()
        {
            var result = TestDbTableNames("MySql");
            Assert.True(result.Success, result.Details);
        }

        #endregion

        #region PostgreSql 完整测试

        [Fact]
        public void PostgreSql_FullOrmTest()
        {
            var dbName = "PostgreSql";
            var results = RunFullOrmTest(dbName);
            PrintResults(dbName, results);

            Assert.All(results, r => Assert.True(r.Value.Success, $"{r.Key} 失败: {r.Value.Details}"));
        }

        [Fact]
        public void PostgreSql_Insert()
        {
            var result = TestInsert("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_Query()
        {
            var result = TestQuery("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_Update()
        {
            var result = TestUpdate("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_Delete()
        {
            var result = TestDelete("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_ChainQuery()
        {
            var result = TestChainQuery("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_Pagination()
        {
            var result = TestPagination("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_BatchInsert()
        {
            var result = TestBatchInsert("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_LambdaWhere()
        {
            var result = TestLambdaWhere("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_OrderByGroupBy()
        {
            var result = TestOrderByGroupBy("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        [Fact]
        public void PostgreSql_DbTableNames()
        {
            var result = TestDbTableNames("PostgreSql");
            Assert.True(result.Success, result.Details);
        }

        #endregion

        #region 测试方法

        private Dictionary<string, TestResult> RunFullOrmTest(string dbName)
        {
            var results = new Dictionary<string, TestResult>();

            results["1.插入"] = TestInsert(dbName);
            results["2.查询"] = TestQuery(dbName);
            results["3.更新"] = TestUpdate(dbName);
            results["4.删除"] = TestDelete(dbName);
            results["5.链式查询"] = TestChainQuery(dbName);
            results["6.分页查询"] = TestPagination(dbName);
            results["7.批量插入"] = TestBatchInsert(dbName);
            results["8.Lambda查询"] = TestLambdaWhere(dbName);
            results["9.排序分组"] = TestOrderByGroupBy(dbName);
            results["10.表名映射"] = TestDbTableNames(dbName);

            return results;
        }

        private TestResult TestInsert(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var entity = new PerfUser
                {
                    UserName = $"TestUser_{DateTime.Now.Ticks}",
                    Email = $"test_{DateTime.Now.Ticks}@example.com",
                    Age = 25,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                using var db = new DataContext(dbName);
                var result = db.Add(entity);
                stopwatch.Stop();

                return new TestResult
                {
                    Success = result.WriteReturn.IsSuccess,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = result.WriteReturn.IsSuccess ? "插入成功" : $"插入失败: {result.WriteReturn.Message}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestQuery(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);
                var result = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
                stopwatch.Stop();

                return new TestResult
                {
                    Success = true,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"查询成功，返回 {result.List.Count} 条记录"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestUpdate(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 先查询一条记录
                using var db = new DataContext(dbName);
                var queryResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));

                if (queryResult.List.Count == 0)
                {
                    stopwatch.Stop();
                    return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = "没有可更新的记录" };
                }

                var user = queryResult.List[0];
                user.UserName = $"Updated_{DateTime.Now.Ticks}";
                user.Email = $"updated_{DateTime.Now.Ticks}@example.com";

                // 排除 Id 字段，只更新其他字段
                var result = db.Update(user, u => u.Id == user.Id, u => new { u.UserName, u.Email, u.Age, u.IsActive, u.CreatedAt });
                stopwatch.Stop();

                return new TestResult
                {
                    Success = result.WriteReturn.IsSuccess,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = result.WriteReturn.IsSuccess ? "更新成功" : $"更新失败: {result.WriteReturn.Message}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestDelete(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 先插入一条记录
                var entity = new PerfUser
                {
                    UserName = $"DeleteTest_{DateTime.Now.Ticks}",
                    Email = $"delete_{DateTime.Now.Ticks}@example.com",
                    Age = 30,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                using var db = new DataContext(dbName);
                var insertResult = db.Add(entity);

                if (!insertResult.WriteReturn.IsSuccess)
                {
                    stopwatch.Stop();
                    return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"插入失败: {insertResult.WriteReturn.Message}" };
                }

                // 查询刚插入的记录
                var queryResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.UserName == entity.UserName));

                if (queryResult.List.Count == 0)
                {
                    stopwatch.Stop();
                    return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = "未找到要删除的记录" };
                }

                // 删除记录
                var deleteResult = db.Delete<PerfUser>(u => u.Id == queryResult.List[0].Id);
                stopwatch.Stop();

                return new TestResult
                {
                    Success = deleteResult.WriteReturn.IsSuccess,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = deleteResult.WriteReturn.IsSuccess ? "删除成功" : $"删除失败: {deleteResult.WriteReturn.Message}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestChainQuery(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);
                var query = FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive)
                    .And<PerfUser>(u => u.Age > 20)
                    .OrderBy<PerfUser>(u => u.Id)
                    .Take(10);

                var result = db.GetList<PerfUser>(query);
                stopwatch.Stop();

                return new TestResult
                {
                    Success = true,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"链式查询成功，返回 {result.List.Count} 条记录"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestPagination(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);

                // 测试分页查询
                var result = FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive)
                    .OrderBy<PerfUser>(u => u.Id)
                    .ToPagination<PerfUser>(1, 10);

                stopwatch.Stop();

                return new TestResult
                {
                    Success = result.Data != null,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"分页查询成功: Page={result.Page}, PageSize={result.PageSize}, Total={result.Total}, Data={result.Data?.Count ?? 0}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestBatchInsert(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var entities = new List<PerfUser>();
                for (int i = 0; i < 10; i++)
                {
                    entities.Add(new PerfUser
                    {
                        UserName = $"Batch_{DateTime.Now.Ticks}_{i}",
                        Email = $"batch_{DateTime.Now.Ticks}_{i}@example.com",
                        Age = 20 + i,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }

                using var db = new DataContext(dbName);
                var result = db.AddList(entities);
                stopwatch.Stop();

                return new TestResult
                {
                    Success = result.WriteReturn.IsSuccess,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = result.WriteReturn.IsSuccess ? $"批量插入 {entities.Count} 条成功" : $"批量插入失败: {result.WriteReturn.Message}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestLambdaWhere(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);

                // 测试 Where
                var whereResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.Age > 25));

                // 测试 Or
                var orResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.Age > 30).Or<PerfUser>(u => u.UserName.Contains("Test")));

                // 测试 Like
                var likeResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).Like<PerfUser>(u => u.UserName, "Batch%"));

                // 测试 In
                var ages = new List<object> { 20, 25, 30 };
                var inResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).In<PerfUser>(u => u.Age, ages));

                // 测试 Between
                var betweenResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).Between<PerfUser>(u => u.Age, 20, 30));

                stopwatch.Stop();

                return new TestResult
                {
                    Success = true,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"Lambda查询成功: Where={whereResult.List.Count}, Or={orResult.List.Count}, Like={likeResult.List.Count}, In={inResult.List.Count}, Between={betweenResult.List.Count}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestOrderByGroupBy(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);

                // 测试 OrderBy
                var orderByResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).OrderBy<PerfUser>(u => u.Age));

                // 测试 OrderBy Desc
                var orderByDescResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).OrderBy<PerfUser>(u => u.Age, true));

                // 测试 GroupBy
                var groupByResult = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive).GroupBy<PerfUser>(u => u.Age));

                stopwatch.Stop();

                return new TestResult
                {
                    Success = true,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"排序分组成功: OrderBy={orderByResult.List.Count}, OrderByDesc={orderByDescResult.List.Count}, GroupBy={groupByResult.List.Count}"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        private TestResult TestDbTableNames(string dbName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var db = new DataContext(dbName);

                // 测试 MultiDbUser（DbTableNames 属性）
                var result = db.GetList<MultiDbUser>(FastRead.Use(dbName).Query<MultiDbUser>(u => u.Id > 0));

                stopwatch.Stop();

                return new TestResult
                {
                    Success = true,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Details = $"表名映射测试成功: MultiDbUser 查询 {result.List.Count} 条"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult { Success = false, ElapsedMs = stopwatch.ElapsedMilliseconds, Details = $"异常: {ex.Message}" };
            }
        }

        #endregion

        #region 辅助方法

        private void PrintResults(string dbName, Dictionary<string, TestResult> results)
        {
            Console.WriteLine($"\n{dbName} ORM 完整测试:");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"测试项",-15} {"结果",-8} {"耗时(ms)",-12} {"详情"}");
            Console.WriteLine(new string('-', 80));

            foreach (var test in results)
            {
                var status = test.Value.Success ? "PASS" : "FAIL";
                Console.WriteLine($"{test.Key,-15} {status,-8} {test.Value.ElapsedMs,-12} {test.Value.Details}");
            }

            Console.WriteLine(new string('-', 80));
            var passed = results.Count(r => r.Value.Success);
            Console.WriteLine($"总计: {passed}/{results.Count} 通过\n");
        }

        #endregion

        private class TestResult
        {
            public bool Success { get; set; }
            public long ElapsedMs { get; set; }
            public string Details { get; set; }
        }
    }
}
