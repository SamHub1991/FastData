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
        /// 1. 开启事务
        /// 2. 执行多个数据库操作
        /// 3. 提交或回滚
        /// 
        /// 示例请求:
        /// 
        ///     POST api/transaction/batch-orders
        ///     {
        ///         "userId": 1,
        ///         "productIds": [1, 2, 3],
        ///         "quantities": [10, 20, 30]
        ///     }
        /// </remarks>
        [HttpPost("batch-orders")]
        public IActionResult CreateBatchOrders([FromBody] BatchOrderRequest request)
        {
            var tran = (DataContext)null;
            try
            {
                // 开启事务
                tran = FastWrite.BeginTrans();

                // 检查库存
                foreach (var productId in request.ProductIds)
                {
                    // 实际业务中这里需要查询库存
                    Console.WriteLine($"检查商品 {productId} 库存...");
                }

                // 创建订单
                // var order = new Order { UserId = request.UserId, ... };
                // FastWrite.Add(order, tran);

                // 扣减库存
                // foreach (var item in orderItems)
                // {
                //     FastWrite.Update(item, tran);
                // }

                // 创建订单日志
                // FastWrite.Add(new OrderLog { ... }, tran);

                // 提交事务
                tran.Commit();

                return Ok(new { Success = true, Message = "订单创建成功" });
            }
            catch (Exception ex)
            {
                // 回滚事务
                tran?.Rollback();
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            finally
            {
                tran?.Dispose();
            }
        }

        /// <summary>
        /// 使用事务范围示例
        /// </summary>
        [HttpPost("transfer")]
        public IActionResult TransferMoney([FromBody] TransferRequest request)
        {
            try
            {
                using (var tran = FastWrite.BeginTrans())
                {
                    // 从账户 A 扣款
                    // var accountA = FastRead.Query<Account>(a => a.Id == request.FromAccountId).ToList()[0];
                    // accountA.Balance -= request.Amount;
                    // FastWrite.Update(accountA, tran);

                    // 向账户 B 加款
                    // var accountB = FastRead.Query<Account>(a => a.Id == request.ToAccountId).ToList()[0];
                    // accountB.Balance += request.Amount;
                    // FastWrite.Update(accountB, tran);

                    // 记录交易日志
                    // FastWrite.Add(new TransactionLog { ... }, tran);

                    // 提交事务
                    tran.Commit();

                    return Ok(new { Success = true, Message = "转账成功" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// 事务隔离级别示例
        /// </summary>
        [HttpPost("with-isolation")]
        public IActionResult TransactionWithIsolation()
        {
            try
            {
                // 使用特定隔离级别开启事务
                using (var tran = FastWrite.BeginTrans(System.Data.IsolationLevel.ReadCommitted))
                {
                    // 业务操作...
                    
                    tran.Commit();
                    return Ok(new { Success = true });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
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
        /// 转出账户 ID
        /// </summary>
        public int FromAccountId { get; set; }

        /// <summary>
        /// 转入账户 ID
        /// </summary>
        public int ToAccountId { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal Amount { get; set; }
    }
}
