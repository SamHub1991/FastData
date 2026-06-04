using FastData;
using FastData.Context;
using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 高并发测试控制器
    /// 用于对各个 API 进行高并发压力测试
    /// </summary>
    [Route("api/ConcurrencyTest")]
    [ApiController]
    public class ConcurrencyTestController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, ConcurrentBag<long>> _latencies = new();
        private static readonly ConcurrentDictionary<string, int> _successCounts = new();
        private static readonly ConcurrentDictionary<string, int> _failureCounts = new();

        private static DbParameter[] CreateParams(string dbKey, params (string name, object value)[] pairs)
        {
            return pairs.Select(p => new SqlParameter($"@{p.name}", p.value ?? DBNull.Value)).ToArray<DbParameter>();
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetStats()
        {
            _latencies.Clear();
            _successCounts.Clear();
            _failureCounts.Clear();
            return Ok(new { message = "统计数据已重置" });
        }

        /// <summary>
        /// 获取统计结果
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var stats = new Dictionary<string, object>();

            foreach (var key in _latencies.Keys)
            {
                var latencies = _latencies[key].OrderBy(x => x).ToList();
                if (latencies.Count > 0)
                {
                    stats[key] = new
                    {
                        TotalRequests = latencies.Count,
                        SuccessCount = _successCounts.GetValueOrDefault(key, 0),
                        FailureCount = _failureCounts.GetValueOrDefault(key, 0),
                        SuccessRate = $"{(_successCounts.GetValueOrDefault(key, 0) * 100.0 / latencies.Count):F2}%",
                        AvgLatencyMs = $"{latencies.Average():F2}",
                        MinLatencyMs = latencies.Min(),
                        MaxLatencyMs = latencies.Max(),
                        P50LatencyMs = latencies[latencies.Count / 2],
                        P95LatencyMs = latencies[(int)(latencies.Count * 0.95)],
                        P99LatencyMs = latencies[(int)(latencies.Count * 0.99)]
                    };
                }
            }

            return Ok(stats);
        }

        /// <summary>
        /// 测试1: 单条查询
        /// </summary>
        [HttpGet("query/single")]
        public async Task<IActionResult> TestSingleQuery([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = "SELECT TOP 1 * FROM perf_users WHERE Id > @Id ORDER BY NEWID()";
                var param = CreateParams(dbKey, ("Id", new Random().Next(1, 9000)));
                
                var result = await FastRead.ExecuteSqlAsync<PerfUser>(sql, param, key: dbKey);
                
                sw.Stop();
                RecordLatency($"Query_Single_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    user = result.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Query_Single_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试2: 批量查询
        /// </summary>
        [HttpGet("query/batch")]
        public async Task<IActionResult> TestBatchQuery([FromQuery] int pageSize = 100, [FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = "SELECT * FROM perf_users WHERE Id > @Offset AND Id <= @End";
                var random = new Random();
                var offset = random.Next(1, 8000);
                var param = CreateParams(dbKey, ("Offset", offset), ("End", offset + pageSize));
                
                var result = await FastRead.ExecuteSqlAsync<PerfUser>(sql, param, key: dbKey);
                
                sw.Stop();
                RecordLatency($"Query_Batch_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    count = result.Count
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Query_Batch_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试3: 写入操作
        /// </summary>
        [HttpPost("write/insert")]
        public async Task<IActionResult> TestInsert([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = @"INSERT INTO perf_users (Name, Email, Age, CreatedAt) 
                           VALUES (@Name, @Email, @Age, @CreatedAt)";
                var param = CreateParams(dbKey, 
                    ("Name", $"Test_{Guid.NewGuid():N}"),
                    ("Email", $"test_{Guid.NewGuid():N}@example.com"),
                    ("Age", new Random().Next(18, 80)),
                    ("CreatedAt", DateTime.Now));
                
                var result = await FastWrite.ExecuteSqlAsync(sql, param, key: dbKey);
                
                sw.Stop();
                RecordLatency($"Write_Insert_{dbKey}", sw.ElapsedMilliseconds, result.IsSuccess);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Write_Insert_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试4: 更新操作
        /// </summary>
        [HttpPut("write/update")]
        public async Task<IActionResult> TestUpdate([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var random = new Random();
                var id = random.Next(1, 9000);
                var param = CreateParams(dbKey, ("Id", id), ("Name", $"Updated_{DateTime.Now.Ticks}"));

                var sql = "UPDATE perf_users SET Name = @Name WHERE Id = @Id";
                var result = await FastWrite.ExecuteSqlAsync(sql, param, key: dbKey);
                
                sw.Stop();
                RecordLatency($"Write_Update_{dbKey}", sw.ElapsedMilliseconds, result.IsSuccess);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Write_Update_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试5: 删除操作
        /// </summary>
        [HttpDelete("write/delete")]
        public async Task<IActionResult> TestDelete([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = "DELETE FROM perf_users WHERE Name LIKE 'Test_%' AND Id > 9000";
                var result = await FastWrite.ExecuteSqlAsync(sql, new DbParameter[0], key: dbKey);
                
                sw.Stop();
                RecordLatency($"Write_Delete_{dbKey}", sw.ElapsedMilliseconds, result.IsSuccess);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Write_Delete_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试6: 事务操作
        /// </summary>
        [HttpPost("transaction")]
        public async Task<IActionResult> TestTransaction([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var context = new DataContext(dbKey);
                context.BeginTrans();

                // 插入
                var insertSql = "INSERT INTO perf_users (Name, Email, Age, CreatedAt) VALUES (@Name, @Email, @Age, @CreatedAt)";
                var insertParam = CreateParams(dbKey,
                    ("Name", $"Txn_{Guid.NewGuid():N}"),
                    ("Email", $"txn_{Guid.NewGuid():N}@example.com"),
                    ("Age", new Random().Next(18, 80)),
                    ("CreatedAt", DateTime.Now));
                context.ExecuteSql(insertSql, insertParam);

                // 更新
                var updateSql = "UPDATE perf_users SET Age = Age + 1 WHERE Name LIKE 'Txn_%'";
                context.ExecuteSql(updateSql, new DbParameter[0]);

                context.SubmitTrans();
                
                sw.Stop();
                RecordLatency($"Transaction_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    message = "事务执行成功"
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Transaction_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试7: 分页查询
        /// </summary>
        [HttpGet("query/paged")]
        public async Task<IActionResult> TestPagedQuery([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = @"SELECT * FROM perf_users 
                           WHERE Id > @Offset AND Id <= @End
                           ORDER BY Id";
                var offset = (page - 1) * pageSize;
                var param = CreateParams(dbKey, ("Offset", offset), ("End", offset + pageSize));
                
                var result = await FastRead.ExecuteSqlAsync<PerfUser>(sql, param, key: dbKey);
                
                sw.Stop();
                RecordLatency($"Query_Paged_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    page,
                    pageSize,
                    count = result.Count
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Query_Paged_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试8: 聚合查询
        /// </summary>
        [HttpGet("query/aggregate")]
        public async Task<IActionResult> TestAggregateQuery([FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = @"SELECT 
                               COUNT(*) as TotalCount,
                               AVG(CAST(Age as FLOAT)) as AvgAge,
                               MIN(Age) as MinAge,
                               MAX(Age) as MaxAge
                           FROM perf_users";
                
                var result = await FastRead.ExecuteSqlAsync(sql, new DbParameter[0], key: dbKey);
                
                sw.Stop();
                RecordLatency($"Query_Aggregate_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    stats = result.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Query_Aggregate_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试9: 多数据库并发查询
        /// </summary>
        [HttpGet("query/multi-db")]
        public async Task<IActionResult> TestMultiDbQuery()
        {
            var sw = Stopwatch.StartNew();
            var dbKeys = new[] { "SqlServer", "MySql", "PostgreSql" };

            try
            {
                var tasks = dbKeys.Select(dbKey => Task.Factory.StartNew(async () =>
                {
                    var sql = "SELECT COUNT(*) as Count FROM perf_users";
                    var result = await FastRead.ExecuteSqlAsync(sql, new DbParameter[0], key: dbKey);
                    return new { dbKey, success = true, count = result.FirstOrDefault() };
                }).Unwrap()).ToArray();

                var results = await Task.WhenAll(tasks);
                
                sw.Stop();
                RecordLatency("Query_MultiDb", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    results
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency("Query_MultiDb", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试10: 批量写入
        /// </summary>
        [HttpPost("write/bulk")]
        public async Task<IActionResult> TestBulkInsert([FromQuery] int count = 100, [FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = @"INSERT INTO perf_users (Name, Email, Age, CreatedAt) 
                           VALUES (@Name, @Email, @Age, @CreatedAt)";
                
                for (int i = 0; i < count; i++)
                {
                    var param = CreateParams(dbKey,
                        ("Name", $"Bulk_{Guid.NewGuid():N}"),
                        ("Email", $"bulk_{Guid.NewGuid():N}@example.com"),
                        ("Age", new Random().Next(18, 80)),
                        ("CreatedAt", DateTime.Now));
                    await FastWrite.ExecuteSqlAsync(sql, param, key: dbKey);
                }
                
                sw.Stop();
                RecordLatency($"Write_Bulk_{dbKey}", sw.ElapsedMilliseconds, true);
                
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    insertedCount = count
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Write_Bulk_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试11: 连接池压力测试
        /// </summary>
        [HttpGet("pool/stress")]
        public async Task<IActionResult> TestConnectionPoolStress([FromQuery] int iterations = 10, [FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            var results = new ConcurrentBag<(int iteration, bool success, long latencyMs, string error)>();

            try
            {
                var tasks = Enumerable.Range(1, iterations).Select(i => Task.Factory.StartNew(async () =>
                {
                    var innerSw = Stopwatch.StartNew();
                    try
                    {
                        var sql = "SELECT 1";
                        var result = await FastRead.ExecuteSqlAsync(sql, new DbParameter[0], key: dbKey);
                        innerSw.Stop();
                        results.Add((i, true, innerSw.ElapsedMilliseconds, null));
                    }
                    catch (Exception ex)
                    {
                        innerSw.Stop();
                        results.Add((i, false, innerSw.ElapsedMilliseconds, ex.Message));
                    }
                }).Unwrap()).ToArray();

                await Task.WhenAll(tasks);
                
                sw.Stop();
                var allResults = results.ToList();
                var successCount = allResults.Count(r => r.success);
                
                RecordLatency($"Pool_Stress_{dbKey}", sw.ElapsedMilliseconds, successCount == iterations);
                
                return Ok(new
                {
                    success = successCount == iterations,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    iterations,
                    successCount,
                    failureCount = iterations - successCount,
                    avgLatencyMs = allResults.Average(r => r.latencyMs)
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Pool_Stress_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// 测试12: 混合读写压力测试
        /// </summary>
        [HttpPost("mixed/stress")]
        public async Task<IActionResult> TestMixedStress([FromQuery] int count = 100, [FromQuery] string dbKey = "SqlServer")
        {
            var sw = Stopwatch.StartNew();
            var results = new ConcurrentBag<(int index, bool success, long latencyMs, string error)>();

            try
            {
                var tasks = Enumerable.Range(1, count).Select(i => Task.Factory.StartNew(async () =>
                {
                    var innerSw = Stopwatch.StartNew();
                    try
                    {
                        if (i % 3 == 0)
                        {
                            // 写操作
                            var param = CreateParams(dbKey,
                                ("Name", $"Mixed_{Guid.NewGuid():N}"),
                                ("Email", $"mixed_{Guid.NewGuid():N}@example.com"),
                                ("Age", new Random().Next(18, 80)),
                                ("CreatedAt", DateTime.Now));
                            var sql = "INSERT INTO perf_users (Name, Email, Age, CreatedAt) VALUES (@Name, @Email, @Age, @CreatedAt)";
                            await FastWrite.ExecuteSqlAsync(sql, param, key: dbKey);
                        }
                        else
                        {
                            // 读操作
                            var sql = "SELECT TOP 1 * FROM perf_users WHERE Id > @Id ORDER BY NEWID()";
                            var param = CreateParams(dbKey, ("Id", new Random().Next(1, 9000)));
                            await FastRead.ExecuteSqlAsync<PerfUser>(sql, param, key: dbKey);
                        }
                        
                        innerSw.Stop();
                        results.Add((i, true, innerSw.ElapsedMilliseconds, null));
                    }
                    catch (Exception ex)
                    {
                        innerSw.Stop();
                        results.Add((i, false, innerSw.ElapsedMilliseconds, ex.Message));
                    }
                }).Unwrap()).ToArray();

                await Task.WhenAll(tasks);
                
                sw.Stop();
                var allResults = results.ToList();
                var successCount = allResults.Count(r => r.success);
                
                RecordLatency($"Mixed_Stress_{dbKey}", sw.ElapsedMilliseconds, successCount == count);
                
                return Ok(new
                {
                    success = successCount == count,
                    latencyMs = sw.ElapsedMilliseconds,
                    dbKey,
                    totalOperations = count,
                    successCount,
                    failureCount = count - successCount,
                    successRate = $"{(successCount * 100.0 / count):F2}%",
                    avgLatencyMs = allResults.Average(r => r.latencyMs)
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                RecordLatency($"Mixed_Stress_{dbKey}", sw.ElapsedMilliseconds, false);
                return StatusCode(500, new { error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
            }
        }

        private void RecordLatency(string key, long latencyMs, bool success)
        {
            _latencies.AddOrUpdate(key,
                new ConcurrentBag<long> { latencyMs },
                (k, v) => { v.Add(latencyMs); return v; });

            if (success)
                _successCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
            else
                _failureCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
        }
    }
}
