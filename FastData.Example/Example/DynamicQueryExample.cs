using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Example.Model;
using FastData.Model;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 动态查询业务场景
    /// 
    /// 覆盖 ORM 功能：
    /// - Where&lt;T&gt; 条件构建器（动态拼接条件）
    /// - Any/All 存在性判断
    /// - First/FirstOrDefault 取首条
    /// - Single/SingleOrDefault 单条精确匹配
    /// - Count 计数
    /// - ToArray/ToDictionary/ToDics 集合转换
    /// </summary>
    public static class DynamicQueryExample
    {
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  动态查询业务场景");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunDynamicFilter();
            Console.WriteLine();
            RunExistenceCheck();
            Console.WriteLine();
            RunSingleQuery();
            Console.WriteLine();
            RunCollectionConvert();
        }

        #region 动态条件过滤

        /// <summary>
        /// 用户搜索：多条件动态组合
        /// 
        /// 业务场景：后台管理系统的用户搜索功能，用户可以输入多个搜索条件，每个条件都是可选的
        /// 使用 Where&lt;T&gt; 条件构建器动态拼接条件
        /// </summary>
        private static void RunDynamicFilter()
        {
            Console.WriteLine("【1】动态条件过滤 - 用户搜索");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 模拟搜索参数（实际项目中来自前端请求）
                string searchName = "张";
                string searchEmail = null;  // 未输入
                int? minAge = 18;           // 最小年龄
                int? maxAge = null;         // 未设置最大年龄
                bool? isActive = true;      // 只查活跃用户
                string department = null;   // 未选择部门

                // 使用 Where<T> 条件构建器动态拼接条件
                var where = new Where<User>();

                // 姓名模糊搜索（只在有输入时添加条件）
                if (!string.IsNullOrEmpty(searchName))
                {
                    where.And(u => u.UserName.Contains(searchName));
                }

                // 邮箱精确匹配
                if (!string.IsNullOrEmpty(searchEmail))
                {
                    where.And(u => u.Email == searchEmail);
                }

                // 年龄范围（只在设置时添加）
                if (minAge.HasValue)
                {
                    where.And(u => u.Age >= minAge.Value);
                }
                if (maxAge.HasValue)
                {
                    where.And(u => u.Age <= maxAge.Value);
                }

                // 状态过滤
                if (isActive.HasValue)
                {
                    where.And(u => u.IsActive == isActive.Value);
                }

                // 部门过滤
                if (!string.IsNullOrEmpty(department))
                {
                    where.And(u => u.Department == department);
                }

                // 应用条件查询
                var users = FastRead.Query<User>(u => u.Id > 0)
                    .Where(where)
                    .OrderBy<User>(u => u.CreateTime)
                    .ToPage<User>(new PageModel { PageId = 1, PageSize = 10 });

                Console.WriteLine($"  搜索条件: 姓名含'{searchName}', 年龄>={minAge}, 状态=活跃");
                Console.WriteLine($"  结果: {users.list.Count} 条, 总计 {users.pModel.TotalRecord} 条");

                // 2. 使用 Or 条件
                //    业务逻辑：搜索VIP用户（年龄>=30 或 薪资>=10000）
                var vipWhere = new Where<User>();
                vipWhere.Or(u => u.Age >= 30);
                vipWhere.Or(u => u.Salary >= 10000);

                var vipUsers = FastRead.Query<User>(u => u.Id > 0)
                    .Where(vipWhere)
                    .OrderByDescending<User>(u => u.Salary)
                    .ToList<User>();

                Console.WriteLine($"  VIP用户数: {vipUsers.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
        }

        #endregion

        #region 存在性判断

        /// <summary>
        /// 数据存在性检查
        /// 
        /// 业务场景：
        /// - Any: 检查是否存在符合条件的数据（比 Count 更高效）
        /// - All: 检查是否所有数据都符合条件
        /// </summary>
        private static void RunExistenceCheck()
        {
            Console.WriteLine("【2】存在性判断 - Any/All");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. Any - 检查是否存在活跃用户
                var hasActiveUsers = FastRead.Query<User>(u => u.IsActive).Any();
                Console.WriteLine($"  是否有活跃用户: {(hasActiveUsers ? "是" : "否")}");

                // 2. Any - 检查是否存在待处理订单
                var hasPendingOrders = FastRead.Query<Order>(o => o.Status == 0).Any();
                Console.WriteLine($"  是否有待处理订单: {(hasPendingOrders ? "是" : "否")}");

                // 3. All - 检查所有用户是否都设置了邮箱
                var allHaveEmail = FastRead.Query<User>(u => u.IsActive).All(u => !string.IsNullOrEmpty(u.Email));
                Console.WriteLine($"  所有活跃用户都有邮箱: {(allHaveEmail ? "是" : "否")}");

                // 4. All - 检查所有已支付订单金额是否都大于0
                var allPositiveAmount = FastRead.Query<Order>(o => o.Status == 1).All(o => o.TotalAmount > 0);
                Console.WriteLine($"  所有已支付订单金额>0: {(allPositiveAmount ? "是" : "否")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  检查异常: {ex.Message}");
            }
        }

        #endregion

        #region 单条查询

        /// <summary>
        /// 精确获取单条数据
        /// 
        /// 业务场景：
        /// - FirstOrDefault: 获取第一条或默认值（不会报错）
        /// - First: 获取第一条（无数据时报错）
        /// - Single: 确保只有一条数据（多条时报错）
        /// - SingleOrDefault: 确保最多一条数据
        /// </summary>
        private static void RunSingleQuery()
        {
            Console.WriteLine("【3】单条查询 - First/Single");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. FirstOrDefault - 获取最新用户（可能无数据）
                var latestUser = FastRead.Query<User>(u => u.IsActive)
                    .OrderByDescending<User>(u => u.CreateTime)
                    .FirstOrDefault();

                if (latestUser != null)
                {
                    Console.WriteLine($"  最新用户: {latestUser.UserName} ({latestUser.Email})");
                }
                else
                {
                    Console.WriteLine("  暂无活跃用户");
                }

                // 2. First - 获取第一个活跃用户（无数据会报错）
                try
                {
                    var firstUser = FastRead.Query<User>(u => u.IsActive)
                        .OrderBy<User>(u => u.Id)
                        .First();
                    Console.WriteLine($"  第一个活跃用户: {firstUser.UserName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  First 异常: {ex.Message}");
                }

                // 3. Single - 确保唯一性（通过ID查询应该只有一条）
                var userById = FastRead.Query<User>(u => u.Id == 1).SingleOrDefault();
                if (userById != null)
                {
                    Console.WriteLine($"  ID=1 用户: {userById.UserName}");
                }
                else
                {
                    Console.WriteLine("  ID=1 用户不存在");
                }

                // 4. 查询指定用户的订单
                var userOrders = FastRead.Query<Order>(o => o.UserId == 1)
                    .OrderByDescending<Order>(o => o.CreateTime)
                    .Take(3)
                    .ToList<Order>();

                Console.WriteLine($"  用户1的订单数: {userOrders.Count}");
                foreach (var order in userOrders)
                {
                    Console.WriteLine($"    {order.OrderNo}: {order.TotalAmount:C}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
        }

        #endregion

        #region 集合转换

        /// <summary>
        /// 查询结果转换为不同集合类型
        /// 
        /// 业务场景：
        /// - ToArray: 转为数组（适合批量处理）
        /// - ToDictionary: 转为字典（适合快速查找）
        /// - ToDics: 转为字典列表（适合动态列）
        /// </summary>
        private static void RunCollectionConvert()
        {
            Console.WriteLine("【4】集合转换 - Array/Dictionary");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. ToList - 获取活跃用户列表
                var activeUsers = FastRead.Query<User>(u => u.IsActive)
                    .ToList<User>();
                var activeUserIds = activeUsers.Select(u => u.Id).ToArray();

                Console.WriteLine($"  活跃用户ID数组: [{string.Join(", ", activeUserIds.Take(5))}...]");

                // 2. 批量处理场景：获取待处理订单ID数组
                var pendingOrders = FastRead.Query<Order>(o => o.Status == 0)
                    .ToList<User>();
                var pendingOrderIds = pendingOrders.Select(o => o.Id).ToArray();

                Console.WriteLine($"  待处理订单ID数组: [{string.Join(", ", pendingOrderIds.Take(5))}...]");

                // 3. ToDictionary - 用户ID到用户名的映射
                var userList = FastRead.Query<User>(u => u.IsActive)
                    .Take(10)
                    .ToList<User>();
                var userDict = userList.ToDictionary(u => u.Id, u => u.UserName);

                Console.WriteLine($"  用户字典: {userDict.Count} 项");
                foreach (var kv in userDict.Take(3))
                {
                    Console.WriteLine($"    [{kv.Key}] = {kv.Value}");
                }

                // 4. ToDics - 动态列场景
                var userDics = FastRead.Query<User>(u => u.IsActive)
                    .Take(5)
                    .ToDics();

                Console.WriteLine($"  字典列表: {userDics.Count} 条");
                foreach (var dic in userDics.Take(2))
                {
                    Console.WriteLine($"    {string.Join(", ", dic.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }

                // 5. ToCount - 统计总数
                var totalActive = FastRead.Query<User>(u => u.IsActive).ToCount();
                var totalOrders = FastRead.Query<Order>(o => o.Id > 0).ToCount();

                Console.WriteLine($"  活跃用户数: {totalActive}");
                Console.WriteLine($"  订单总数: {totalOrders}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  转换异常: {ex.Message}");
            }
        }

        #endregion
    }
}
