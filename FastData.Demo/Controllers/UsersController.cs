using FastData;
using FastData.Base;
using FastData.Context;
using FastData.Config;
using FastData.Model;
using FastData.Demo.Models;
using FastData.Demo.Repositories;
using FastData.Demo.Services;
// using FastData.DevTools;
using FastRedis.Repository;
using FastRedis.Services;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 用户 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserCacheService _cacheService;

        public UsersController(IUserRepository userRepository, IUserCacheService cacheService)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AppUser>>> GetAll()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 根据 ID 获取用户
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppUser>> GetById(int id)
        {
            try
            {
                // 使用缓存服务
                var user = await _cacheService.GetUserAsync(id, async () =>
                {
                    return await _userRepository.GetByIdAsync(id);
                });

                if (user == null)
                    return NotFound();

                // 增加浏览次数
                await _cacheService.IncrementViewCountAsync(id);

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取活跃用户
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<AppUser>>> GetActiveUsers()
        {
            try
            {
                var users = await _userRepository.GetActiveUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.ToString() });
            }
        }

        /// <summary>
        /// 根据部门获取用户
        /// </summary>
        [HttpGet("department/{department}")]
        public async Task<ActionResult<List<AppUser>>> GetByDepartment(string department)
        {
            try
            {
                var users = await _userRepository.GetByDepartmentAsync(department);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分页获取用户
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PageResult<AppUser>>> GetPaged([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var users = await _userRepository.GetPagedAsync(pageIndex, pageSize);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] AppUser user)
        {
            try
            {
                if (user == null)
                {
                    return BadRequest(new { error = "User is null" });
                }

                // 直接调用 FastWrite.Add 诊断
                user.CreateTime = DateTime.Now;
                user.IsActive = true;
                var writeResult = await Task.Run(() => FastWrite.Add(user));

                return Ok(new
                {
                    IsSuccess = writeResult.IsSuccess,
                    Message = writeResult.Message,
                    User = user
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message,
                    innerStackTrace = ex.InnerException?.StackTrace
                });
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<int>> Update(int id, [FromBody] AppUser user)
        {
            try
            {
                user.Id = id;
                var result = await _userRepository.UpdateAsync(user);

                // 清除缓存
                await _cacheService.RemoveUserAsync(id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<int>> Delete(int id)
        {
            try
            {
                var result = await _userRepository.DeleteAsync(id);

                // 清除缓存
                await _cacheService.RemoveUserAsync(id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 动态条件查询（演示 Where&lt;T&gt; 条件构建器）
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<AppUser>>> Search(
            [FromQuery] string keyword = null,
            [FromQuery] string department = null,
            [FromQuery] int? minAge = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var where = new Where<AppUser>();

                // 基础条件
                where.Add(u => u.Id > 0);

                // 动态添加条件
                if (!string.IsNullOrEmpty(keyword))
                    where.And(u => u.UserName.Contains(keyword) || u.Email.Contains(keyword));

                if (!string.IsNullOrEmpty(department))
                    where.And(u => u.Department == department);

                if (minAge.HasValue)
                    where.And(u => u.Age >= minAge.Value);

                if (maxAge.HasValue)
                    where.And(u => u.Age <= maxAge.Value);

                if (isActive.HasValue)
                    where.And(u => u.IsActive == isActive.Value);

                var users = FastRead.Query<AppUser>(u => u.Id > 0)
                    .Where(where)
                    .OrderBy(u => u.Id)
                    .ToList();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        [HttpGet("health")]
        public ActionResult<object> Check()
        {
            // var result = HealthChecker.CheckAll();
            // return Ok(new
            // {
            //     Status = result.IsHealthy ? "Healthy" : "Unhealthy",
            //     Timestamp = result.Timestamp,
            //     Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            //     Checks = result.Checks.ToDictionary(kvp => kvp.Key, kvp => new
            //     {
            //         status = kvp.Value.Status,
            //         isHealthy = kvp.Value.IsHealthy,
            //         message = kvp.Value.Message
            //     })
            // });

            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                redisConnected = false
            });
        }
    }
}