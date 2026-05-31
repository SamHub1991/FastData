using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Context;
using FastData.Example.Model;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 数据校验与空值处理业务场景
    /// 
    /// 覆盖 ORM 功能：
    /// - NullSafety 扩展方法 (OrDefault/OrEmpty/OrZero/OrEmptyList/SafeGet)
    /// - NullableResult (ToNullable/Safe/OrThrow)
    /// - 字段值判断与过滤
    /// - 异常捕获与降级处理
    /// - 数据完整性校验
    /// </summary>
    public static class DataValidationExample
    {
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  数据校验与空值处理");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunNullSafety();
            Console.WriteLine();
            RunNullableResult();
            Console.WriteLine();
            RunFieldValidation();
            Console.WriteLine();
            RunExceptionHandling();
            Console.WriteLine();
            RunDataIntegrityCheck();
        }

        #region 空值安全处理

        /// <summary>
        /// 空值安全处理
        /// 
        /// 业务场景：从数据库查询的数据可能为 null，
        /// 直接访问会抛 NullReferenceException，
        /// 使用 NullSafety 扩展方法安全处理
        /// </summary>
        private static void RunNullSafety()
        {
            Console.WriteLine("【1】空值安全处理");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. OrDefault - null 时返回默认值
                //    业务逻辑：查询用户，不存在时返回默认用户
                var user = FastRead.Query<User>(u => u.Id == 99999)
                    .FirstOrDefault();

                // user 可能为 null，使用 OrDefault 安全处理
                var safeUser = user.OrDefault(new User
                {
                    UserName = "默认用户",
                    Email = "default@example.com",
                    Age = 0
                });

                Console.WriteLine($"  用户名: {safeUser.UserName}");

                // 2. OrEmpty - 字段为 null 时返回空字符串
                //    业务逻辑：安全获取用户信息，避免 null
                var userName = user?.UserName.OrEmpty("未设置");
                var email = user?.Email.OrEmpty("未填写");
                var phone = user?.Phone.OrEmpty("未绑定");

                Console.WriteLine($"  姓名: {userName}, 邮箱: {email}, 手机: {phone}");

                // 3. OrZero - 数值为 null 时返回 0
                //    业务逻辑：安全计算用户年龄
                var age = user?.Age ?? 0;
                var salary = user?.Salary ?? 0;

                Console.WriteLine($"  年龄: {age}, 薪资: {salary}");

                // 4. OrEmptyList - 列表为 null 时返回空列表
                //    业务逻辑：安全遍历订单列表
                List<Order> orders = null;
                var safeOrders = orders.OrEmptyList();

                Console.WriteLine($"  订单数: {safeOrders.Count} (null 被转为空列表)");

                // 5. SafeGet - 安全获取列表元素
                //    业务逻辑：安全获取第一个订单，避免索引越界
                var firstOrder = safeOrders.SafeGet(0, new Order { OrderNo = "无订单" });
                Console.WriteLine($"  第一个订单: {firstOrder.OrderNo}");

                // 6. 实际业务场景：安全构建用户信息
                var activeUsers = FastRead.Query<User>(u => u.IsActive)
                    .Take(3)
                    .ToList<User>();

                Console.WriteLine("  安全构建用户信息:");
                foreach (var u in activeUsers)
                {
                    var info = $"{u.UserName.OrEmpty("匿名")}" +
                               $" | {u.Email.OrEmpty("无邮箱")}" +
                               $" | {u.Department.OrEmpty("无部门")}" +
                               $" | 年龄:{u.Age}" +
                               $" | 薪资:{u.Salary:C}";
                    Console.WriteLine($"    {info}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region NullableResult 包装

        /// <summary>
        /// NullableResult 安全包装
        /// 
        /// 业务场景：链式操作中避免 null 传播，
        /// 使用 NullableResult 包装查询结果
        /// </summary>
        private static void RunNullableResult()
        {
            Console.WriteLine("【2】NullableResult 安全包装");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. ToNullable - 包装查询结果
                //    业务逻辑：查询用户并安全获取值
                var userResult = FastRead.Query<User>(u => u.Id == 1)
                    .FirstOrDefault()
                    .ToNullable();

                // 安全获取值或默认值
                var userName = userResult.OrDefault(new User { UserName = "未知用户" }).UserName;
                Console.WriteLine($"  用户名 (OrDefault): {userName}");

                // 2. Safe - 安全包装
                //    业务逻辑：查询订单并安全处理
                var orderResult = FastRead.Query<Order>(o => o.Id == 1)
                    .FirstOrDefault()
                    .Safe();

                if (orderResult.HasValue)
                {
                    Console.WriteLine($"  订单存在: {orderResult.Value.OrderNo}");
                }
                else
                {
                    Console.WriteLine("  订单不存在");
                }

                // 3. OrThrow - 不存在时抛出业务异常
                //    业务逻辑：查询必需的配置项，不存在时抛异常
                try
                {
                    var requiredUser = FastRead.Query<User>(u => u.Role == "Admin")
                        .FirstOrDefault()
                        .ToNullable()
                        .OrThrow(new InvalidOperationException("未找到管理员账号，请联系系统管理员"));

                    Console.WriteLine($"  管理员: {requiredUser.UserName}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"  业务异常: {ex.Message}");
                }

                // 4. 链式安全操作
                //    业务逻辑：查询用户 -> 获取部门 -> 查询同部门用户
                var department = FastRead.Query<User>(u => u.Id == 1)
                    .FirstOrDefault()
                    .ToNullable()
                    .OrDefault(new User { Department = "默认部门" })
                    .Department;

                var sameDeptUsers = FastRead.Query<User>(u => u.Department == department && u.IsActive)
                    .ToList();

                Console.WriteLine($"  {department} 活跃用户: {sameDeptUsers.Count} 人");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region 字段值判断与过滤

        /// <summary>
        /// 字段值判断与过滤
        /// 
        /// 业务场景：根据业务规则过滤数据，
        /// 如年龄范围、薪资区间、状态判断等
        /// </summary>
        private static void RunFieldValidation()
        {
            Console.WriteLine("【3】字段值判断与过滤");
            Console.WriteLine("----------------------------------------");

            try
            {
                var users = FastRead.Query<User>(u => u.IsActive)
                    .ToList();

                // 1. 数值范围过滤
                //    业务逻辑：筛选成年人用户
                var adults = users.Where(u => u.Age >= 18).ToList();
                Console.WriteLine($"  成年用户: {adults.Count}/{users.Count}");

                // 2. 字符串非空过滤
                //    业务逻辑：筛选已填写邮箱的用户
                var withEmail = users.Where(u => !string.IsNullOrEmpty(u.Email)).ToList();
                Console.WriteLine($"  有邮箱用户: {withEmail.Count}/{users.Count}");

                // 3. 多条件组合过滤
                //    业务逻辑：筛选高价值用户（活跃 + 有邮箱 + 年龄25-45 + 薪资>5000）
                var highValueUsers = users.Where(u =>
                    u.IsActive &&
                    !string.IsNullOrEmpty(u.Email) &&
                    u.Age >= 25 && u.Age <= 45 &&
                    u.Salary > 5000
                ).ToList();
                Console.WriteLine($"  高价值用户: {highValueUsers.Count}/{users.Count}");

                // 4. 可空字段安全比较
                //    业务逻辑：筛选有更新记录的用户
                var updatedUsers = users.Where(u => u.UpdateTime.HasValue).ToList();
                Console.WriteLine($"  有更新记录: {updatedUsers.Count}/{users.Count}");

                // 5. 枚举值判断
                //    业务逻辑：按角色统计用户
                var roleGroups = users
                    .GroupBy(u => u.Role.OrEmpty("未分配"))
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToList();

                Console.WriteLine("  角色分布:");
                foreach (var g in roleGroups)
                {
                    Console.WriteLine($"    {g.Role}: {g.Count} 人");
                }

                // 6. 订单状态过滤
                //    业务逻辑：筛选有效订单（已支付、已发货、已完成）
                var validOrders = FastRead.Query<Order>(o => o.Id > 0)
                    .ToList()
                    .Where(o => o.Status >= 1 && o.Status <= 3)
                    .ToList();

                Console.WriteLine($"  有效订单: {validOrders.Count}");

                // 7. 金额区间统计
                var orderAmounts = validOrders.Select(o => o.TotalAmount).ToList();
                if (orderAmounts.Any())
                {
                    Console.WriteLine($"  订单金额: 最小={orderAmounts.Min():C}, 最大={orderAmounts.Max():C}, 平均={orderAmounts.Average():C}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  过滤异常: {ex.Message}");
            }
        }

        #endregion

        #region 异常捕获与降级

        /// <summary>
        /// 异常捕获与降级处理
        /// 
        /// 业务场景：数据库查询失败时的降级策略，
        /// 如返回缓存数据、默认值、或空结果
        /// </summary>
        private static void RunExceptionHandling()
        {
            Console.WriteLine("【4】异常捕获与降级处理");
            Console.WriteLine("----------------------------------------");

            // 1. 查询异常降级
            //    业务逻辑：查询失败时返回空列表，不中断业务流程
            List<User> users;
            try
            {
                users = FastRead.Query<User>(u => u.IsActive)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询失败，降级处理: {ex.Message}");
                users = new List<User>();  // 降级为空列表
            }

            Console.WriteLine($"  用户数: {users.Count}");

            // 2. 写入异常处理
            //    业务逻辑：写入失败时记录错误并继续
            try
            {
                var newUser = new User
                {
                    UserName = "test_user",
                    Email = "test@example.com",
                    Age = 25,
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                var result = FastWrite.Add(newUser);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"  写入成功: ID={newUser.Id}");
                }
                else
                {
                    Console.WriteLine($"  写入失败: {result.Message}");
                    // 降级：记录到日志或重试队列
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  写入异常: {ex.Message}");
                // 降级：记录到失败队列
            }

            // 3. 事务异常处理
            //    业务逻辑：事务失败时自动回滚
            try
            {
                var db = new DataContext("db");
                db.BeginTrans();

                try
                {
                    var user = new User
                    {
                        UserName = "tx_user",
                        Email = "tx@example.com",
                        Age = 30,
                        IsActive = true,
                        CreateTime = DateTime.Now
                    };

                    var r1 = db.Add(user);
                    if (!r1.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        Console.WriteLine($"  事务回滚: {r1.WriteReturn.Message}");
                        return;
                    }

                    db.SubmitTrans();
                    Console.WriteLine("  事务提交成功");
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    Console.WriteLine($"  事务异常回滚: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  事务创建异常: {ex.Message}");
            }

            // 4. 批量操作异常处理
            //    业务逻辑：批量写入时部分失败，记录失败项
            try
            {
                var batchUsers = new List<User>
                {
                    new User { UserName = "batch_1", Email = "b1@example.com", Age = 20, IsActive = true, CreateTime = DateTime.Now },
                    new User { UserName = "batch_2", Email = "b2@example.com", Age = 25, IsActive = true, CreateTime = DateTime.Now },
                    new User { UserName = "batch_3", Email = "b3@example.com", Age = 30, IsActive = true, CreateTime = DateTime.Now }
                };

                var batchResult = FastWrite.AddList(batchUsers);

                if (batchResult.IsSuccess)
                {
                    Console.WriteLine($"  批量写入成功: {batchUsers.Count} 条");
                }
                else
                {
                    Console.WriteLine($"  批量写入失败: {batchResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  批量操作异常: {ex.Message}");
            }
        }

        #endregion

        #region 数据完整性校验

        /// <summary>
        /// 数据完整性校验
        /// 
        /// 业务场景：删除/更新前检查关联数据，
        /// 确保不会破坏数据完整性
        /// </summary>
        private static void RunDataIntegrityCheck()
        {
            Console.WriteLine("【5】数据完整性校验");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 删除前检查关联数据
                //    业务逻辑：删除用户前检查是否有未完成订单
                var userId = 1;
                var hasUnfinishedOrders = FastRead.Query<Order>(
                    o => o.UserId == userId && o.Status < 3)
                    .Any();

                if (hasUnfinishedOrders)
                {
                    Console.WriteLine($"  用户{userId}有未完成订单，不能删除");
                }
                else
                {
                    Console.WriteLine($"  用户{userId}可以安全删除");
                }

                // 2. 更新前检查唯一性
                //    业务逻辑：修改用户名前检查是否已存在
                var newUserName = "admin";
                var isDuplicate = FastRead.Query<User>(u => u.Id > 0)
                    .Any(u => u.UserName == newUserName);

                if (isDuplicate)
                {
                    Console.WriteLine($"  用户名'{newUserName}'已存在，不能使用");
                }
                else
                {
                    Console.WriteLine($"  用户名'{newUserName}'可用");
                }

                // 3. 状态转换校验
                //    业务逻辑：订单状态只能单向流转
                var orderId = 1;
                var order = FastRead.Query<Order>(o => o.Id == orderId)
                    .FirstOrDefault();

                if (order != null)
                {
                    var currentStatus = order.Status;
                    var targetStatus = 2; // 尝试改为已发货

                    // 状态只能从 已支付(1) -> 已发货(2)
                    if (currentStatus == 1 && targetStatus == 2)
                    {
                        Console.WriteLine($"  订单{orderId}状态可以从{currentStatus}改为{targetStatus}");
                    }
                    else
                    {
                        Console.WriteLine($"  订单{orderId}状态不能从{currentStatus}改为{targetStatus}");
                    }
                }

                // 4. 数据一致性检查
                //    业务逻辑：检查订单金额是否与商品价格一致
                var orders = FastRead.Query<Order>(o => o.Status == 0)
                    .ToList();

                var inconsistentOrders = orders.Where(o => o.TotalAmount <= 0).ToList();
                Console.WriteLine($"  待支付订单: {orders.Count}, 金额异常: {inconsistentOrders.Count}");

                // 5. 批量数据校验
                //    业务逻辑：批量导入前校验数据
                var importUsers = new List<User>
                {
                    new User { UserName = "import_1", Email = "i1@example.com", Age = 25 },
                    new User { UserName = "", Email = "i2@example.com", Age = 30 },  // 名称为空
                    new User { UserName = "import_3", Email = "", Age = -1 },  // 邮箱为空，年龄异常
                };

                var validUsers = importUsers.Where(u =>
                    !string.IsNullOrEmpty(u.UserName) &&
                    !string.IsNullOrEmpty(u.Email) &&
                    u.Age > 0 && u.Age < 150
                ).ToList();

                Console.WriteLine($"  导入数据: {importUsers.Count} 条, 有效: {validUsers.Count} 条");
                Console.WriteLine($"  过滤: {importUsers.Count - validUsers.Count} 条数据不合法");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  校验异常: {ex.Message}");
            }
        }

        #endregion
    }
}
