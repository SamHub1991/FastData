using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FastData;
using FastData.Demo.Models;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 数据导出控制器
    /// 覆盖 ORM 功能：ToDics/ToDataTable/ToArray/投影/ToJson
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataExportController : ControllerBase
    {
        /// <summary>
        /// 导出为字典列表（ToDics）
        /// </summary>
        [HttpGet("dics")]
        public IActionResult ExportAsDics([FromQuery] string department = null)
        {
            var query = FastRead.Query<AppUser>(u => u.IsActive);

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(u => u.Department == department);
            }

            var dics = query.ToDics();

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(dics));
        }

        /// <summary>
        /// 导出为 DataTable（ToDataTable）
        /// </summary>
        [HttpGet("datatable")]
        public IActionResult ExportAsDataTable()
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
        /// 导出为数组（ToArray）
        /// </summary>
        [HttpGet("array")]
        public IActionResult ExportAsArray()
        {
            var users = FastRead.Query<AppUser>(u => u.IsActive)
                .ToList<AppUser>();

            var names = users.Select(u => u.UserName).ToArray();

            return Ok(ApiResponse<string[]>.Ok(names));
        }

        /// <summary>
        /// 投影导出（Select 部分字段）
        /// </summary>
        [HttpGet("projection")]
        public IActionResult ExportProjection()
        {
            var users = FastRead.Query<AppUser>(u => u.IsActive)
                .ToList<AppUser>();

            var projections = users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.Department
            }).ToList();

            return Ok(ApiResponse<object>.Ok(projections));
        }

        /// <summary>
        /// 订单导出（关联查询）
        /// </summary>
        [HttpGet("orders")]
        public IActionResult ExportOrders([FromQuery] int? status = null)
        {
            var query = FastRead.Query<AppOrder>(o => o.Id > 0);

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = query.OrderByDescending(o => o.CreateTime)
                .ToList<AppOrder>();

            return Ok(ApiResponse<List<AppOrder>>.Ok(orders));
        }

        /// <summary>
        /// 导出为 JSON 字符串（ToJson）
        /// </summary>
        [HttpGet("json")]
        public IActionResult ExportAsJson()
        {
            var json = FastRead.Query<AppUser>(u => u.IsActive)
                .ToJson();

            return Ok(ApiResponse<string>.Ok(json));
        }

        /// <summary>
        /// 分页导出
        /// </summary>
        [HttpGet("paged")]
        public IActionResult ExportPaged([FromQuery] int page = 1, [FromQuery] int size = 100)
        {
            var pModel = new PageModel { PageId = page, PageSize = size };
            var result = FastRead.Query<AppUser>(u => u.IsActive)
                .OrderBy(u => u.Id)
                .ToPage<AppUser>(pModel);

            return Ok(ApiResponse<PageResult<AppUser>>.Ok(result));
        }

        /// <summary>
        /// 字典导出（ToDictionary）
        /// </summary>
        [HttpGet("dictionary")]
        public IActionResult ExportAsDictionary()
        {
            var dict = FastRead.Query<AppUser>(u => u.IsActive)
                .ToDictionary(u => u.Id, u => u.UserName);

            return Ok(ApiResponse<Dictionary<int, string>>.Ok(dict));
        }

        /// <summary>
        /// 统计导出（聚合）
        /// </summary>
        [HttpGet("stats")]
        public IActionResult ExportStats()
        {
            var stats = FastRead.ExecuteSql(@"
                SELECT 
                    Department,
                    COUNT(*) as UserCount,
                    AVG(Salary) as AvgSalary,
                    MIN(Salary) as MinSalary,
                    MAX(Salary) as MaxSalary
                FROM AppUser
                WHERE IsActive = 1
                GROUP BY Department
                ORDER BY UserCount DESC", null);

            return Ok(ApiResponse<List<Dictionary<string, object>>>.Ok(stats));
        }
    }
}
