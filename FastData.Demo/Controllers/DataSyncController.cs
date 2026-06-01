using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using FastData;
using FastData.Demo.Models;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 数据同步 Demo - 跨数据库数据同步
    /// </summary>
    [ApiController]
    [Route("api/DataSync")]
    public class DataSyncController : ControllerBase
    {
        /// <summary>
        /// 获取同步状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Feature = "数据同步",
                Description = "多数据库数据同步",
                Status = "就绪",
                SupportedDatabases = new[] { "SqlServer", "MySql", "PostgreSql", "SQLite" }
            });
        }

        /// <summary>
        /// 同步用户数据：从源数据库读取，写入目标数据库
        /// </summary>
        [HttpPost("sync-users")]
        public IActionResult SyncUsers([FromBody] DataSyncRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var sourceKey = request.SourceDb ?? "mysql";
                var targetKey = request.TargetDb ?? "postgres";

                var sourceUsers = FastRead.Query<AppUser>(u => u.Id > 0, key: sourceKey).ToList<AppUser>();

                var synced = 0;
                var failed = 0;
                foreach (var user in sourceUsers)
                {
                    var existing = FastRead.Query<AppUser>(u => u.UserName == user.UserName, key: targetKey).ToItem<AppUser>();
                    if (existing != null)
                    {
                        existing.Email = user.Email;
                        existing.Phone = user.Phone;
                        existing.Age = user.Age;
                        existing.Department = user.Department;
                        existing.Salary = user.Salary;
                        existing.UpdateTime = DateTime.Now;
                        var updateResult = FastWrite.Update(existing, key: targetKey);
                        if (updateResult.IsSuccess) synced++;
                        else failed++;
                    }
                    else
                    {
                        user.Id = 0;
                        user.CreateTime = DateTime.Now;
                        var addResult = FastWrite.Add(user, key: targetKey);
                        if (addResult.IsSuccess) synced++;
                        else failed++;
                    }
                }

                stopwatch.Stop();
                return Ok(new
                {
                    Success = true,
                    Message = $"同步完成: 成功 {synced} 条, 失败 {failed} 条",
                    SourceDb = sourceKey,
                    TargetDb = targetKey,
                    TotalRead = sourceUsers.Count,
                    Synced = synced,
                    Failed = failed,
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return Ok(new
                {
                    Success = false,
                    Message = $"同步失败: {ex.Message}",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 同步订单数据
        /// </summary>
        [HttpPost("sync-orders")]
        public IActionResult SyncOrders([FromBody] DataSyncRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var sourceKey = request.SourceDb ?? "mysql";
                var targetKey = request.TargetDb ?? "postgres";

                var sourceOrders = FastRead.Query<AppOrder>(o => o.Id > 0, key: sourceKey).ToList<AppOrder>();

                var synced = 0;
                var failed = 0;
                foreach (var order in sourceOrders)
                {
                    var existing = FastRead.Query<AppOrder>(o => o.OrderNo == order.OrderNo, key: targetKey).ToItem<AppOrder>();
                    if (existing != null)
                    {
                        existing.Status = order.Status;
                        existing.TotalAmount = order.TotalAmount;
                        existing.PayTime = order.PayTime;
                        var updateResult = FastWrite.Update(existing, key: targetKey);
                        if (updateResult.IsSuccess) synced++;
                        else failed++;
                    }
                    else
                    {
                        order.Id = 0;
                        var addResult = FastWrite.Add(order, key: targetKey);
                        if (addResult.IsSuccess) synced++;
                        else failed++;
                    }
                }

                stopwatch.Stop();
                return Ok(new
                {
                    Success = true,
                    Message = $"订单同步完成: 成功 {synced} 条, 失败 {failed} 条",
                    SourceDb = sourceKey,
                    TargetDb = targetKey,
                    TotalRead = sourceOrders.Count,
                    Synced = synced,
                    Failed = failed,
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return Ok(new
                {
                    Success = false,
                    Message = $"订单同步失败: {ex.Message}",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 获取各数据库用户数量
        /// </summary>
        [HttpGet("count")]
        public IActionResult GetUserCount()
        {
            try
            {
                var sqlserver = FastRead.Query<AppUser>(u => u.Id > 0, key: "sqlserver").ToCount();
                var mysql = FastRead.Query<AppUser>(u => u.Id > 0, key: "mysql").ToCount();
                var postgres = FastRead.Query<AppUser>(u => u.Id > 0, key: "postgres").ToCount();
                var sqlite = FastRead.Query<AppUser>(u => u.Id > 0, key: "sqlite").ToCount();

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        SqlServer = sqlserver,
                        MySql = mysql,
                        PostgreSql = postgres,
                        SQLite = sqlite
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }
    }

    public class DataSyncRequest
    {
        public string SourceDb { get; set; }
        public string TargetDb { get; set; }
        public string TableName { get; set; }
    }
}
