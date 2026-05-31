using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using FastData;
using FastData.Context;
using FastData.Model;
using FastData.Example.Model;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 订单业务流程示例
    /// 场景：完整的订单生命周期管理（创建、支付、发货、完成、取消）
    /// </summary>
    public static class OrderBusinessExample
    {
        private const string DbKey = "db";

        #region 运行示例

        /// <summary>
        /// 运行订单业务示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== 订单业务流程示例 ===");
            Console.WriteLine();

            try
            {
                DemoOrderLifecycle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"示例运行异常: {ex.Message}");
                Console.WriteLine("请确保已配置数据库连接并创建相关表。");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 演示完整订单生命周期
        /// </summary>
        private static void DemoOrderLifecycle()
        {
            Console.WriteLine("--- 完整订单生命周期演示 ---");
            Console.WriteLine();

            // 第一步：创建订单
            Console.WriteLine("1. 创建订单");
            var orderItems = new List<(int productId, int quantity)>
            {
                (1, 2),
                (2, 1)
            };
            var createResult = CreateOrder(userId: 1, items: orderItems, remark: "测试订单");
            if (!createResult.success)
            {
                Console.WriteLine($"   创建订单失败: {createResult.message}");
                return;
            }
            Console.WriteLine($"   订单创建成功，订单号: {createResult.order.OrderNo}，金额: {createResult.order.TotalAmount}");

            // 第二步：支付订单
            Console.WriteLine("2. 支付订单");
            var payResult = PayOrder(createResult.order.Id);
            Console.WriteLine(payResult.success ? "   支付成功" : $"   支付失败: {payResult.message}");

            // 第三步：发货
            Console.WriteLine("3. 发货");
            var shipResult = ShipOrder(createResult.order.Id);
            Console.WriteLine(shipResult.success ? "   发货成功" : $"   发货失败: {shipResult.message}");

            // 第四步：完成订单
            Console.WriteLine("4. 完成订单");
            var completeResult = CompleteOrder(createResult.order.Id);
            Console.WriteLine(completeResult.success ? "   订单完成" : $"   完成失败: {completeResult.message}");

            // 查询订单详情
            Console.WriteLine("5. 查询订单详情");
            var detail = GetOrderDetail(createResult.order.Id);
            if (detail.Order != null)
            {
                Console.WriteLine($"   订单号: {detail.Order.OrderNo}, 状态: {GetStatusText(detail.Order.Status)}, 商品数: {detail.Items.Count}");
            }

            // 查询用户订单（分页）
            Console.WriteLine("6. 查询用户订单（分页）");
            var userOrders = GetUserOrders(userId: 1, status: null, pageIndex: 1, pageSize: 10);
            Console.WriteLine($"   共 {userOrders.pModel.TotalRecord} 条订单，当前页 {userOrders.list.Count} 条");

            // 订单统计
            Console.WriteLine("7. 订单统计");
            var stats = GetOrderStatistics();
            foreach (var stat in stats)
            {
                Console.WriteLine($"   状态 {GetStatusText(stat.Key)}: {stat.Value} 单");
            }

            // 演示取消订单（创建一个新订单然后取消）
            Console.WriteLine("8. 取消订单演示");
            var cancelItems = new List<(int productId, int quantity)> { (1, 1) };
            var cancelOrderResult = CreateOrder(userId: 1, items: cancelItems, remark: "待取消订单");
            if (cancelOrderResult.success)
            {
                var cancelResult = CancelOrder(cancelOrderResult.order.Id);
                Console.WriteLine(cancelResult.success ? "   订单已取消，库存已回滚" : $"   取消失败: {cancelResult.message}");
            }
        }

        #endregion

        #region 创建订单

        /// <summary>
        /// 创建订单 - 完整的订单创建流程
        /// 流程：验证用户 -> 检查库存 -> 生成订单号 -> 创建订单 -> 创建订单明细 -> 扣减库存
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="items">商品列表（商品ID, 数量）</param>
        /// <param name="remark">订单备注</param>
        /// <returns>创建结果，包含订单信息</returns>
        public static (bool success, string message, Order order) CreateOrder(int userId, List<(int productId, int quantity)> items, string remark = null)
        {
            if (items == null || items.Count == 0)
                return (false, "订单商品不能为空", null);

            using (var db = new DataContext(DbKey))
            {
                try
                {
                    // 1. 验证用户存在且有效
                    var user = FastRead.Query<User>(u => u.Id == userId && u.IsActive && !u.IsDeleted).ToItem();
                    if (user == null || user.Id == 0)
                        return (false, "用户不存在或已被禁用", null);

                    // 2. 查询所有商品信息并校验库存
                    var products = new Dictionary<int, Product>();
                    foreach (var item in items)
                    {
                        var product = FastRead.Query<Product>(p => p.Id == item.productId && p.IsActive).ToItem();
                        if (product == null || product.Id == 0)
                            return (false, $"商品ID={item.productId}不存在或已下架", null);
                        products[item.productId] = product;
                    }

                    // 检查库存
                    foreach (var item in items)
                    {
                        var product = products[item.productId];
                        if (product.Stock < item.quantity)
                            return (false, $"商品[{product.ProductName}]库存不足，当前库存: {product.Stock}，需求: {item.quantity}", null);
                    }

                    // 3. 计算总金额
                    decimal totalAmount = 0;
                    foreach (var item in items)
                    {
                        totalAmount += products[item.productId].Price * item.quantity;
                    }

                    // 4. 生成唯一订单号
                    var orderNo = GenerateOrderNo();

                    // 5. 开始事务
                    db.BeginTrans();

                    // 6. 创建订单主表
                    var order = new Order
                    {
                        UserId = userId,
                        OrderNo = orderNo,
                        TotalAmount = totalAmount,
                        Status = 0, // 待支付
                        Remark = remark,
                        CreateTime = DateTime.Now
                    };
                    var addOrderResult = db.Add(order);
                    if (!addOrderResult.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return (false, $"创建订单失败: {addOrderResult.WriteReturn.Message}", null);
                    }

                    // 7. 创建订单明细
                    foreach (var item in items)
                    {
                        var product = products[item.productId];
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = item.productId,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            Quantity = item.quantity,
                            Subtotal = product.Price * item.quantity
                        };
                        var addItemResult = db.Add(orderItem);
                        if (!addItemResult.WriteReturn.IsSuccess)
                        {
                            db.RollbackTrans();
                            return (false, $"创建订单明细失败: {addItemResult.WriteReturn.Message}", null);
                        }
                    }

                    // 8. 扣减库存（使用原生SQL确保原子性扣减）
                    foreach (var item in items)
                    {
                        var stockSql = "UPDATE Products SET Stock = Stock - @Quantity, UpdateTime = @UpdateTime WHERE Id = @ProductId AND Stock >= @Quantity";
                        var stockParams = new DbParameter[]
                        {
                            CreateParam(db, "@ProductId", item.productId),
                            CreateParam(db, "@Quantity", item.quantity),
                            CreateParam(db, "@UpdateTime", DateTime.Now)
                        };
                        var stockResult = db.ExecuteSql(stockSql, stockParams);
                        if (!stockResult.WriteReturn.IsSuccess)
                        {
                            db.RollbackTrans();
                            return (false, $"扣减商品[{products[item.productId].ProductName}]库存失败", null);
                        }
                    }

                    // 9. 提交事务
                    db.SubmitTrans();

                    return (true, "订单创建成功", order);
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    return (false, $"创建订单异常: {ex.Message}", null);
                }
            }
        }

        #endregion

        #region 支付订单

        /// <summary>
        /// 支付订单 - 将订单状态从待支付更新为已支付
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        public static (bool success, string message) PayOrder(int orderId)
        {
            try
            {
                // 查询订单
                var order = FastRead.Query<Order>(o => o.Id == orderId).ToItem();
                if (order == null || order.Id == 0)
                    return (false, "订单不存在");

                if (order.Status != 0)
                    return (false, $"订单状态不正确，当前状态: {GetStatusText(order.Status)}，只有待支付订单可以支付");

                // 更新订单状态为已支付
                var updateOrder = new Order
                {
                    Id = order.Id,
                    Status = 1,
                    PayTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                var updateResult = FastWrite.Update(updateOrder, o => o.Id == orderId,
                    field: o => new { o.Status, o.PayTime, o.UpdateTime });
                if (!updateResult.IsSuccess)
                    return (false, $"支付更新失败: {updateResult.Message}");

                return (true, "支付成功");
            }
            catch (Exception ex)
            {
                return (false, $"支付异常: {ex.Message}");
            }
        }

        #endregion

        #region 发货

        /// <summary>
        /// 发货 - 将订单状态从已支付更新为已发货
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        public static (bool success, string message) ShipOrder(int orderId)
        {
            try
            {
                var order = FastRead.Query<Order>(o => o.Id == orderId).ToItem();
                if (order == null || order.Id == 0)
                    return (false, "订单不存在");

                if (order.Status != 1)
                    return (false, $"订单状态不正确，当前状态: {GetStatusText(order.Status)}，只有已支付订单可以发货");

                var updateOrder = new Order
                {
                    Id = order.Id,
                    Status = 2,
                    UpdateTime = DateTime.Now
                };
                var updateResult = FastWrite.Update(updateOrder, o => o.Id == orderId,
                    field: o => new { o.Status, o.UpdateTime });
                if (!updateResult.IsSuccess)
                    return (false, $"发货更新失败: {updateResult.Message}");

                return (true, "发货成功");
            }
            catch (Exception ex)
            {
                return (false, $"发货异常: {ex.Message}");
            }
        }

        #endregion

        #region 完成订单

        /// <summary>
        /// 完成订单 - 将订单状态从已发货更新为已完成
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        public static (bool success, string message) CompleteOrder(int orderId)
        {
            try
            {
                var order = FastRead.Query<Order>(o => o.Id == orderId).ToItem();
                if (order == null || order.Id == 0)
                    return (false, "订单不存在");

                if (order.Status != 2)
                    return (false, $"订单状态不正确，当前状态: {GetStatusText(order.Status)}，只有已发货订单可以完成");

                var updateOrder = new Order
                {
                    Id = order.Id,
                    Status = 3,
                    UpdateTime = DateTime.Now
                };
                var updateResult = FastWrite.Update(updateOrder, o => o.Id == orderId,
                    field: o => new { o.Status, o.UpdateTime });
                if (!updateResult.IsSuccess)
                    return (false, $"完成订单失败: {updateResult.Message}");

                return (true, "订单已完成");
            }
            catch (Exception ex)
            {
                return (false, $"完成订单异常: {ex.Message}");
            }
        }

        #endregion

        #region 取消订单

        /// <summary>
        /// 取消订单 - 支持待支付和已支付状态的取消，同时回滚库存
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        public static (bool success, string message) CancelOrder(int orderId)
        {
            using (var db = new DataContext(DbKey))
            {
                try
                {
                    // 查询订单
                    var order = FastRead.Query<Order>(o => o.Id == orderId).ToItem();
                    if (order == null || order.Id == 0)
                        return (false, "订单不存在");

                    // 只有待支付或已支付的订单可以取消
                    if (order.Status != 0 && order.Status != 1)
                        return (false, $"订单状态不正确，当前状态: {GetStatusText(order.Status)}，只有待支付或已支付的订单可以取消");

                    // 查询订单明细（用于库存回滚）
                    var orderItems = FastRead.Query<OrderItem>(oi => oi.OrderId == orderId).ToList();
                    if (orderItems.Count == 0)
                        return (false, "订单明细不存在");

                    // 开始事务
                    db.BeginTrans();

                    // 更新订单状态为已取消（使用DataContext事务）
                    var cancelOrder = new Order
                    {
                        Id = order.Id,
                        Status = 4,
                        UpdateTime = DateTime.Now
                    };
                    var updateResult = db.Update(cancelOrder, o => o.Id == orderId,
                        field: o => new { o.Status, o.UpdateTime });
                    if (!updateResult.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return (false, $"取消订单失败: {updateResult.WriteReturn.Message}");
                    }

                    // 回滚库存
                    foreach (var item in orderItems)
                    {
                        var stockSql = "UPDATE Products SET Stock = Stock + @Quantity, UpdateTime = @UpdateTime WHERE Id = @ProductId";
                        var stockParams = new DbParameter[]
                        {
                            CreateParam(db, "@ProductId", item.ProductId),
                            CreateParam(db, "@Quantity", item.Quantity),
                            CreateParam(db, "@UpdateTime", DateTime.Now)
                        };
                        var stockResult = db.ExecuteSql(stockSql, stockParams);
                        if (!stockResult.WriteReturn.IsSuccess)
                        {
                            db.RollbackTrans();
                            return (false, $"回滚商品[{item.ProductName}]库存失败");
                        }
                    }

                    // 提交事务
                    db.SubmitTrans();

                    return (true, "订单已取消，库存已回滚");
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    return (false, $"取消订单异常: {ex.Message}");
                }
            }
        }

        #endregion

        #region 查询订单详情

        /// <summary>
        /// 获取订单详情 - 包含订单主表和明细信息
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>订单详情（订单 + 明细列表）</returns>
        public static (Order Order, List<OrderItem> Items) GetOrderDetail(int orderId)
        {
            try
            {
                // 查询订单主表
                var order = FastRead.Query<Order>(o => o.Id == orderId).ToItem();
                if (order == null || order.Id == 0)
                    return (null, new List<OrderItem>());

                // 查询订单明细
                var items = FastRead.Query<OrderItem>(oi => oi.OrderId == orderId).ToList();

                return (order, items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询订单详情异常: {ex.Message}");
                return (null, new List<OrderItem>());
            }
        }

        #endregion

        #region 用户订单列表（分页）

        /// <summary>
        /// 获取用户订单列表 - 支持状态筛选和分页
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="status">订单状态筛选（null表示全部）</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>分页结果</returns>
        public static PageResult<Order> GetUserOrders(int userId, int? status, int pageIndex, int pageSize)
        {
            var pModel = new PageModel
            {
                PageId = pageIndex,
                PageSize = pageSize
            };

            try
            {
                DataQuery<Order> query;
                if (status.HasValue)
                {
                    query = FastRead.Query<Order>(o => o.UserId == userId && o.Status == status.Value);
                }
                else
                {
                    query = FastRead.Query<Order>(o => o.UserId == userId);
                }

                query = query.OrderByDescending(o => o.Id);
                return query.ToPage<Order>(pModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询用户订单异常: {ex.Message}");
                return new PageResult<Order>();
            }
        }

        #endregion

        #region 订单统计

        /// <summary>
        /// 获取订单统计 - 按状态分组统计订单数量
        /// 使用原生SQL实现聚合查询
        /// </summary>
        /// <returns>统计结果（状态 -> 数量）</returns>
        public static Dictionary<int, int> GetOrderStatistics()
        {
            var result = new Dictionary<int, int>();

            try
            {
                var sql = "SELECT Status, COUNT(*) AS OrderCount FROM Orders GROUP BY Status ORDER BY Status";
                var stats = FastRead.ExecuteSql(sql, null);
                foreach (var row in stats)
                {
                    if (row.ContainsKey("Status") && row.ContainsKey("OrderCount"))
                    {
                        var statusVal = Convert.ToInt32(row["Status"]);
                        var count = Convert.ToInt32(row["OrderCount"]);
                        result[statusVal] = count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"订单统计查询异常: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成唯一订单号
        /// 格式: ORD + 日期时间（yyyyMMddHHmmss）+ 4位随机数
        /// </summary>
        private static string GenerateOrderNo()
        {
            return "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        /// <summary>
        /// 获取订单状态文本描述
        /// </summary>
        private static string GetStatusText(int status)
        {
            switch (status)
            {
                case 0: return "待支付";
                case 1: return "已支付";
                case 2: return "已发货";
                case 3: return "已完成";
                case 4: return "已取消";
                default: return "未知状态";
            }
        }

        /// <summary>
        /// 创建数据库参数
        /// </summary>
        private static DbParameter CreateParam(DataContext db, string name, object value)
        {
            var param = System.Data.Common.DbProviderFactories.GetFactory(db.config.ProviderName).CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            return param;
        }

        #endregion
    }
}
