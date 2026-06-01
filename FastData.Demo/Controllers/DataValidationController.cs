using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Demo.Models;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 数据校验控制器
    /// 覆盖 ORM 功能：NullSafety/字段验证/异常处理/WriteReturn
    /// </summary>
    [ApiController]
    [Route("api/DataValidation")]
    public class DataValidationController : ControllerBase
    {
        /// <summary>
        /// 空值安全查询
        /// </summary>
        [HttpGet("null-safety")]
        public IActionResult NullSafety()
        {
            // 空值安全：FirstOrDefault 可能返回 null
            var user = FastRead.Query<AppUser>(u => u.Id == 999999)
                .FirstOrDefault<AppUser>();

            // 空值安全处理
            var result = new
            {
                IsNull = user == null,
                UserName = user?.UserName ?? "未找到",
                Email = user?.Email ?? "N/A",
                Age = user?.Age ?? 0
            };

            return Ok(ApiResponse<object>.Ok(result));
        }

        /// <summary>
        /// 批量空值安全查询
        /// </summary>
        [HttpGet("null-safety-batch")]
        public IActionResult NullSafetyBatch()
        {
            var ids = new List<int> { 1, 2, 999999 };
            var users = new List<AppUser>();

            foreach (var id in ids)
            {
                var user = FastRead.Query<AppUser>(u => u.Id == id)
                    .FirstOrDefault<AppUser>();

                if (user != null)
                    users.Add(user);
            }

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// 字段验证 - 添加用户
        /// </summary>
        [HttpPost("validate-add")]
        public IActionResult ValidateAndAdd([FromBody] AppUser user)
        {
            // 字段验证
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(user.UserName))
                errors.Add("用户名不能为空");

            if (string.IsNullOrWhiteSpace(user.Email))
                errors.Add("邮箱不能为空");
            else if (!user.Email.Contains("@"))
                errors.Add("邮箱格式不正确");

            if (user.Age < 0 || user.Age > 150)
                errors.Add("年龄范围不正确");

            if (user.Salary < 0)
                errors.Add("薪资不能为负数");

            if (errors.Count > 0)
                return Ok(ApiResponse.Fail(string.Join("; ", errors)));

            // 验证通过，添加数据
            user.CreateTime = DateTime.Now;
            user.IsActive = true;

            var result = FastWrite.Add(user);

            if (result.IsSuccess)
                return Ok(ApiResponse.Ok("添加成功"));
            else
                return Ok(ApiResponse.Fail(result.Message));
        }

        /// <summary>
        /// 字段验证 - 更新用户
        /// </summary>
        [HttpPut("validate-update/{id}")]
        public IActionResult ValidateAndUpdate(int id, [FromBody] AppUser user)
        {
            // 验证用户是否存在
            var existing = FastRead.Query<AppUser>(u => u.Id == id)
                .FirstOrDefault<AppUser>();

            if (existing == null)
                return Ok(ApiResponse.NotFound("用户不存在"));

            // 字段验证
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(user.Email) && !user.Email.Contains("@"))
                errors.Add("邮箱格式不正确");

            if (user.Age < 0 || user.Age > 150)
                errors.Add("年龄范围不正确");

            if (user.Salary < 0)
                errors.Add("薪资不能为负数");

            if (errors.Count > 0)
                return Ok(ApiResponse.Fail(string.Join("; ", errors)));

            // 更新数据（只更新非空字段）
            if (!string.IsNullOrWhiteSpace(user.UserName))
                existing.UserName = user.UserName;
            if (!string.IsNullOrWhiteSpace(user.Email))
                existing.Email = user.Email;
            if (user.Age > 0)
                existing.Age = user.Age;
            if (user.Salary > 0)
                existing.Salary = user.Salary;
            if (!string.IsNullOrWhiteSpace(user.Department))
                existing.Department = user.Department;

            existing.UpdateTime = DateTime.Now;

            var result = FastWrite.Update(existing);

            if (result.IsSuccess)
                return Ok(ApiResponse.Ok("更新成功"));
            else
                return Ok(ApiResponse.Fail(result.Message));
        }

        /// <summary>
        /// 异常处理 - 安全查询
        /// </summary>
        [HttpGet("safe-query")]
        public IActionResult SafeQuery([FromQuery] int? id = null)
        {
            try
            {
                if (!id.HasValue)
                    return Ok(ApiResponse.Fail("请提供用户 ID"));

                var user = FastRead.Query<AppUser>(u => u.Id == id.Value)
                    .FirstOrDefault<AppUser>();

                if (user == null)
                    return Ok(ApiResponse<AppUser>.NotFound("用户不存在"));

                return Ok(ApiResponse<AppUser>.Ok(user));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Fail($"查询异常: {ex.Message}"));
            }
        }

        /// <summary>
        /// 异常处理 - 安全写入
        /// </summary>
        [HttpPost("safe-write")]
        public IActionResult SafeWrite([FromBody] AppUser user)
        {
            try
            {
                // 空值检查
                if (user == null)
                    return Ok(ApiResponse.Fail("请求数据不能为空"));

                // 字段验证
                if (string.IsNullOrWhiteSpace(user.UserName))
                    return Ok(ApiResponse.Fail("用户名不能为空"));

                // 写入数据
                user.CreateTime = DateTime.Now;
                user.IsActive = true;

                var result = FastWrite.Add(user);

                if (result.IsSuccess)
                    return Ok(ApiResponse.Ok("写入成功"));
                else
                    return Ok(ApiResponse.Fail(result.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Fail($"写入异常: {ex.Message}"));
            }
        }

        /// <summary>
        /// 批量写入异常处理
        /// </summary>
        [HttpPost("safe-batch-write")]
        public IActionResult SafeBatchWrite([FromBody] List<AppUser> users)
        {
            if (users == null || users.Count == 0)
                return Ok(ApiResponse.Fail("数据列表不能为空"));

            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var user in users)
            {
                try
                {
                    // 验证
                    if (string.IsNullOrWhiteSpace(user.UserName))
                    {
                        results.Add(new { UserName = user.UserName, Success = false, Message = "用户名不能为空" });
                        failCount++;
                        continue;
                    }

                    // 写入
                    user.CreateTime = DateTime.Now;
                    user.IsActive = true;

                    var result = FastWrite.Add(user);

                    if (result.IsSuccess)
                    {
                        results.Add(new { UserName = user.UserName, Success = true, Message = "添加成功" });
                        successCount++;
                    }
                    else
                    {
                        results.Add(new { UserName = user.UserName, Success = false, Message = result.Message });
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { UserName = user.UserName, Success = false, Message = ex.Message });
                    failCount++;
                }
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                Total = users.Count,
                Success = successCount,
                Failed = failCount,
                Details = results
            }));
        }

        /// <summary>
        /// WriteReturn 信息展示
        /// </summary>
        [HttpGet("write-return")]
        public IActionResult WriteReturnDemo()
        {
            // 添加用户
            var user = new AppUser
            {
                UserName = "WriteReturn测试",
                Email = "test@example.com",
                Age = 25,
                Department = "技术部",
                Salary = 8000,
                IsActive = true,
                CreateTime = DateTime.Now
            };

            var addResult = FastWrite.Add(user);

            var info = new
            {
                IsSuccess = addResult.IsSuccess,
                Message = addResult.Message
            };

            return Ok(ApiResponse<object>.Ok(info));
        }

        /// <summary>
        /// 空值安全 - 集合操作
        /// </summary>
        [HttpGet("null-safety-collection")]
        public IActionResult NullSafetyCollection()
        {
            // 查询可能为空的列表
            var users = FastRead.Query<AppUser>(u => u.Department == "不存在的部门")
                .ToList<AppUser>();

            // 空值安全处理
            var result = new
            {
                IsNull = users == null,
                IsEmpty = users?.Count == 0,
                Count = users?.Count ?? 0,
                FirstOrDefault = users?.FirstOrDefault()?.UserName ?? "无数据"
            };

            return Ok(ApiResponse<object>.Ok(result));
        }

        /// <summary>
        /// 数据完整性验证
        /// </summary>
        [HttpPost("integrity-check")]
        public IActionResult IntegrityCheck([FromBody] AppOrder order)
        {
            var errors = new List<string>();

            // 订单号验证
            if (string.IsNullOrWhiteSpace(order.OrderNo))
                errors.Add("订单号不能为空");

            // 用户验证
            var user = FastRead.Query<AppUser>(u => u.Id == order.UserId)
                .FirstOrDefault<AppUser>();
            if (user == null)
                errors.Add("用户不存在");
            else if (!user.IsActive)
                errors.Add("用户已禁用");

            // 商品验证
            if (string.IsNullOrWhiteSpace(order.ProductName))
                errors.Add("商品名称不能为空");

            // 数量验证
            if (order.Quantity <= 0)
                errors.Add("数量必须大于 0");

            // 金额验证
            if (order.UnitPrice <= 0)
                errors.Add("单价必须大于 0");

            if (order.TotalAmount <= 0)
                errors.Add("总金额必须大于 0");

            if (order.TotalAmount != order.Quantity * order.UnitPrice)
                errors.Add("总金额与数量*单价不匹配");

            if (errors.Count > 0)
                return Ok(ApiResponse.Fail(string.Join("; ", errors)));

            return Ok(ApiResponse.Ok("验证通过"));
        }
    }
}
