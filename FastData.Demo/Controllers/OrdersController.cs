using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastData.Demo.Models;
using FastData.Demo.Repositories;
using FastData.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 订单 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/orders")]
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
        public async Task<ActionResult<List<AppOrder>>> GetAllOrders()
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
}
