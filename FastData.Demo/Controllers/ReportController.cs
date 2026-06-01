using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FastData;
using FastData.Demo.Models;
using FastData.Model;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 报表统计控制器
    /// 覆盖 ORM 功能：GroupBy/Join/聚合/ToJson/ToDics/ToDataTable
    /// </summary>
    [ApiController]
    [Route("api/Report")]
    public class ReportController : ControllerBase
    {
        /// <summary>
        /// 用户统计报表（GroupBy + 聚合）
        /// </summary>
        [HttpGet("user-stats")]
        public IActionResult GetUserStats()
        {
            // 按部门分组统计
            var deptStats = FastRead.Query<AppUser>(u => u.Id > 0)
                .GroupBy<AppUser>(u => u.Department)
                .ToDics();

            // 按状态分组统计
            var statusStats = FastRead.Query<AppUser>(u => u.Id > 0)
                .GroupBy<AppUser>(u => u.IsActive)
                .ToDics();

            return Ok(ApiResponse<object>.Ok(new
            {
                DepartmentStats = deptStats,
                StatusStats = statusStats
            }));
        }

        /// <summary>
        /// 订单统计报表（GroupBy + 聚合）
        /// </summary>
        [HttpGet("order-stats")]
        public IActionResult GetOrderStats()
        {
            // 按状态分组统计
            var statusReport = FastRead.Query<AppOrder>(o => o.Id > 0)
                .GroupBy<AppOrder>(o => o.Status)
                .ToDics();

            // 按用户分组统计消费金额
            var userReport = FastRead.Query<AppOrder>(o => o.Status >= 1)
                .GroupBy<AppOrder>(o => o.UserId)
                .ToDics();

            return Ok(ApiResponse<object>.Ok(new
            {
                StatusReport = statusReport,
                UserReport = userReport
            }));
        }

        /// <summary>
        /// 月度趋势报表（原生 SQL 聚合）
        /// </summary>
        [HttpGet("monthly-trend")]
        public IActionResult GetMonthlyTrend()
        {
            var monthlyReport = FastRead.ExecuteSql(@"
                SELECT 
                    YEAR(CreateTime) as Year,
                    MONTH(CreateTime) as Month,
                    COUNT(*) as OrderCount,
                    SUM(TotalAmount) as TotalAmount,
                    AVG(TotalAmount) as AvgAmount
                FROM AppOrder 
                WHERE Status >= 1
                GROUP BY YEAR(CreateTime), MONTH(CreateTime)
                ORDER BY Year DESC, Month DESC", null);

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(monthlyReport));
        }

        /// <summary>
        /// 用户订单报表（Join 查询）
        /// </summary>
        [HttpGet("user-order-report")]
        public IActionResult GetUserOrderReport()
        {
            // 使用原生 SQL 做 Join 查询
            var report = FastRead.ExecuteSql(@"
                SELECT 
                    u.UserName,
                    u.Department,
                    COUNT(o.Id) as OrderCount,
                    SUM(o.TotalAmount) as TotalAmount,
                    MAX(o.CreateTime) as LastOrderTime
                FROM AppUser u
                LEFT JOIN AppOrder o ON u.Id = o.UserId
                WHERE u.IsActive = 1
                GROUP BY u.UserName, u.Department
                ORDER BY TotalAmount DESC", null);

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(report));
        }

        /// <summary>
        /// 导出为 JSON
        /// </summary>
        [HttpGet("export/json")]
        public IActionResult ExportJson()
        {
            var users = FastRead.Query<AppUser>(u => u.IsActive)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// 导出为 DataTable
        /// </summary>
        [HttpGet("export/datatable")]
        public IActionResult ExportDataTable()
        {
            var dt = FastRead.Query<AppUser>(u => u.IsActive)
                .ToDataTable();

            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                rows.Add(dict);
            }

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(rows));
        }

        /// <summary>
        /// 导出为字典列表
        /// </summary>
        [HttpGet("export/dics")]
        public IActionResult ExportDics()
        {
            var dics = FastRead.Query<AppUser>(u => u.IsActive)
                .ToDics();

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(dics));
        }

        /// <summary>
        /// 投影查询（Select）
        /// </summary>
        [HttpGet("projection")]
        public IActionResult GetProjection()
        {
            // 投影查询：只取部分字段
            var users = FastRead.Query<AppUser>(u => u.IsActive)
                .ToList<AppUser>();

            var projections = users.Select(u => new { u.UserName, u.Email, u.Department }).ToList();

            return Ok(ApiResponse<object>.Ok(projections));
        }

        /// <summary>
        /// 聚合统计（Count/Sum/Avg）
        /// </summary>
        [HttpGet("aggregate")]
        public IActionResult GetAggregate()
        {
            // 用户统计
            var totalUsers = FastRead.Query<AppUser>(u => u.Id > 0).ToCount();
            var activeUsers = FastRead.Query<AppUser>(u => u.IsActive).ToCount();

            // 订单统计
            var totalOrders = FastRead.Query<AppOrder>(o => o.Id > 0).ToCount();
            var paidOrders = FastRead.Query<AppOrder>(o => o.Status >= 1).ToCount();

            // 使用原生 SQL 做聚合
            var amountStats = FastRead.ExecuteSql(@"
                SELECT 
                    COUNT(*) as TotalOrders,
                    SUM(TotalAmount) as TotalAmount,
                    AVG(TotalAmount) as AvgAmount,
                    MIN(TotalAmount) as MinAmount,
                    MAX(TotalAmount) as MaxAmount
                FROM AppOrder 
                WHERE Status >= 1", null);

            return Ok(ApiResponse<object>.Ok(new
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalOrders = totalOrders,
                PaidOrders = paidOrders,
                AmountStats = amountStats.FirstOrDefault()
            }));
        }

        /// <summary>
        /// 分组聚合报表（GroupBy + ToJson）
        /// </summary>
        [HttpGet("groupby-json")]
        public IActionResult GetGroupByJson()
        {
            // 按部门分组并转 JSON
            var deptJson = FastRead.Query<AppUser>(u => u.Id > 0)
                .GroupBy<AppUser>(u => u.Department)
                .ToJson();

            return Ok(ApiResponse<string>.Ok(deptJson));
        }

        /// <summary>
        /// 分页统计报表
        /// </summary>
        [HttpGet("paged-report")]
        public IActionResult GetPagedReport([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var result = FastRead.Query<AppOrder>(o => o.Status >= 1)
                .OrderByDescending(o => o.CreateTime)
                .ToPage<AppOrder>(new PageModel { PageId = page, PageSize = size });

            return Ok(ApiResponse<object>.Ok(new
            {
                Data = result.list,
                Total = result.pModel.TotalRecord,
                Page = result.pModel.PageId,
                PageSize = result.pModel.PageSize
            }));
        }
    }
}
