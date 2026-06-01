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
    /// 数据导出控制器（修复版）
    /// </summary>
    [ApiController]
    [Route("api/DataExport")]
    public class DataExportController : ControllerBase
    {
        /// <summary>
        /// 导出为字典列表
        /// </summary>
        [HttpGet("dics")]
        public IActionResult ExportAsDics([FromQuery] string department = null)
        {
            try
            {
                var query = FastRead.Query<AppUser>(u => u.IsActive);
                if (!string.IsNullOrEmpty(department))
                    query = query.Where(u => u.Department.Contains(department));
                
                var users = query.Take(100).ToList<AppUser>();
                var dics = users.Select(u => new Dictionary<string, object>
                {
                    { "Id", u.Id },
                    { "UserName", u.UserName },
                    { "Email", u.Email },
                    { "Department", u.Department }
                }).ToList();
                
                return Ok(new { count = dics.Count, data = dics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 导出为 DataTable
        /// </summary>
        [HttpGet("datatable")]
        public IActionResult ExportAsDataTable()
        {
            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive).Take(100).ToList<AppUser>();
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("UserName", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Department", typeof(string));
                
                foreach (var user in users)
                {
                    dt.Rows.Add(user.Id, user.UserName, user.Email, user.Department);
                }
                
                return Ok(new { count = dt.Rows.Count, data = dt.AsEnumerable().Select(r => new {
                    Id = r.Field<int>("Id"),
                    UserName = r.Field<string>("UserName"),
                    Email = r.Field<string>("Email"),
                    Department = r.Field<string>("Department")
                }).ToList() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 导出为数组
        /// </summary>
        [HttpGet("array")]
        public IActionResult ExportAsArray()
        {
            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive).Take(100).ToList<AppUser>();
                var names = users.Select(u => u.UserName).ToArray();
                return Ok(new { count = names.Length, data = names });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 投影导出
        /// </summary>
        [HttpGet("projection")]
        public IActionResult ExportProjection()
        {
            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive)
                    .Take(100)
                    .ToList<AppUser>()
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToList();
                return Ok(new { count = users.Count, data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 导出订单
        /// </summary>
        [HttpGet("orders")]
        public IActionResult ExportOrders()
        {
            try
            {
                var orders = FastRead.Query<AppOrder>(o => o.Id > 0)
                    .Take(100)
                    .ToList<AppOrder>();
                return Ok(new { count = orders.Count, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 导出为 JSON
        /// </summary>
        [HttpGet("json")]
        public IActionResult ExportAsJson()
        {
            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive).Take(100).ToList<AppUser>();
                return Ok(new { count = users.Count, data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 导出统计
        /// </summary>
        [HttpGet("stats")]
        public IActionResult ExportStats()
        {
            try
            {
                var totalUsers = FastRead.Query<AppUser>(u => u.Id > 0).ToList().Count;
                var activeUsers = FastRead.Query<AppUser>(u => u.IsActive).ToList().Count;
                var totalOrders = FastRead.Query<AppOrder>(o => o.Id > 0).ToList().Count;
                
                return Ok(new
                {
                    totalUsers,
                    activeUsers,
                    totalOrders,
                    activeRate = totalUsers > 0 ? (double)activeUsers / totalUsers * 100 : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
