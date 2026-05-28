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
    /// MySQL 批量插入性能测试（集成测试，需要数据库连接）
    /// </summary>
    public class MySqlBatchInsertTests
    {
        private readonly ITestOutputHelper _output;

        public MySqlBatchInsertTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private bool IsMySqlAvailable()
        {
            try
            {
                var config = DataConfig.GetConfig("MySql");
                return config != null;
            }
            catch
            {
                return false;
            }
        }

        [Fact]
        public void TestSingleInsert()
        {
            if (!IsMySqlAvailable())
            {
                _output.WriteLine("MySQL config not found, skipping integration test");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var count = 100;

            for (int i = 0; i < count; i++)
            {
                var entity = new TestBatchEntity
                {
                    Name = $"Test_{i}",
                    Value = i * 1.5m,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                using (var db = new DataContext("MySql"))
                {
                    db.Add(entity);
                }
            }

            stopwatch.Stop();
            _output.WriteLine($"单条插入 {count} 条记录: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"平均每条: {stopwatch.ElapsedMilliseconds / count}ms");
        }

        [Fact]
        public void TestBatchInsertWithTransaction()
        {
            if (!IsMySqlAvailable())
            {
                _output.WriteLine("MySQL config not found, skipping integration test");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var batchSize = 1000;
            var entities = GenerateEntities(batchSize);

            using (var db = new DataContext("MySql"))
            {
                db.BeginTrans();
                try
                {
                    foreach (var entity in entities)
                    {
                        db.Add(entity, isTrans: true);
                    }
                    db.SubmitTrans();
                }
                catch
                {
                    db.RollbackTrans();
                    throw;
                }
            }

            stopwatch.Stop();
            _output.WriteLine($"批量插入 {batchSize} 条记录（事务）: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"平均每条: {(double)stopwatch.ElapsedMilliseconds / batchSize}ms");
        }

        [Fact]
        public void TestDifferentBatchSizes()
        {
            if (!IsMySqlAvailable())
            {
                _output.WriteLine("MySQL config not found, skipping integration test");
                return;
            }

            var batchSizes = new[] { 100, 500, 1000, 5000 };
            var results = new Dictionary<int, long>();

            foreach (var batchSize in batchSizes)
            {
                var entities = GenerateEntities(batchSize);
                var stopwatch = Stopwatch.StartNew();

                using (var db = new DataContext("MySql"))
                {
                    db.BeginTrans();
                    try
                    {
                        foreach (var entity in entities)
                        {
                            db.Add(entity, isTrans: true);
                        }
                        db.SubmitTrans();
                    }
                    catch
                    {
                        db.RollbackTrans();
                        throw;
                    }
                }

                stopwatch.Stop();
                results[batchSize] = stopwatch.ElapsedMilliseconds;
                _output.WriteLine($"批量大小 {batchSize}: {stopwatch.ElapsedMilliseconds}ms");
            }

            _output.WriteLine("\n性能对比:");
            foreach (var kvp in results)
            {
                _output.WriteLine($"  {kvp.Key} 条: {kvp.Value}ms (平均 {(double)kvp.Value / kvp.Key}ms/条)");
            }
        }

        [Fact]
        public void TestFastWriteBatch()
        {
            if (!IsMySqlAvailable())
            {
                _output.WriteLine("MySQL config not found, skipping integration test");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var batchSize = 1000;
            var entities = GenerateEntities(batchSize);

            var result = FastWrite.AddList(entities, key: "MySql", IsTrans: true);

            stopwatch.Stop();
            _output.WriteLine($"FastWrite 批量插入 {batchSize} 条记录: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"IsSuccess: {result.IsSuccess}");
        }

        [Fact]
        public void TestAddListDirect()
        {
            if (!IsMySqlAvailable())
            {
                _output.WriteLine("MySQL config not found, skipping integration test");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var batchSize = 1000;
            var entities = GenerateEntities(batchSize);

            using (var db = new DataContext("MySql"))
            {
                var result = db.AddList(entities, IsTrans: true);
                _output.WriteLine($"AddList 结果: {result.writeReturn.IsSuccess}");
            }

            stopwatch.Stop();
            _output.WriteLine($"DataContext.AddList 插入 {batchSize} 条记录: {stopwatch.ElapsedMilliseconds}ms");
        }

        private List<TestBatchEntity> GenerateEntities(int count)
        {
            var entities = new List<TestBatchEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(new TestBatchEntity
                {
                    Name = $"Batch_{i}",
                    Value = i * 2.5m,
                    CreatedAt = DateTime.Now,
                    IsActive = i % 2 == 0
                });
            }
            return entities;
        }
    }

    public class TestBatchEntity
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
