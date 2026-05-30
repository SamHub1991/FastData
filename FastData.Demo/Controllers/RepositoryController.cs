using FastData.Demo.Models;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepositoryController : ControllerBase
    {
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive)
                    .OrderBy<AppUser>(u => u.Id)
                    .ToList<AppUser>();

                result["success"] = true;
                result["count"] = users.Count;
                result["data"] = users;
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        [HttpGet("users/{id}")]
        public IActionResult GetUserById(int id)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var user = FastRead.Query<AppUser>(u => u.Id == id)
                    .ToItem<AppUser>();

                if (user != null)
                {
                    result["success"] = true;
                    result["data"] = user;
                }
                else
                {
                    result["success"] = false;
                    result["message"] = "用户不存在";
                }
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        [HttpPost("users")]
        public IActionResult CreateUser([FromBody] AppUser user)
        {
            try
            {
                user.IsActive = true;
                user.CreateTime = DateTime.Now;

                var writeResult = FastWrite.Add(user);

                return Ok(writeResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] AppUser user)
        {
            try
            {
                var existingUser = FastRead.Query<AppUser>(u => u.Id == id)
                    .ToItem<AppUser>();

                if (existingUser == null)
                {
                    return Ok(new { success = false, message = "用户不存在" });
                }

                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.Age = user.Age;
                existingUser.UpdateTime = DateTime.Now;

                var writeResult = FastWrite.Update(existingUser);

                return Ok(writeResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var user = FastRead.Query<AppUser>(u => u.Id == id)
                    .ToItem<AppUser>();

                if (user == null)
                {
                    return Ok(new { success = false, message = "用户不存在" });
                }

                user.IsActive = false;
                user.UpdateTime = DateTime.Now;

                var writeResult = FastWrite.Update(user);

                return Ok(writeResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users/page/{page}/{pageSize}")]
        public IActionResult GetUsersByPage(int page, int pageSize)
        {
            try
            {
                var pagination = FastRead.Query<AppUser>(u => u.IsActive)
                    .OrderBy<AppUser>(u => u.Id)
                    .ToPagination<AppUser>(page, pageSize);

                return Ok(pagination);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["feature"] = "Repository 模式",
                ["description"] = "封装数据访问逻辑，提供统一的接口",
                ["advantages"] = new[]
                {
                    "分离业务逻辑和数据访问",
                    "便于单元测试",
                    "统一数据访问接口",
                    "支持多种数据库"
                },
                ["usage"] = new Dictionary<string, string>
                {
                    ["查询"] = "FastRead.Query<T>(u => u.IsActive).ToList<T>()",
                    ["新增"] = "FastWrite.Add(entity) 返回 WriteReturn",
                    ["更新"] = "FastWrite.Update(entity) 返回 WriteReturn",
                    ["删除"] = "FastWrite.Delete<T>(where) 返回 WriteReturn",
                    ["分页"] = "FastRead.Query<T>(...).ToPagination<T>(page, pageSize)"
                }
            };

            return Ok(info);
        }
    }
}
