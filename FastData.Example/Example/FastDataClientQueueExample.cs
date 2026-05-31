#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastData;
using FastData.Queue;
using FastData.Example.Model;
using NewLife.Caching;

namespace FastData.Example.Example
{
    /// <summary>
    /// FastDataClient 消息队列完整示例
    /// 
    /// 演示使用 FastDataClient 统一入口进行消息队列操作：
    /// 1. 配置消息队列
    /// 2. 链式写入（自动降级）
    /// 3. 链式查询（异步推送）
    /// 4. 批量操作
    /// 5. 组合使用场景
    /// </summary>
    public static class FastDataClientQueueExample
    {
        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  FastDataClient 消息队列示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 创建 Redis 实例
            var redis = new FullRedis
            {
                Server = "127.0.0.1:6379",
                Db = 7,
                Timeout = 15000
            };

            // 创建客户端实例（带消息队列支持）
            var client = new FastDataClient("db1", poolConfig: null, redis: redis);

            RunConfigureExample(client);
            Console.WriteLine();
            RunWriteQueueExample(client);
            Console.WriteLine();
            RunReadQueueExample(client);
            Console.WriteLine();
            RunBatchExample(client);
            Console.WriteLine();
            RunCombinedExample(client);
        }

        #region 配置示例

        /// <summary>
        /// 配置消息队列示例
        /// </summary>
        private static void RunConfigureExample(FastDataClient client)
        {
            Console.WriteLine("【1】配置消息队列");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 配置 User 表启用可信队列（削峰场景）
                client.ConfigureQueue<User>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.ReliableQueue,
                    EnableFallback = true,      // 启用降级（数据库异常时自动切换到队列）
                    EnableAutoRecovery = true,  // 启用自动恢复（数据库恢复后自动切回）
                    Topic = "users:write",
                    BatchFlushSize = 50,        // 批量写入阈值
                    RecoveryIntervalSeconds = 30 // 恢复检查间隔（秒）
                });
                Console.WriteLine("  已配置 User 表启用可信队列");

                // 2. 配置 Order 表启用 Stream（多方推送场景）
                client.ConfigureQueue<Order>(new WriteBehindConfig
                {
                    QueueType = WriteBehindQueueType.Stream,
                    EnableFallback = true,
                    Topic = "orders:sync"
                });
                Console.WriteLine("  已配置 Order 表启用 Stream");

                // 3. 检查配置状态
                var userQueueEnabled = client.IsQueueEnabled<User>();
                var orderQueueEnabled = client.IsQueueEnabled<Order>();
                Console.WriteLine($"  User 队列启用: {userQueueEnabled}");
                Console.WriteLine($"  Order 队列启用: {orderQueueEnabled}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region 写入队列示例

        /// <summary>
        /// 写入队列示例（自动降级）
        /// </summary>
        private static void RunWriteQueueExample(FastDataClient client)
        {
            Console.WriteLine("【2】写入队列（自动降级）");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 单条写入
                Console.WriteLine("  2.1 单条写入:");
                var user1 = new User
                {
                    UserName = "张三",
                    Email = "zhangsan@example.com",
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                var result1 = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"source", "FastDataClientQueueExample"},
                        {"operation", "single-add"}
                    })
                    .Add(user1)
                    .Execute();

                Console.WriteLine($"    结果: Success={result1.Success}");
                Console.WriteLine($"    直接写入: {result1.DirectWriteCount}");
                Console.WriteLine($"    队列写入: {result1.QueuedCount}");
                Console.WriteLine($"    降级发生: {result1.FallbackOccurred}");

                // 2. 批量写入（带元数据）
                Console.WriteLine("\n  2.2 批量写入（带元数据）:");
                var users = new List<User>
                {
                    new User { UserName = "李四", Email = "lisi@example.com", IsActive = true, CreateTime = DateTime.Now },
                    new User { UserName = "王五", Email = "wangwu@example.com", IsActive = true, CreateTime = DateTime.Now },
                    new User { UserName = "赵六", Email = "zhaoliu@example.com", IsActive = false, CreateTime = DateTime.Now }
                };

                var result2 = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"source", "FastDataClientQueueExample"},
                        {"batchId", Guid.NewGuid().ToString("N").Substring(0, 8)},
                        {"operator", "system"}
                    })
                    .Add(users[0], new Dictionary<string, object> { {"priority", "high"} })
                    .Add(users[1])
                    .Add(users[2])
                    .Execute();

                Console.WriteLine($"    结果: Success={result2.Success}");
                Console.WriteLine($"    直接写入: {result2.DirectWriteCount}");
                Console.WriteLine($"    队列写入: {result2.QueuedCount}");

                // 3. 混合操作（添加、更新、删除）
                Console.WriteLine("\n  2.3 混合操作:");
                var result3 = client.WriteQueue()
                    .Add(new User { UserName = "新用户", Email = "new@example.com", IsActive = true, CreateTime = DateTime.Now })
                    .Update(new User { Id = 1, UserName = "更新用户" })
                    .Delete(new User { Id = 999 })
                    .Execute();

                Console.WriteLine($"    结果: Success={result3.Success}");
                Console.WriteLine($"    总操作数: {result3.DirectWriteCount + result3.QueuedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region 读取队列示例

        /// <summary>
        /// 读取队列示例（异步推送）
        /// </summary>
        private static void RunReadQueueExample(FastDataClient client)
        {
            Console.WriteLine("【3】读取队列（异步推送）");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 查询列表
                Console.WriteLine("  3.1 查询列表:");
                var result1 = client.ReadQueue<User>()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"requestId", Guid.NewGuid().ToString("N").Substring(0, 8)},
                        {"source", "FastDataClientQueueExample"}
                    })
                    .QueryList(u => u.IsActive, metadata: new Dictionary<string, object>
                    {
                        {"queryType", "active-users"}
                    })
                    .Execute();

                Console.WriteLine($"    结果: Success={result1.Success}");
                Console.WriteLine($"    推送到队列: {result1.QueuedCount}");

                // 2. 查询数量
                Console.WriteLine("\n  3.2 查询数量:");
                var result2 = client.ReadQueue<User>()
                    .QueryCount(u => u.IsActive, metadata: new Dictionary<string, object>
                    {
                        {"queryType", "count-active"}
                    })
                    .Execute();

                Console.WriteLine($"    结果: Success={result2.Success}");
                Console.WriteLine($"    推送到队列: {result2.QueuedCount}");

                // 3. 分页查询
                Console.WriteLine("\n  3.3 分页查询:");
                var result3 = client.ReadQueue<User>()
                    .QueryPaging(1, 10, u => u.IsActive, u => u.CreateTime, false,
                        new Dictionary<string, object>
                        {
                            {"queryType", "paged-list"}
                        })
                    .Execute();

                Console.WriteLine($"    结果: Success={result3.Success}");
                Console.WriteLine($"    推送到队列: {result3.QueuedCount}");

                // 4. 多个查询组合
                Console.WriteLine("\n  3.4 多个查询组合:");
                var result4 = client.ReadQueue<User>()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"requestId", Guid.NewGuid().ToString("N").Substring(0, 8)},
                        {"source", "FastDataClientQueueExample"}
                    })
                    .QuerySingle(u => u.Id == 1, metadata: new Dictionary<string, object> { {"queryType", "single"} })
                    .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "list"} })
                    .QueryCount(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "count"} })
                    .QueryPaging(1, 5, u => u.IsActive, u => u.Id, true, new Dictionary<string, object> { {"queryType", "paging"} })
                    .Execute();

                Console.WriteLine($"    结果: Success={result4.Success}");
                Console.WriteLine($"    推送到队列: {result4.QueuedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region 批量操作示例

        /// <summary>
        /// 批量操作示例
        /// </summary>
        private static void RunBatchExample(FastDataClient client)
        {
            Console.WriteLine("【4】批量操作");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 使用 AddRange 批量添加
                Console.WriteLine("  4.1 批量添加:");
                var users = Enumerable.Range(1, 10)
                    .Select(i => new User
                    {
                        UserName = $"批量用户_{i}",
                        Email = $"batch{i}@example.com",
                        IsActive = i % 2 == 0,
                        CreateTime = DateTime.Now
                    })
                    .ToList();

                var result1 = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"source", "batch-test"},
                        {"batchSize", users.Count}
                    })
                    .AddRange(users)
                    .Execute();

                Console.WriteLine($"    结果: Success={result1.Success}");
                Console.WriteLine($"    直接写入: {result1.DirectWriteCount}");
                Console.WriteLine($"    队列写入: {result1.QueuedCount}");

                // 2. 批量读取查询
                Console.WriteLine("\n  4.2 批量读取查询:");
                var result2 = client.ReadQueue<User>()
                    .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "active"} })
                    .QueryList(u => !u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "inactive"} })
                    .QueryCount(u => u.Id > 0, metadata: new Dictionary<string, object> { {"queryType", "total"} })
                    .Execute();

                Console.WriteLine($"    结果: Success={result2.Success}");
                Console.WriteLine($"    推送到队列: {result2.QueuedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region 组合使用场景

        /// <summary>
        /// 组合使用场景
        /// </summary>
        private static void RunCombinedExample(FastDataClient client)
        {
            Console.WriteLine("【5】组合使用场景");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 场景：用户注册流程
                Console.WriteLine("  5.1 用户注册流程:");

                // 5.1.1 创建用户
                var newUser = new User
                {
                    UserName = "新注册用户",
                    Email = "newuser@example.com",
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                var createResult = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"operation", "user-registration"},
                        {"source", "registration-form"}
                    })
                    .Add(newUser)
                    .Execute();

                Console.WriteLine($"    创建用户: Success={createResult.Success}");

                // 5.1.2 查询用户列表
                var queryResult = client.ReadQueue<User>()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"operation", "refresh-user-list"},
                        {"trigger", "user-registration"}
                    })
                    .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "active-users"} })
                    .QueryCount(u => u.IsActive, metadata: new Dictionary<string, object> { {"queryType", "active-count"} })
                    .Execute();

                Console.WriteLine($"    查询用户: Success={queryResult.Success}");

                // 场景：订单处理流程
                Console.WriteLine("\n  5.2 订单处理流程:");

                // 5.2.1 创建订单
                var order = new Order
                {
                    UserId = 1,
                    OrderNo = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                    TotalAmount = 199.99m,
                    Status = 0, // 待支付
                    CreateTime = DateTime.Now
                };

                var orderResult = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"operation", "create-order"},
                        {"userId", order.UserId},
                        {"orderNo", order.OrderNo}
                    })
                    .Add(order)
                    .Execute();

                Console.WriteLine($"    创建订单: Success={orderResult.Success}");

                // 5.2.2 查询订单统计
                var orderQueryResult = client.ReadQueue<Order>()
                    .QueryCount(o => o.Status == 0, metadata: new Dictionary<string, object> { {"queryType", "pending-count"} })
                    .QueryCount(o => o.Status == 1, metadata: new Dictionary<string, object> { {"queryType", "paid-count"} })
                    .QueryCount(o => o.Status == 3, metadata: new Dictionary<string, object> { {"queryType", "completed-count"} })
                    .Execute();

                Console.WriteLine($"    查询订单统计: Success={orderQueryResult.Success}");

                // 场景：批量数据同步
                Console.WriteLine("\n  5.3 批量数据同步:");

                var syncUsers = Enumerable.Range(1, 5)
                    .Select(i => new User
                    {
                        UserName = $"同步用户_{i}",
                        Email = $"sync{i}@example.com",
                        IsActive = true,
                        CreateTime = DateTime.Now
                    })
                    .ToList();

                var syncResult = client.WriteQueue()
                    .WithMetadata(new Dictionary<string, object>
                    {
                        {"operation", "data-sync"},
                        {"source", "external-system"},
                        {"syncTime", DateTime.Now}
                    })
                    .AddRange(syncUsers)
                    .Execute();

                Console.WriteLine($"    批量同步: Success={syncResult.Success}");
                Console.WriteLine($"    直接写入: {syncResult.DirectWriteCount}");
                Console.WriteLine($"    队列写入: {syncResult.QueuedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion
    }
}
#endif
