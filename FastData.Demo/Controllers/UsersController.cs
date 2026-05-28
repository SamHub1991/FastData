using FastData;
using FastData.Base;
using FastData.Demo.Models;
using FastData.Demo.Repositories;
using FastData.Demo.Services;
using FastData.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 用户 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
                var users = await _cacheService.GetActiveUsersAsync(async () =>
                {
                    return await _userRepository.GetActiveUsersAsync();
                });
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
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
        public async Task<ActionResult<List<AppUser>>> GetPaged([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
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
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
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

                var users = FastRead.Query<AppUser>(u => true)
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
    }

    /// <summary>
    /// 订单 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICacheService _cacheService;

        public OrdersController(IOrderRepository orderRepository, ICacheService cacheService)
        {
            _orderRepository = orderRepository;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 获取所有订单
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AppOrder>>> GetAll()
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 根据 ID 获取订单
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AppOrder>> GetById(int id)
        {
            try
            {
                var key = $"order:{id}";
                var order = await _cacheService.GetOrSetAsync(key, async () =>
                {
                    return await _orderRepository.GetByIdAsync(id);
                }, hours: 2);

                if (order == null)
                    return NotFound();

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取用户订单
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<AppOrder>>> GetByUserId(int userId)
        {
            try
            {
                var key = $"orders:user:{userId}";
                var orders = await _cacheService.GetOrSetAsync(key, async () =>
                {
                    return await _orderRepository.GetByUserIdAsync(userId);
                }, hours: 1);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] AppOrder order)
        {
            try
            {
                var result = await _orderRepository.AddAsync(order);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 更新订单状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<int>> UpdateStatus(int id, [FromQuery] int status)
        {
            try
            {
                var result = await _orderRepository.UpdateStatusAsync(id, status);

                // 清除缓存
                await _cacheService.RemoveAsync($"order:{id}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 数据同步 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IDataSyncService _syncService;

        public SyncController(IDataSyncService syncService)
        {
            _syncService = syncService;
        }

        /// <summary>
        /// 同步所有表
        /// </summary>
        [HttpPost("all")]
        public async Task<ActionResult<SyncResult>> SyncAll()
        {
            try
            {
                var result = await _syncService.SyncAllTablesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 同步用户表
        /// </summary>
        [HttpPost("users")]
        public async Task<ActionResult<SyncResult>> SyncUsers()
        {
            try
            {
                var result = await _syncService.SyncUsersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 同步订单表
        /// </summary>
        [HttpPost("orders")]
        public async Task<ActionResult<SyncResult>> SyncOrders()
        {
            try
            {
                var result = await _syncService.SyncOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 健康检查 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ICacheService _cacheService;

        public HealthController(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Check()
        {
            var result = new
            {
                Status = "Healthy",
                Timestamp = DateTime.Now,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                RedisConnected = await _cacheService.ExistsAsync("health:check")
            };

            // 写入健康检查标记
            await _cacheService.SetAsync("health:check", new { Timestamp = DateTime.Now }, hours: 1);

            return Ok(result);
        }
    }
}
