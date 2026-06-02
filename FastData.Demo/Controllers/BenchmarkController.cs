using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FastData;
using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 基准测试 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/benchmark")]
    public class BenchmarkController : ControllerBase
    {
        /// <summary>
        /// 性能基准测试 - 批量插入
        /// </summary>
        /// <param name="count">插入数量</param>
        [HttpPost("batch-insert")]
        public async Task<ActionResult> BenchmarkBatchInsert([FromQuery] int count = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var users = new List<AppUser>();
                for (int i = 0; i < count; i++)
                {
                    users.Add(new AppUser
                    {
                        UserName = $"bench_{i}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                        Email = $"bench{i}@test.com",
                        Age = 20 + (i % 30),
                        Department = $"Dept{i % 10}",
                        IsActive = true,
                        CreateTime = DateTime.Now
                    });
                }

                var insertStopwatch = Stopwatch.StartNew();
                var result = await Task.Run(() => FastWrite.Use("SqlServer").BulkInsertAsync(users));
                insertStopwatch.Stop();

                stopwatch.Stop();

                return Ok(new
                {
                    success = result.IsSuccess,
                    insertedCount = users.Count,
                    totalMs = stopwatch.ElapsedMilliseconds,
                    insertMs = insertStopwatch.ElapsedMilliseconds,
                    avgMsPerRecord = insertStopwatch.ElapsedMilliseconds / (double)count
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return StatusCode(500, new
                {
                    error = ex.Message,
                    totalMs = stopwatch.ElapsedMilliseconds
                });
            }
        }
    }
}
