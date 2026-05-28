using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FastData.Base;
using FastData.Config;
using FastData.Context;
using FastData.Model;
using FastData.Property;
using Xunit;
using Xunit.Abstractions;

namespace FastData.Tests
{
    /// <summary>
    /// MySQL → PostgreSQL 数据同步测试（集成测试）
    /// </summary>
    public class MySqlToPgSyncTests
    {
        private readonly ITestOutputHelper _output;

        public MySqlToPgSyncTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private bool IsSyncAvailable()
        {
            try
            {
                var mysqlConfig = DataConfig.GetConfig("MySql");
                var pgConfig = DataConfig.GetConfig("PostgreSql");
                return mysqlConfig != null && pgConfig != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试从 MySQL 读取数据并写入 PostgreSQL
        /// </summary>
        [Fact]
        public void TestSyncFromMySqlToPostgreSql()
        {
            if (!IsSyncAvailable())
            {
                _output.WriteLine("MySQL or PostgreSQL config not found, skipping integration test");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            // 1. 从 MySQL 读取数据
            var mysqlData = FastRead.Query<SyncTestEntity>(q => true, key: "MySql").ToList();
            _output.WriteLine($"从 MySQL 读取 {mysqlData.Count} 条记录");

            // 2. 写入 PostgreSQL
            var pgResult = FastWrite.AddList(mysqlData, key: "PostgreSql", IsTrans: true);

            stopwatch.Stop();
            _output.WriteLine($"同步到 PostgreSQL 结果: {pgResult.IsSuccess}");
            _output.WriteLine($"同步耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 测试批量同步性能
        /// </summary>
        [Fact]
        public void TestBatchSyncPerformance()
        {
            if (!IsSyncAvailable())
            {
                _output.WriteLine("MySQL or PostgreSQL config not found, skipping integration test");
                return;
            }

            var batchSizes = new[] { 100, 500, 1000, 5000 };
            var results = new Dictionary<int, long>();

            foreach (var batchSize in batchSizes)
            {
                // 1. 在 MySQL 中插入测试数据
                var entities = GenerateEntities(batchSize);
                var mysqlInsertResult = FastWrite.AddList(entities, key: "MySql", IsTrans: true);
                _output.WriteLine($"MySQL 插入 {batchSize} 条: {mysqlInsertResult.IsSuccess}");

                // 2. 从 MySQL 读取
                var mysqlData = FastRead.Query<SyncTestEntity>(q => true, key: "MySql").ToList();

                // 3. 同步到 PostgreSQL
                var stopwatch = Stopwatch.StartNew();
                var pgResult = FastWrite.AddList(mysqlData, key: "PostgreSql", IsTrans: true);
                stopwatch.Stop();

                results[batchSize] = stopwatch.ElapsedMilliseconds;
                _output.WriteLine($"同步 {batchSize} 条到 PostgreSQL: {stopwatch.ElapsedMilliseconds}ms, Success: {pgResult.IsSuccess}");
            }

            _output.WriteLine("\n同步性能对比:");
            foreach (var kvp in results)
            {
                _output.WriteLine($"  {kvp.Key} 条: {kvp.Value}ms (平均 {(double)kvp.Value / kvp.Key}ms/条)");
            }
        }

        /// <summary>
        /// 测试数据一致性
        /// </summary>
        [Fact]
        public void TestDataConsistency()
        {
            if (!IsSyncAvailable())
            {
                _output.WriteLine("MySQL or PostgreSQL config not found, skipping integration test");
                return;
            }

            // 1. 在 MySQL 中插入特定数据
            var testEntity = new SyncTestEntity
            {
                Name = "ConsistencyTest_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Value = 999.99m,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            FastWrite.Add(testEntity, key: "MySql");
            _output.WriteLine($"MySQL 插入测试数据: {testEntity.Name}");

            // 2. 从 MySQL 读取
            var mysqlEntity = FastRead.Query<SyncTestEntity>(q => q.Name == testEntity.Name, key: "MySql").ToList().FirstOrDefault();

            Assert.NotNull(mysqlEntity);
            _output.WriteLine($"MySQL 读取: {mysqlEntity.Name}, Value: {mysqlEntity.Value}");

            // 3. 同步到 PostgreSQL
            FastWrite.Add(mysqlEntity, key: "PostgreSql");

            // 4. 从 PostgreSQL 读取验证
            var pgEntity = FastRead.Query<SyncTestEntity>(q => q.Name == testEntity.Name, key: "PostgreSql").ToList().FirstOrDefault();

            Assert.NotNull(pgEntity);
            Assert.Equal(mysqlEntity.Name, pgEntity.Name);
            Assert.Equal(mysqlEntity.Value, pgEntity.Value);
            Assert.Equal(mysqlEntity.IsActive, pgEntity.IsActive);

            _output.WriteLine($"PostgreSQL 读取: {pgEntity.Name}, Value: {pgEntity.Value}");
            _output.WriteLine("数据一致性验证通过!");
        }

        /// <summary>
        /// 测试增量同步
        /// </summary>
        [Fact]
        public void TestIncrementalSync()
        {
            if (!IsSyncAvailable())
            {
                _output.WriteLine("MySQL or PostgreSQL config not found, skipping integration test");
                return;
            }

            // 1. 初始同步
            var initialEntities = GenerateEntities(100);
            FastWrite.AddList(initialEntities, key: "MySql", IsTrans: true);
            _output.WriteLine($"初始插入 {initialEntities.Count} 条到 MySQL");

            // 2. 读取并同步
            var mysqlData = FastRead.Query<SyncTestEntity>(q => true, key: "MySql").ToList();
            FastWrite.AddList(mysqlData, key: "PostgreSql", IsTrans: true);
            _output.WriteLine($"初始同步 {mysqlData.Count} 条到 PostgreSQL");

            // 3. 增量数据
            var incrementalEntities = GenerateEntities(50);
            incrementalEntities.ForEach(e => e.Name = "Incremental_" + e.Name);
            FastWrite.AddList(incrementalEntities, key: "MySql", IsTrans: true);
            _output.WriteLine($"增量插入 {incrementalEntities.Count} 条到 MySQL");

            // 4. 增量同步（实际场景中需要记录同步点，这里简化处理）
            var incrementalData = FastRead.Query<SyncTestEntity>(q => q.Name.StartsWith("Incremental_"), key: "MySql").ToList();
            FastWrite.AddList(incrementalData, key: "PostgreSql", IsTrans: true);
            _output.WriteLine($"增量同步 {incrementalData.Count} 条到 PostgreSQL");

            // 5. 验证总数
            var pgCount = FastRead.Query<SyncTestEntity>(q => true, key: "PostgreSql").ToList().Count;
            _output.WriteLine($"PostgreSQL 总记录数: {pgCount}");
            Assert.True(pgCount >= initialEntities.Count + incrementalEntities.Count);
        }

        private List<SyncTestEntity> GenerateEntities(int count)
        {
            var entities = new List<SyncTestEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(new SyncTestEntity
                {
                    Name = $"SyncTest_{i}",
                    Value = i * 3.14m,
                    CreatedAt = DateTime.Now,
                    IsActive = i % 3 != 0
                });
            }
            return entities;
        }
    }

    public class SyncTestEntity
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
