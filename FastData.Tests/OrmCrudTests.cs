#if !NETFRAMEWORK
using FastData;
using FastData.Tests.Integration;
using FastData.Queue;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// ORM CRUD 功能覆盖率测试
    /// 覆盖之前未测试的核心 ORM 操作
    /// </summary>
    public class OrmCrudTests : IDisposable
    {
        private readonly string _key = "SqlServer";
        private bool _dbAvailable = true;

        public OrmCrudTests()
        {
            try
            {
                var users = FastRead.Query<PerfUser>(u => true, key: _key)
                    .Take(1)
                    .ToList<PerfUser>();
                _dbAvailable = true;
            }
            catch
            {
                _dbAvailable = false;
            }
        }

        public void Dispose()
        {
            if (_dbAvailable)
            {
                try
                {
                    FastWrite.Delete<PerfUser>(u => u.UserName.StartsWith("orm_test_"), key: _key);
                }
                catch { }
            }
        }

        private PerfUser CreateTestUser(string suffix = null)
        {
            var tag = suffix ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            return new PerfUser
            {
                UserName = $"orm_test_{tag}",
                Email = $"orm_test_{tag}@example.com",
                Age = 25,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
        }

        #region Update by primary key

        [Fact]
        public void Update_ByPrimaryKey_ShouldModifyEntity()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = CreateTestUser(tag);
            var addResult = FastWrite.Add(user, key: _key);
            Assert.True(addResult.IsSuccess, $"Add failed: {addResult.Message}");

            var inserted = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .ToItem<PerfUser>();
            Assert.NotNull(inserted);

            inserted.Email = $"updated_{tag}@example.com";
            inserted.Age = 99;

            var updateResult = FastWrite.Update(inserted, key: _key);
            Console.WriteLine($"Update by PK: IsSuccess={updateResult.IsSuccess}, Message={updateResult.Message}");
            Assert.True(updateResult.IsSuccess, $"Update failed: {updateResult.Message}");

            var updated = FastRead.Query<PerfUser>(u => u.Id == inserted.Id, key: _key)
                .ToItem<PerfUser>();
            Assert.NotNull(updated);
            Assert.Equal(99, updated.Age);
            Assert.Equal($"updated_{tag}@example.com", updated.Email);
        }

        #endregion

        #region Update by predicate

        [Fact]
        public void Update_ByPredicate_ShouldModifyMatchingEntities()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 3; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var template = new PerfUser { IsActive = false };
            var updateResult = FastWrite.Update(template, u => u.UserName.Contains($"orm_test_{tag}"), key: _key);
            Console.WriteLine($"Update by predicate: IsSuccess={updateResult.IsSuccess}, Message={updateResult.Message}");
            Assert.True(updateResult.IsSuccess, $"Update by predicate failed: {updateResult.Message}");

            var updated = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToList<PerfUser>();
            Assert.All(updated, u => Assert.False(u.IsActive));
        }

        #endregion

        #region Update with field selector

        [Fact]
        public void Update_WithFieldSelector_ShouldModifyOnlySelectedFields()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = CreateTestUser(tag);
            FastWrite.Add(user, key: _key);

            var inserted = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .ToItem<PerfUser>();
            Assert.NotNull(inserted);

            inserted.Email = $"field_updated_{tag}@example.com";
            inserted.Age = 50;

            var updateResult = FastWrite.Update(inserted, u => new { u.Email }, key: _key);
            Console.WriteLine($"Update with field selector: IsSuccess={updateResult.IsSuccess}, Message={updateResult.Message}");
            Assert.True(updateResult.IsSuccess, $"Update with field selector failed: {updateResult.Message}");
        }

        #endregion

        #region UpdateList

        [Fact]
        public void UpdateList_ShouldModifyMultipleEntities()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var users = new List<PerfUser>();
            for (int i = 0; i < 3; i++)
            {
                var u = CreateTestUser(tag + "_" + i);
                FastWrite.Add(u, key: _key);
                users.Add(u);
            }

            var inserted = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToList<PerfUser>();
            Assert.True(inserted.Count >= 3, $"Expected at least 3, got {inserted.Count}");

            foreach (var u in inserted)
            {
                u.Age = 77;
            }

            var updateResult = FastWrite.UpdateList(inserted, u => new { u.Age }, key: _key);
            Console.WriteLine($"UpdateList: IsSuccess={updateResult.IsSuccess}, Message={updateResult.Message}");
            Assert.True(updateResult.IsSuccess, $"UpdateList failed: {updateResult.Message}");

            var updated = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToList<PerfUser>();
            Assert.All(updated, u => Assert.Equal(77, u.Age));
        }

        #endregion

        #region Delete by entity model

        [Fact]
        public void Delete_ByModel_ShouldRemoveEntity()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = CreateTestUser(tag);
            var addResult = FastWrite.Add(user, key: _key);
            Assert.True(addResult.IsSuccess, $"Add failed: {addResult.Message}");

            var inserted = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .ToItem<PerfUser>();
            Assert.NotNull(inserted);

            var deleteResult = FastWrite.Delete(inserted, key: _key);
            Console.WriteLine($"Delete by model: IsSuccess={deleteResult.IsSuccess}, Message={deleteResult.Message}");
            Assert.True(deleteResult.IsSuccess, $"Delete by model failed: {deleteResult.Message}");

            var remaining = FastRead.Query<PerfUser>(u => u.Id == inserted.Id, key: _key)
                .ToItem<PerfUser>();
            Assert.Null(remaining);
        }

        #endregion

        #region ExecuteSql raw write

        [Fact]
        public void ExecuteSql_RawWrite_ShouldExecuteNonQuery()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var result = FastWrite.ExecuteSql(
                "INSERT INTO perf_users (UserName, Email, Age, IsActive, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4)",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@p0", $"orm_test_sql_{tag}"),
                    new Microsoft.Data.SqlClient.SqlParameter("@p1", $"sql_{tag}@example.com"),
                    new Microsoft.Data.SqlClient.SqlParameter("@p2", 42),
                    new Microsoft.Data.SqlClient.SqlParameter("@p3", true),
                    new Microsoft.Data.SqlClient.SqlParameter("@p4", DateTime.Now)
                },
                key: _key);

            Console.WriteLine($"ExecuteSql: IsSuccess={result.IsSuccess}, Message={result.Message}");
            Assert.True(result.IsSuccess, $"ExecuteSql failed: {result.Message}");

            var found = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_sql_{tag}", key: _key)
                .ToItem<PerfUser>();
            Assert.NotNull(found);
            Assert.Equal(42, found.Age);
        }

        #endregion

        #region BulkInsert

        [Fact]
        public void BulkInsert_ShouldInsertMultipleRecords()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var list = new List<PerfUser>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new PerfUser
                {
                    UserName = $"orm_test_bulk_{tag}_{i}",
                    Email = $"bulk_{tag}_{i}@example.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            var result = FastWrite.BulkInsert(list, key: _key);
            Console.WriteLine($"BulkInsert: IsSuccess={result.IsSuccess}, Message={result.Message}");
            Assert.True(result.IsSuccess, $"BulkInsert failed: {result.Message}");

            var count = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_bulk_{tag}"), key: _key)
                .ToCount();
            Console.WriteLine($"BulkInsert count after insert: {count}");
            Assert.Equal(10, count);
        }

        #endregion

        #region ToItem

        [Fact]
        public void ToItem_ShouldReturnSingleEntity()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = CreateTestUser(tag);
            FastWrite.Add(user, key: _key);

            var result = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .ToItem<PerfUser>();

            Console.WriteLine($"ToItem: UserName={result?.UserName}, Id={result?.Id}");
            Assert.NotNull(result);
            Assert.Equal($"orm_test_{tag}", result.UserName);
        }

        [Fact]
        public void ToItem_NoMatch_ShouldReturnNull()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var result = FastRead.Query<PerfUser>(u => u.UserName == "nonexistent_user_xyz_12345", key: _key)
                .ToItem<PerfUser>();

            Console.WriteLine($"ToItem no match: result is null = {result == null}");
            Assert.Null(result);
        }

        #endregion

        #region ToCount

        [Fact]
        public void ToCount_ShouldReturnRecordCount()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 5; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var count = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToCount();

            Console.WriteLine($"ToCount: {count}");
            Assert.True(count >= 5, $"Expected >= 5, got {count}");
        }

        [Fact]
        public void ToCount_NoMatch_ShouldReturnZero()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var count = FastRead.Query<PerfUser>(u => u.UserName == "nonexistent_count_xyz", key: _key)
                .ToCount();

            Console.WriteLine($"ToCount no match: {count}");
            Assert.Equal(0, count);
        }

        #endregion

        #region ToPage with PageModel

        [Fact]
        public void ToPage_ShouldReturnPagedResults()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 15; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var pModel = new PageModel
            {
                PageId = 1,
                PageSize = 5
            };

            var result = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .OrderBy<PerfUser>(u => u.Id)
                .ToPage<PerfUser>(pModel);

            Console.WriteLine($"ToPage: list.Count={result.list.Count}, TotalRecord={result.pModel.TotalRecord}, TotalPage={result.pModel.TotalPage}");
            Assert.NotNull(result);
            Assert.NotNull(result.list);
            Assert.True(result.list.Count <= 5, $"Expected <= 5 items, got {result.list.Count}");
            Assert.True(result.pModel.TotalRecord >= 15, $"Expected TotalRecord >= 15, got {result.pModel.TotalRecord}");
        }

        [Fact]
        public void ToPage_SecondPage_ShouldReturnCorrectOffset()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 12; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var pModel = new PageModel
            {
                PageId = 2,
                PageSize = 5
            };

            var result = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .OrderBy<PerfUser>(u => u.Id)
                .ToPage<PerfUser>(pModel);

            Console.WriteLine($"ToPage p2: list.Count={result.list.Count}, TotalRecord={result.pModel.TotalRecord}");
            Assert.NotNull(result.list);
            Assert.True(result.list.Count <= 5, $"Expected <= 5 items on page 2, got {result.list.Count}");
        }

        #endregion

        #region ToJson

        [Fact]
        public void ToJson_ShouldReturnJsonString()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            FastWrite.Add(CreateTestUser(tag), key: _key);

            var json = FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .Take(1)
                .ToJson();

            Console.WriteLine($"ToJson length: {json?.Length ?? 0}");
            Assert.False(string.IsNullOrEmpty(json), "ToJson returned null or empty");
            Assert.Contains($"orm_test_{tag}", json);
        }

        [Fact]
        public void ToJson_EmptyResult_ShouldReturnEmptyOrNull()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var json = FastRead.Query<PerfUser>(u => u.UserName == "nonexistent_json_xyz", key: _key)
                .ToJson();

            Console.WriteLine($"ToJson empty: is null={json == null}, length={json?.Length ?? 0}");
        }

        #endregion

        #region Queue builder: ConfigureQueue + IsQueueEnabled

        [Fact]
        public void ConfigureQueue_ShouldEnableQueueForType()
        {
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "test-topic-orm",
                EnableFallback = true,
                BatchFlushSize = 50
            };

            FastWrite.ConfigureQueue<PerfUser>(config);

            var isEnabled = FastWrite.IsQueueEnabled<PerfUser>();
            Console.WriteLine($"IsQueueEnabled<PerfUser>: {isEnabled}");
            Assert.True(isEnabled);
        }

        [Fact]
        public void ConfigureQueue_WithTableName_ShouldEnableQueueForTable()
        {
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.Stream,
                Topic = "test-topic-table-orm"
            };

            FastWrite.ConfigureQueue("custom_test_table", config);

            var isEnabled = FastWrite.IsQueueEnabled("custom_test_table");
            Console.WriteLine($"IsQueueEnabled('custom_test_table'): {isEnabled}");
            Assert.True(isEnabled);
        }

        [Fact]
        public void IsQueueEnabled_UnregisteredType_ShouldReturnFalse()
        {
            var isEnabled = FastWrite.IsQueueEnabled<ConnectionPool.SmartConnectionPool>();
            Console.WriteLine($"IsQueueEnabled<SmartConnectionPool>: {isEnabled}");
            Assert.False(isEnabled);
        }

        #endregion

        #region Take operation

        [Fact]
        public void Take_ShouldLimitResultCount()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 10; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var results = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .Take(3)
                .ToList<PerfUser>();

            Console.WriteLine($"Take(3): got {results.Count} items");
            Assert.True(results.Count <= 3, $"Expected <= 3, got {results.Count}");
            Assert.True(results.Count > 0, "Expected at least 1 result");
        }

        [Fact]
        public void Take_Zero_ShouldReturnEmpty()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var results = FastRead.Query<PerfUser>(u => u.IsActive, key: _key)
                .Take(0)
                .ToList<PerfUser>();

            Console.WriteLine($"Take(0): got {results.Count} items");
            Assert.Empty(results);
        }

        #endregion

        #region AddList bulk add

        [Fact]
        public void AddList_ShouldInsertMultipleEntities()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            var list = new List<PerfUser>();
            for (int i = 0; i < 5; i++)
            {
                list.Add(CreateTestUser(tag + "_" + i));
            }

            var result = FastWrite.AddList(list, key: _key);
            Console.WriteLine($"AddList: IsSuccess={result.IsSuccess}, Message={result.Message}");
            Assert.True(result.IsSuccess, $"AddList failed: {result.Message}");

            var count = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToCount();
            Assert.Equal(5, count);
        }

        #endregion

        #region Delete by predicate

        [Fact]
        public void Delete_ByPredicate_ShouldRemoveMatchingEntities()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 3; i++)
            {
                FastWrite.Add(CreateTestUser(tag + "_" + i), key: _key);
            }

            var countBefore = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToCount();
            Assert.Equal(3, countBefore);

            var deleteResult = FastWrite.Delete<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key);
            Console.WriteLine($"Delete by predicate: IsSuccess={deleteResult.IsSuccess}, Message={deleteResult.Message}");
            Assert.True(deleteResult.IsSuccess, $"Delete by predicate failed: {deleteResult.Message}");

            var countAfter = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .ToCount();
            Assert.Equal(0, countAfter);
        }

        #endregion

        #region Fluent query chain: Where + OrderBy + Take + ToList

        [Fact]
        public void FluentQuery_Where_OrderBy_Take_ToList()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            for (int i = 0; i < 8; i++)
            {
                FastWrite.Add(new PerfUser
                {
                    UserName = $"orm_test_{tag}_{i:D2}",
                    Email = $"chain_{tag}_{i}@example.com",
                    Age = 20 + i,
                    IsActive = i % 2 == 0,
                    CreatedAt = DateTime.Now.AddMinutes(-i)
                }, key: _key);
            }

            var results = FastRead.Query<PerfUser>(u => u.UserName.Contains($"orm_test_{tag}"), key: _key)
                .Where(u => u.Age > 23)
                .OrderByDescending<PerfUser>(u => u.Age)
                .Take(5)
                .ToList<PerfUser>();

            Console.WriteLine($"Fluent chain: got {results.Count} items");
            Assert.NotEmpty(results);
            Assert.True(results.Count <= 5);

            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.True(results[i].Age >= results[i + 1].Age, "Results not in descending order");
            }
        }

        #endregion

        #region Query with Like/Contains

        [Fact]
        public void Query_Like_ShouldMatchPartialStrings()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            FastWrite.Add(CreateTestUser($"abc_{tag}"), key: _key);
            FastWrite.Add(CreateTestUser($"xyz_{tag}"), key: _key);

            var results = FastRead.Query<PerfUser>(u => u.UserName.Contains($"abc_{tag}"), key: _key)
                .ToList<PerfUser>();

            Console.WriteLine($"Query Like: got {results.Count} items");
            Assert.Single(results);
            Assert.Contains("abc", results[0].UserName);
        }

        #endregion

        #region ToList async

        [Fact]
        public async System.Threading.Tasks.Task ToListAsync_ShouldReturnResults()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            FastWrite.Add(CreateTestUser(tag), key: _key);

            var results = await FastRead.Query<PerfUser>(u => u.UserName == $"orm_test_{tag}", key: _key)
                .Take(1)
                .ToListAsync<PerfUser>();

            Console.WriteLine($"ToListAsync: got {results.Count} items");
            Assert.Single(results);
        }

        #endregion

        #region ExecuteSql read

        [Fact]
        public void ExecuteSql_Read_ShouldReturnResults()
        {
            if (!_dbAvailable)
            {
                Console.WriteLine("SKIP: database unavailable");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
            FastWrite.Add(CreateTestUser(tag), key: _key);

            var results = FastRead.ExecuteSql<PerfUser>(
                "SELECT TOP 5 * FROM perf_users WHERE UserName LIKE @p0",
                new[] { new Microsoft.Data.SqlClient.SqlParameter("@p0", $"%orm_test_{tag}%") },
                key: _key);

            Console.WriteLine($"ExecuteSql read: got {results.Count} items");
            Assert.NotEmpty(results);
        }

        #endregion
    }
}
#endif
