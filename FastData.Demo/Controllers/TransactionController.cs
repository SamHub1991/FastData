using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Context;
using FastData.Demo.Models;
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
        [HttpPost("batch-orders")]
        public IActionResult CreateBatchOrders([FromBody] BatchOrderRequest request)
        {
            try
            {
                var db = new DataContext("sqlserver");
                db.BeginTrans();

                var orders = new List<AppOrder>();
                for (int i = 0; i < request.ProductIds.Length; i++)
                {
                    var order = new AppOrder
                    {
                        UserId = request.UserId,
                        OrderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{i:D4}",
                        TotalAmount = request.Quantities[i] * 100,
                        Quantity = request.Quantities[i],
                        Status = 0,
                        CreateTime = DateTime.Now
                    };

                    var result = db.Add(order);
                    if (!result.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return Ok(new { Success = false, Message = $"第{i + 1}个订单创建失败: {result.WriteReturn.Message}" });
                    }
                    orders.Add(order);
                }

                db.SubmitTrans();
                return Ok(new
                {
                    Success = true,
                    Message = $"成功创建 {orders.Count} 个订单",
                    Orders = orders.Select(o => new { o.OrderNo, o.TotalAmount })
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = $"事务执行失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 事务示例：转账操作
        /// </summary>
        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] TransferRequest request)
        {
            try
            {
                var db = new DataContext("sqlserver");
                db.BeginTrans();

                var fromUser = FastRead.Query<AppUser>(u => u.Id == request.FromAccountId).ToItem<AppUser>();
                var toUser = FastRead.Query<AppUser>(u => u.Id == request.ToAccountId).ToItem<AppUser>();

                if (fromUser == null || toUser == null)
                {
                    db.RollbackTrans();
                    return Ok(new { Success = false, Message = "账户不存在" });
                }

                if (fromUser.Salary < request.Amount)
                {
                    db.RollbackTrans();
                    return Ok(new { Success = false, Message = "余额不足" });
                }

                fromUser.Salary -= request.Amount;
                toUser.Salary += request.Amount;

                var r1 = db.Update(fromUser, a => new { a.Salary });
                if (!r1.WriteReturn.IsSuccess)
                {
                    db.RollbackTrans();
                    return Ok(new { Success = false, Message = $"扣款失败: {r1.WriteReturn.Message}" });
                }

                var r2 = db.Update(toUser, a => new { a.Salary });
                if (!r2.WriteReturn.IsSuccess)
                {
                    db.RollbackTrans();
                    return Ok(new { Success = false, Message = $"入账失败: {r2.WriteReturn.Message}" });
                }

                db.SubmitTrans();
                return Ok(new
                {
                    Success = true,
                    Message = $"转账成功: {fromUser.UserName} -> {toUser.UserName}, 金额: {request.Amount}",
                    FromBalance = fromUser.Salary,
                    ToBalance = toUser.Salary
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = $"转账失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 事务示例：批量添加用户
        /// </summary>
        [HttpPost("batch-users")]
        public IActionResult BatchCreateUsers([FromBody] List<AppUser> users)
        {
            try
            {
                var db = new DataContext("sqlserver");
                db.BeginTrans();

                var created = new List<string>();
                foreach (var user in users)
                {
                    user.CreateTime = DateTime.Now;
                    user.IsActive = true;
                    var result = db.Add(user);
                    if (!result.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return Ok(new { Success = false, Message = $"用户 {user.UserName} 创建失败: {result.WriteReturn.Message}" });
                    }
                    created.Add(user.UserName);
                }

                db.SubmitTrans();
                return Ok(new
                {
                    Success = true,
                    Message = $"成功创建 {created.Count} 个用户",
                    Users = created
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = $"批量创建失败: {ex.Message}" });
            }
        }
    }

    public class BatchOrderRequest
    {
        public int UserId { get; set; }
        public int[] ProductIds { get; set; }
        public int[] Quantities { get; set; }
    }

    public class TransferRequest
    {
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
