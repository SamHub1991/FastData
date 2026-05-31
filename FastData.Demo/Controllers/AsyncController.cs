using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastData;
using FastData.Context;
using FastData.Demo.Models;
using FastData.Model;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 异步并发控制器
    /// 覆盖 ORM 功能：AddAsy/UpdateAsy/DeleteAsy/QueryAsy/ToPageAsy/CountAsy
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AsyncController : ControllerBase
    {
        /// <summary>
        /// 异步查询用户列表
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAsync()
        {
            var users = await FastRead.Query<AppUser>(u => u.IsActive)
                .ToListAsync<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// 异步分页查询
        /// </summary>
        [HttpGet("users/paged")]
        public IActionResult GetUsersPaged([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var result = FastRead.Query<AppUser>(u => u.IsActive)
                .OrderBy(u => u.Id)
                .ToPage<AppUser>(new PageModel { PageId = page, PageSize = size });

            return Ok(ApiResponse<object>.Ok(new
            {
                Data = result.list,
                Total = result.pModel.TotalRecord,
                Page = result.pModel.PageId,
                PageSize = result.pModel.PageSize
            }));
        }

        /// <summary>
        /// 异步计数
        /// </summary>
        [HttpGet("users/count")]
        public async Task<IActionResult> GetUserCountAsync()
        {
            var total = FastRead.Query<AppUser>(u => u.Id > 0).ToCount();
            var active = FastRead.Query<AppUser>(u => u.IsActive).ToCount();

            return Ok(ApiResponse<object>.Ok(new { Total = total, Active = active }));
        }

        /// <summary>
        /// 异步添加用户
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> AddUserAsync([FromBody] AppUser user)
        {
            user.CreateTime = DateTime.Now;
            user.IsActive = true;

            var result = await FastWrite.AddAsy(user);

            if (result.IsSuccess)
                return Ok(ApiResponse.Ok("添加成功"));
            else
                return Ok(ApiResponse.Fail(result.Message));
        }

        /// <summary>
        /// 异步批量添加
        /// </summary>
        [HttpPost("users/batch")]
        public async Task<IActionResult> BatchAddUsersAsync([FromBody] List<AppUser> users)
        {
            var results = new List<bool>();
            foreach (var user in users)
            {
                user.CreateTime = DateTime.Now;
                user.IsActive = true;
                var result = await FastWrite.AddAsy(user);
                results.Add(result.IsSuccess);
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                Total = users.Count,
                Success = results.Count(r => r),
                Failed = results.Count(r => !r)
            }));
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("users/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] AppUser user)
        {
            user.Id = id;
            user.UpdateTime = DateTime.Now;

            var result = FastWrite.Update(user);

            if (result.IsSuccess)
                return Ok(ApiResponse.Ok("更新成功"));
            else
                return Ok(ApiResponse.Fail(result.Message));
        }

        /// <summary>
        /// 异步删除用户
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUserAsync(int id)
        {
            var result = await FastWrite.DeleteAsy<AppUser>(u => u.Id == id);

            if (result.IsSuccess)
                return Ok(ApiResponse.Ok("删除成功"));
            else
                return Ok(ApiResponse.Fail(result.Message));
        }

        /// <summary>
        /// 并发查询（多个任务同时执行）
        /// </summary>
        [HttpGet("concurrent")]
        public IActionResult ConcurrentQuery()
        {
            // 同时执行多个查询
            var users = FastRead.Query<AppUser>(u => u.IsActive).ToList<AppUser>();
            var orders = FastRead.Query<AppOrder>(o => o.Id > 0).ToList<AppOrder>();
            var userCount = FastRead.Query<AppUser>(u => u.Id > 0).ToCount();
            var orderCount = FastRead.Query<AppOrder>(o => o.Id > 0).ToCount();

            return Ok(ApiResponse<object>.Ok(new
            {
                Users = users,
                Orders = orders,
                UserCount = userCount,
                OrderCount = orderCount
            }));
        }

        /// <summary>
        /// 并发写入（批量插入）
        /// </summary>
        [HttpPost("concurrent-write")]
        public IActionResult ConcurrentWrite([FromQuery] int count = 10)
        {
            var results = new List<bool>();

            for (int i = 0; i < count; i++)
            {
                var user = new AppUser
                {
                    UserName = $"批量用户_{i}",
                    Email = $"batch_{i}@example.com",
                    Age = 20 + i % 30,
                    Department = i % 2 == 0 ? "技术部" : "市场部",
                    Salary = 5000 + i * 100,
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                var result = FastWrite.Add(user);
                results.Add(result.IsSuccess);
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                Total = count,
                Success = results.Count(r => r),
                Failed = results.Count(r => !r)
            }));
        }

        /// <summary>
        /// 事务操作
        /// </summary>
        [HttpPost("transaction")]
        public IActionResult Transaction()
        {
            using var db = new DataContext("db1");
            db.BeginTrans();

            try
            {
                // 添加用户
                var user = new AppUser
                {
                    UserName = "事务用户",
                    Email = "trans@example.com",
                    Age = 25,
                    Department = "技术部",
                    Salary = 8000,
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                var addResult = FastWrite.Add(user, db);
                if (!addResult.IsSuccess)
                {
                    db.RollbackTrans();
                    return Ok(ApiResponse.Fail("添加用户失败"));
                }

                // 添加订单
                var order = new AppOrder
                {
                    OrderNo = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                    UserId = 1,
                    ProductName = "测试商品",
                    Quantity = 1,
                    UnitPrice = 99.99m,
                    TotalAmount = 99.99m,
                    Status = 0,
                    CreateTime = DateTime.Now
                };

                var orderResult = FastWrite.Add(order, db);
                if (!orderResult.IsSuccess)
                {
                    db.RollbackTrans();
                    return Ok(ApiResponse.Fail("添加订单失败"));
                }

                db.SubmitTrans();
                return Ok(ApiResponse.Ok("事务提交成功"));
            }
            catch (Exception ex)
            {
                db.RollbackTrans();
                return Ok(ApiResponse.Fail($"事务异常: {ex.Message}"));
            }
        }

        /// <summary>
        /// 查询单条
        /// </summary>
        [HttpGet("users/{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = FastRead.Query<AppUser>(u => u.Id == id)
                .FirstOrDefault<AppUser>();

            if (user == null)
                return Ok(ApiResponse<AppUser>.NotFound());

            return Ok(ApiResponse<AppUser>.Ok(user));
        }

        /// <summary>
        /// 查询第一条
        /// </summary>
        [HttpGet("users/first")]
        public IActionResult GetFirstUser()
        {
            var user = FastRead.Query<AppUser>(u => u.IsActive)
                .OrderBy(u => u.Id)
                .FirstOrDefault<AppUser>();

            return Ok(ApiResponse<AppUser>.Ok(user));
        }
    }
}
