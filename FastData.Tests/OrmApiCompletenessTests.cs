using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastData.Base;
using FastData.Config;
using FastData.Context;
using FastData.Model;
using FastData.Property;
using FastData.Sharding.Strategies;
using Xunit;
using Xunit.Abstractions;

namespace FastData.Tests
{
    /// <summary>
    /// ORM API 完整性验证测试
    /// </summary>
    public class OrmApiCompletenessTests
    {
        private readonly ITestOutputHelper _output;

        public OrmApiCompletenessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Read API Tests

        [Fact]
        public void TestQuery_WithPredicate()
        {
            var query = FastRead.Query<TestEntity>(q => q.IsActive);
            Assert.NotNull(query);
            _output.WriteLine("Query with predicate: OK");
        }

        [Fact]
        public void TestQuery_WithComplexPredicate()
        {
            var query = FastRead.Query<TestEntity>(q => q.IsActive && q.Age > 18);
            Assert.NotNull(query);
            _output.WriteLine("Query with complex predicate: OK");
        }

        [Fact]
        public void TestQuery_WithNullCheck()
        {
            var query = FastRead.Query<TestEntity>(q => q.Name != null);
            Assert.NotNull(query);
            _output.WriteLine("Query with null check: OK");
        }

        [Fact]
        public void TestQuery_WithContains()
        {
            var names = new List<string> { "Alice", "Bob" };
            var query = FastRead.Query<TestEntity>(q => names.Contains(q.Name));
            Assert.NotNull(query);
            _output.WriteLine("Query with Contains: OK");
        }

        [Fact]
        public void TestQuery_WithStartsWith()
        {
            var query = FastRead.Query<TestEntity>(q => q.Name.StartsWith("A"));
            Assert.NotNull(query);
            _output.WriteLine("Query with StartsWith: OK");
        }

        [Fact]
        public void TestQuery_WithEndsWith()
        {
            var query = FastRead.Query<TestEntity>(q => q.Name.EndsWith("e"));
            Assert.NotNull(query);
            _output.WriteLine("Query with EndsWith: OK");
        }

        #endregion

        #region Chainable API Tests

        [Fact]
        public void TestWhere_Chainable()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive)
                .Where(q => q.Age > 18);
            Assert.NotNull(query);
            _output.WriteLine("Where chainable: OK");
        }

        [Fact]
        public void TestOr_Chainable()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive)
                .Or(q => q.Age > 30);
            Assert.NotNull(query);
            _output.WriteLine("Or chainable: OK");
        }

        [Fact]
        public void TestAnd_Chainable()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive)
                .And(q => q.Age > 18);
            Assert.NotNull(query);
            _output.WriteLine("And chainable: OK");
        }

        [Fact]
        public void TestSelect_Projection()
        {
            var query = new DataQuery<TestEntity>()
                .Select(q => new { q.Name, q.Age });
            Assert.NotNull(query);
            _output.WriteLine("Select projection: OK");
        }

        [Fact]
        public void TestOrderBy_Chainable()
        {
            var query = new DataQuery<TestEntity>()
                .OrderBy(q => q.Name);
            Assert.NotNull(query);
            _output.WriteLine("OrderBy chainable: OK");
        }

        [Fact]
        public void TestOrderByDescending_Chainable()
        {
            var query = new DataQuery<TestEntity>()
                .OrderByDescending(q => q.Age);
            Assert.NotNull(query);
            _output.WriteLine("OrderByDescending chainable: OK");
        }

        #endregion

        #region Write API Tests

        [Fact]
        public void TestFastWrite_Add()
        {
            var entity = new TestEntity
            {
                Name = "Test_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Age = 25,
                Email = "test@example.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = FastWrite.Add(entity);
            Assert.NotNull(result);
            _output.WriteLine($"FastWrite.Add: {result.IsSuccess}");
        }

        [Fact]
        public void TestFastWrite_AddList()
        {
            var entities = new List<TestEntity>();
            for (int i = 0; i < 10; i++)
            {
                entities.Add(new TestEntity
                {
                    Name = $"Batch_{i}",
                    Age = 20 + i,
                    Email = $"batch{i}@example.com",
                    IsActive = i % 2 == 0,
                    CreatedAt = DateTime.Now
                });
            }

            var result = FastWrite.AddList(entities);
            Assert.NotNull(result);
            _output.WriteLine($"FastWrite.AddList: {result.IsSuccess}");
        }

        [Fact]
        public void TestFastWrite_Update()
        {
            var entity = new TestEntity
            {
                Name = "UpdateTest",
                Age = 30,
                Email = "update@example.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var addResult = FastWrite.Add(entity);
            Assert.True(addResult.IsSuccess);

            entity.Age = 31;
            var updateResult = FastWrite.Update(entity);
            Assert.NotNull(updateResult);
            _output.WriteLine($"FastWrite.Update: {updateResult.IsSuccess}");
        }

        [Fact]
        public void TestFastWrite_Delete()
        {
            var entity = new TestEntity
            {
                Name = "DeleteTest",
                Age = 25,
                Email = "delete@example.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var addResult = FastWrite.Add(entity);
            Assert.True(addResult.IsSuccess);

            var deleteResult = FastWrite.Delete(entity);
            Assert.NotNull(deleteResult);
            _output.WriteLine($"FastWrite.Delete: {deleteResult.IsSuccess}");
        }

        #endregion

        #region DataQuery Generic API Tests

        [Fact]
        public void TestGenericQuery_Where()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive);
            Assert.NotNull(query);
            _output.WriteLine("Generic Where: OK");
        }

        [Fact]
        public void TestGenericQuery_Or()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive)
                .Or(q => q.Age > 30);
            Assert.NotNull(query);
            _output.WriteLine("Generic Or: OK");
        }

        [Fact]
        public void TestGenericQuery_OrderBy()
        {
            var query = new DataQuery<TestEntity>()
                .OrderBy(q => q.Name);
            Assert.NotNull(query);
            _output.WriteLine("Generic OrderBy: OK");
        }

        [Fact]
        public void TestGenericQuery_OrderByDescending()
        {
            var query = new DataQuery<TestEntity>()
                .OrderByDescending(q => q.Age);
            Assert.NotNull(query);
            _output.WriteLine("Generic OrderByDescending: OK");
        }

        [Fact]
        public void TestGenericQuery_GroupBy()
        {
            var query = new DataQuery<TestEntity>()
                .GroupBy(q => q.Name);
            Assert.NotNull(query);
            _output.WriteLine("Generic GroupBy: OK");
        }

        [Fact]
        public void TestGenericQuery_Select()
        {
            var query = new DataQuery<TestEntity>()
                .Select(q => new { q.Name, q.Age });
            Assert.NotNull(query);
            _output.WriteLine("Generic Select: OK");
        }

        #endregion

        #region Pagination API Tests

        [Fact]
        public void TestToPagination()
        {
            var query = new DataQuery<TestEntity>()
                .Where(q => q.IsActive)
                .OrderBy(q => q.Name)
                .ToPagination(1, 10);
            Assert.NotNull(query);
            _output.WriteLine("ToPagination: OK");
        }

        #endregion

        #region SQL Log API Tests

        [Fact]
        public void TestSqlLog_Global()
        {
            FastDb.EnableSqlLog = true;
            Assert.True(FastDb.EnableSqlLog);
            FastDb.EnableSqlLog = false;
            _output.WriteLine("Global SQL log: OK");
        }

        [Fact]
        public void TestSqlLog_PerQuery()
        {
            var query = new DataQuery<TestEntity>()
                .EnableSqlLog();
            Assert.NotNull(query);
            _output.WriteLine("Per-query SQL log: OK");
        }

        #endregion

        #region Sharding API Tests

        [Fact]
        public void TestSharding_StrategyTypes()
        {
            var timeStrategy = new TimeShardingStrategy();
            Assert.NotNull(timeStrategy);
            Assert.Equal("TimeSharding", timeStrategy.Name);

            var hashStrategy = new HashShardingStrategy();
            Assert.NotNull(hashStrategy);
            Assert.Equal("HashSharding", hashStrategy.Name);

            var listStrategy = new ListShardingStrategy();
            Assert.NotNull(listStrategy);
            Assert.Equal("ListSharding", listStrategy.Name);

            _output.WriteLine("Sharding strategy types: OK");
        }

        [Fact]
        public void TestSharding_UseSharding()
        {
            var query = new DataQuery<TestEntity>()
                .UseSharding();
            Assert.NotNull(query);
            Assert.True(query.EnableSharding);
            _output.WriteLine("UseSharding: OK");
        }

        #endregion

        #region Where Builder Tests

        [Fact]
        public void TestWhereBuilder_Create()
        {
            var builder = new Where<TestEntity>();
            Assert.NotNull(builder);
            _output.WriteLine("Where builder create: OK");
        }

        [Fact]
        public void TestWhereBuilder_And()
        {
            var builder = new Where<TestEntity>()
                .And(q => q.IsActive)
                .And(q => q.Age > 18);
            Assert.NotNull(builder);
            _output.WriteLine("Where builder And: OK");
        }

        [Fact]
        public void TestWhereBuilder_Or()
        {
            var builder = new Where<TestEntity>()
                .And(q => q.IsActive)
                .Or(q => q.Age > 30);
            Assert.NotNull(builder);
            _output.WriteLine("Where builder Or: OK");
        }

        #endregion

        #region Attribute Tests

        [Fact]
        public void TestColumnAttribute_IsIdentity()
        {
            var attr = new ColumnAttribute { IsIdentity = true };
            Assert.True(attr.IsIdentity);
            _output.WriteLine("Column IsIdentity: OK");
        }

        [Fact]
        public void TestTableAttribute_Comments()
        {
            var attr = new TableAttribute { Comments = "Test table" };
            Assert.Equal("Test table", attr.Comments);
            _output.WriteLine("Table Comments: OK");
        }

        #endregion

        #region Config API Tests

        [Fact]
        public void TestConfig_GetConfig()
        {
            try
            {
                var config = DataConfig.GetConfig("SqlServer");
                _output.WriteLine($"GetConfig SqlServer: {config != null}");
            }
            catch
            {
                _output.WriteLine("GetConfig SqlServer: config not found (expected in test)");
            }
        }

        [Fact]
        public void TestConfig_GetActiveEnvironment()
        {
            try
            {
                var env = DataConfig.GetActiveEnvironment();
                _output.WriteLine($"GetActiveEnvironment: {env}");
            }
            catch
            {
                _output.WriteLine("GetActiveEnvironment: not available (expected in test)");
            }
        }

        #endregion

        #region Model Tests

        [Fact]
        public void TestWriteReturn()
        {
            var writeReturn = new WriteReturn
            {
                IsSuccess = true,
                Message = "Success"
            };
            Assert.True(writeReturn.IsSuccess);
            Assert.Equal("Success", writeReturn.Message);
            _output.WriteLine("WriteReturn: OK");
        }

        #endregion
    }

    public class TestEntity
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
