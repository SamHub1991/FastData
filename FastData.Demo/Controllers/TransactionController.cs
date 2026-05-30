using System;
using FastData;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 事务操作示例
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        /// <summary>
        /// 事务示例：批量创建订单
        /// </summary>
        /// <remarks>
        /// 演示 FastWrite 事务用法：
        /// 
        /// 注意：当前版本暂不支持事务操作，此接口仅作为示例。
        /// </remarks>
        [HttpPost("batch-orders")]
        public IActionResult CreateBatchOrders([FromBody] BatchOrderRequest request)
        {
            return Ok(new
            {
                Success = false,
                Message = "当前版本暂不支持事务操作"
            });
        }

        /// <summary>
        /// 事务示例：转账操作
        /// </summary>
        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] TransferRequest request)
        {
            return Ok(new
            {
                Success = false,
                Message = "当前版本暂不支持事务操作"
            });
        }

        /// <summary>
        /// 事务示例：批量操作
        /// </summary>
        [HttpPost("batch")]
        public IActionResult BatchOperation([FromBody] BatchRequest request)
        {
            return Ok(new
            {
                Success = false,
                Message = "当前版本暂不支持事务操作"
            });
        }
    }

    /// <summary>
    /// 批量订单请求
    /// </summary>
    public class BatchOrderRequest
    {
        /// <summary>
        /// 用户 ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 商品 ID 列表
        /// </summary>
        public int[] ProductIds { get; set; }

        /// <summary>
        /// 数量列表
        /// </summary>
        public int[] Quantities { get; set; }
    }

    /// <summary>
    /// 转账请求
    /// </summary>
    public class TransferRequest
    {
        /// <summary>
        /// 源账户 ID
        /// </summary>
        public int FromAccountId { get; set; }

        /// <summary>
        /// 目标账户 ID
        /// </summary>
        public int ToAccountId { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// 批量操作请求
    /// </summary>
    public class BatchRequest
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 数据列表
        /// </summary>
        public object[] Data { get; set; }
    }
}
