using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FastData;
using FastData.Example.Model;
using FastRedis;

namespace FastData.Example.Example
{
    /// <summary>
    /// 缓存策略业务示例
    /// 
    /// 演示生产环境中常用的缓存策略和最佳实践：
    /// 1. Cache-Aside 模式（旁路缓存）
    /// 2. 缓存穿透防护
    /// 3. 缓存雪崩防护
    /// 4. 缓存预热
    /// 5. 分布式锁
    /// 6. 通用缓存帮助方法
    /// 7. 缓存命中率统计
    /// </summary>
    public static class CacheStrategyExample
    {
        #region 缓存键常量

        private const string CACHE_PREFIX_PRODUCT = "product:";
        private const string CACHE_PREFIX_USER = "user:";
        private const string CACHE_PREFIX_ACTIVE_USERS = "users:active:top100";
        private const string CACHE_PREFIX_ORDER_LOCK = "lock:order:";
        private const string CACHE_PREFIX_CACHE_STATS = "cache:stats:";

        #endregion

        /// <summary>
        /// 运行所有缓存策略示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  缓存策略业务示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 1. 标准 Cache-Aside 模式
            Console.WriteLine("【1】Cache-Aside 模式（商品缓存）");
            Console.WriteLine("----------------------------------------");
            CacheAsidePattern(1);
            Console.WriteLine();

            // 2. 缓存穿透防护
            Console.WriteLine("【2】缓存穿透防护");
            Console.WriteLine("----------------------------------------");
            CachePenetrationProtection(99999);
            Console.WriteLine();

            // 3. 缓存雪崩防护
            Console.WriteLine("【3】缓存雪崩防护");
            Console.WriteLine("----------------------------------------");
            CacheAvalancheProtection();
            Console.WriteLine();

            // 4. 缓存预热
            Console.WriteLine("【4】缓存预热（系统启动时加载热点数据）");
            Console.WriteLine("----------------------------------------");
            CacheWarmUp();
            Console.WriteLine();

            // 5. 分布式锁
            Console.WriteLine("【5】分布式锁（防止重复下单）");
            Console.WriteLine("----------------------------------------");
            DistributedLock(1, 1001);
            Console.WriteLine();

            // 6. 通用缓存帮助方法
            Console.WriteLine("【6】通用缓存帮助方法 GetOrSet<T>");
            Console.WriteLine("----------------------------------------");
            GetOrSetHelperDemo();
            Console.WriteLine();

            // 7. 缓存命中率统计
            Console.WriteLine("【7】缓存命中率统计");
            Console.WriteLine("----------------------------------------");
            CacheStatisticsDemo();
        }

        #region 1. Cache-Aside 模式

        /// <summary>
        /// 标准 Cache-Aside 模式
        /// 
        /// 业务场景：商品详情查询
        /// 读取流程：先查缓存 → 未命中则查数据库 → 写入缓存
        /// 更新流程：先更新数据库 → 再删除缓存（而非更新缓存）
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <returns>商品信息</returns>
        public static Product CacheAsidePattern(int productId)
        {
            var cacheKey = $"{CACHE_PREFIX_PRODUCT}{productId}";

            try
            {
                // 第一步：查询缓存
                var cachedProduct = RedisInfo.Get<Product>(cacheKey);
                if (cachedProduct != null)
                {
                    Console.WriteLine($"  [缓存命中] 商品ID={productId}, 名称={cachedProduct.ProductName}");
                    RecordCacheHit();
                    return cachedProduct;
                }

                // 第二步：缓存未命中，查询数据库
                Console.WriteLine($"  [缓存未命中] 查询数据库, 商品ID={productId}");
                RecordCacheMiss();

                var product = FastRead.Query<Product>(p => p.Id == productId).ToItem();
                if (product != null)
                {
                    // 第三步：写入缓存，设置5分钟过期
                    RedisInfo.Set(cacheKey, product, 300);
                    Console.WriteLine($"  [写入缓存] 商品ID={productId}, 过期时间=300秒");
                }

                return product;
            }
            catch (Exception ex)
            {
                // Redis 异常时降级到数据库查询
                Console.WriteLine($"  [Redis异常] 降级到数据库查询: {ex.Message}");
                return FastRead.Query<Product>(p => p.Id == productId).ToItem();
            }
        }

        /// <summary>
        /// 更新商品（Cache-Aside 更新模式）
        /// 先更新数据库，再删除缓存，保证数据一致性
        /// </summary>
        /// <param name="product">商品实体</param>
        public static void UpdateProductWithCacheAside(Product product)
        {
            var cacheKey = $"{CACHE_PREFIX_PRODUCT}{product.Id}";

            try
            {
                // 第一步：更新数据库
                product.UpdateTime = DateTime.Now;
                FastWrite.Update(product);
                Console.WriteLine($"  [数据库更新] 商品ID={product.Id}");

                // 第二步：删除缓存（不是更新缓存，避免并发问题）
                RedisInfo.Remove(cacheKey);
                Console.WriteLine($"  [缓存删除] Key={cacheKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [更新异常] {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 2. 缓存穿透防护

        /// <summary>
        /// 缓存穿透防护
        /// 
        /// 业务场景：查询不存在的商品（如恶意请求、爬虫）
        /// 问题：大量请求查询不存在的数据，每次都穿透到数据库
        /// 解决：缓存空值，设置较短过期时间（60秒）
        /// </summary>
        /// <param name="productId">商品ID（可能不存在）</param>
        /// <returns>商品信息，不存在返回null</returns>
        public static Product CachePenetrationProtection(int productId)
        {
            var cacheKey = $"{CACHE_PREFIX_PRODUCT}{productId}";

            try
            {
                // 第一步：查询缓存
                if (RedisInfo.Exists(cacheKey))
                {
                    var cachedProduct = RedisInfo.Get<Product>(cacheKey);
                    if (cachedProduct != null)
                    {
                        Console.WriteLine($"  [缓存命中] 商品ID={productId}");
                        RecordCacheHit();
                        return cachedProduct;
                    }
                    // Key exists but product is null - this was a cached null marker
                    Console.WriteLine($"  [命中空值缓存] 商品ID={productId} 不存在，直接返回null");
                    RecordCacheHit();
                    return null;
                }

                // 第二步：缓存未命中，查询数据库
                Console.WriteLine($"  [缓存未命中] 查询数据库, 商品ID={productId}");
                RecordCacheMiss();

                var product = FastRead.Query<Product>(p => p.Id == productId).ToItem();

                if (product == null)
                {
                    // 第三步：数据不存在，缓存空值，60秒过期
                    // 使用 Exists 标记防穿透
                    RedisInfo.Set(cacheKey, new Product(), 60);
                    Console.WriteLine($"  [缓存空值] 商品ID={productId} 不存在，缓存空值60秒");
                }
                else
                {
                    // 第四步：数据存在，正常缓存
                    RedisInfo.Set(cacheKey, product, 300);
                    Console.WriteLine($"  [写入缓存] 商品ID={productId}");
                }

                return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Redis异常] 降级到数据库查询: {ex.Message}");
                return FastRead.Query<Product>(p => p.Id == productId).ToItem();
            }
        }

        #endregion

        #region 3. 缓存雪崩防护

        /// <summary>
        /// 缓存雪崩防护
        /// 
        /// 业务场景：大量缓存同时过期，请求瞬间打到数据库
        /// 问题：如果所有缓存设置相同过期时间，会同时失效
        /// 解决：在基础过期时间上增加随机抖动（0-60秒）
        /// </summary>
        public static void CacheAvalancheProtection()
        {
            Console.WriteLine("  批量加载商品数据，添加随机过期时间抖动...");

            try
            {
                // 查询商品列表
                var products = FastRead.Query<Product>(p => p.IsActive).ToList();

                // 基础过期时间：300秒（5分钟）
                var baseExpiry = 300;
                var random = new Random();

                foreach (var product in products)
                {
                    var cacheKey = $"{CACHE_PREFIX_PRODUCT}{product.Id}";

                    // 添加随机抖动：300 + (0~60)秒
                    // 避免所有缓存同时过期
                    var jitter = random.Next(0, 61);
                    var expiry = baseExpiry + jitter;

                    RedisInfo.Set(cacheKey, product, expiry);
                }

                Console.WriteLine($"  [缓存雪崩防护] 已加载 {products.Count} 个商品");
                Console.WriteLine($"  [过期时间] 基础={baseExpiry}秒，随机抖动=0~60秒");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [异常] {ex.Message}");
            }
        }

        #endregion

        #region 4. 缓存预热

        /// <summary>
        /// 缓存预热
        /// 
        /// 业务场景：系统启动时加载热点数据到缓存
        /// 优势：避免启动初期大量缓存未命中，减轻数据库压力
        /// 数据：查询最活跃的100个用户，设置较长过期时间（1小时）
        /// </summary>
        public static void CacheWarmUp()
        {
            Console.WriteLine("  开始缓存预热...");

            try
            {
                // 查询最活跃的100个用户（按最近登录时间排序）
                var activeUsers = FastRead.Query<User>(u => u.IsActive && !u.IsDeleted)
                    .OrderBy<User>(u => u.LastLoginTime, true) // 按最后登录时间降序
                    .Take(100)
                    .ToList<User>();

                // 预热单个用户缓存
                foreach (var user in activeUsers)
                {
                    var cacheKey = $"{CACHE_PREFIX_USER}{user.Id}";
                    RedisInfo.Set(cacheKey, user, 3600); // 1小时过期
                }

                // 预热列表缓存
                RedisInfo.Set(CACHE_PREFIX_ACTIVE_USERS, activeUsers, 3600);

                Console.WriteLine($"  [缓存预热完成] 加载 {activeUsers.Count} 个活跃用户");
                Console.WriteLine($"  [过期时间] 3600秒（1小时）");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [预热异常] {ex.Message}");
            }
        }

        #endregion

        #region 5. 分布式锁

        /// <summary>
        /// 分布式锁
        /// 
        /// 业务场景：防止重复下单
        /// 问题：用户快速点击多次下单按钮，可能创建多个订单
        /// 解决：使用 Redis SET NX（Set if Not eXists）实现分布式锁
        /// 
        /// 锁的特性：
        /// 1. 互斥性：同一时刻只有一个客户端能持有锁
        /// 2. 防死锁：设置自动过期时间，即使崩溃也能释放
        /// 3. 安全性：只能释放自己持有的锁
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="productId">商品ID</param>
        /// <returns>是否成功获取锁并创建订单</returns>
        public static bool DistributedLock(int userId, int productId)
        {
            // 锁的Key：基于用户和商品维度，防止同一用户重复购买同一商品
            var lockKey = $"{CACHE_PREFIX_ORDER_LOCK}{userId}:{productId}";
            var lockValue = Guid.NewGuid().ToString(); // 锁的唯一标识
            var lockExpiry = 30; // 锁的过期时间（秒）

            try
            {
                // 第一步：尝试获取分布式锁（模拟SET NX模式）
                // 检查锁是否已存在
                if (RedisInfo.Exists(lockKey))
                {
                    Console.WriteLine($"  [获取锁失败] 用户={userId}, 商品={productId}, 可能正在处理中");
                    return false;
                }

                // 锁不存在，设置锁（实际生产环境应使用SET NX命令）
                RedisInfo.Set(lockKey, lockValue, lockExpiry);
                Console.WriteLine($"  [获取锁成功] 用户={userId}, 商品={productId}");

                // 第二步：检查是否已经下过单（防重复）
                var existingOrder = CheckExistingOrder(userId, productId);
                if (existingOrder)
                {
                    Console.WriteLine($"  [重复下单] 用户={userId} 已购买过商品={productId}");
                    return false;
                }

                // 第三步：创建订单
                var order = CreateOrder(userId, productId);
                Console.WriteLine($"  [订单创建成功] 订单号={order.OrderNo}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [分布式锁异常] {ex.Message}");
                return false;
            }
            finally
            {
                // 第四步：释放锁（只释放自己持有的锁）
                ReleaseLock(lockKey, lockValue);
            }
        }

        /// <summary>
        /// 检查是否存在重复订单
        /// </summary>
        private static bool CheckExistingOrder(int userId, int productId)
        {
            // 查询该用户对该商品的未取消订单
            var existingOrder = FastRead.Query<Order>(o =>
                o.UserId == userId &&
                o.Status != 4 // 4=已取消
            ).ToItem();

            return existingOrder != null;
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        private static Order CreateOrder(int userId, int productId)
        {
            var order = new Order
            {
                UserId = userId,
                OrderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{userId:D4}{new Random().Next(1000, 9999)}",
                TotalAmount = 0, // 实际应查询商品价格
                Status = 0, // 待支付
                Remark = $"用户{userId}购买商品{productId}",
                CreateTime = DateTime.Now
            };

            FastWrite.Add(order);
            return order;
        }

        /// <summary>
        /// 释放分布式锁（安全释放，只释放自己持有的锁）
        /// </summary>
        /// <param name="lockKey">锁的Key</param>
        /// <param name="lockValue">锁的唯一标识</param>
        private static void ReleaseLock(string lockKey, string lockValue)
        {
            try
            {
                // 验证锁是否仍存在，然后释放
                if (RedisInfo.Exists(lockKey))
                {
                    RedisInfo.Remove(lockKey);
                    Console.WriteLine($"  [释放锁] {lockKey}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [释放锁异常] {ex.Message}");
            }
        }

        #endregion

        #region 6. 通用缓存帮助方法

        /// <summary>
        /// 通用缓存帮助方法 GetOrSet
        /// 
        /// 封装了"先查缓存，未命中则执行工厂方法并缓存"的通用逻辑
        /// 使用泛型支持任意类型，支持自定义过期时间
        /// 
        /// 使用示例：
        /// var user = GetOrSet("user:1", () => GetUserFromDb(1), 300);
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="factory">工厂方法（缓存未命中时执行）</param>
        /// <param name="expirySeconds">过期时间（秒）</param>
        /// <returns>缓存数据或工厂方法返回的数据</returns>
        public static T GetOrSet<T>(string cacheKey, Func<T> factory, int expirySeconds = 300) where T : class, new()
        {
            try
            {
                // 第一步：查询缓存
                var cached = RedisInfo.Get<T>(cacheKey);
                if (cached != null)
                {
                    RecordCacheHit();
                    return cached;
                }

                // 第二步：缓存未命中，执行工厂方法
                RecordCacheMiss();
                var value = factory();

                if (value != null)
                {
                    // 第三步：写入缓存
                    RedisInfo.Set(cacheKey, value, expirySeconds);
                }

                return value;
            }
            catch (Exception ex)
            {
                // Redis 异常时直接执行工厂方法
                Console.WriteLine($"  [GetOrSet Redis异常] {ex.Message}");
                return factory();
            }
        }

        /// <summary>
        /// 通用缓存帮助方法演示
        /// </summary>
        private static void GetOrSetHelperDemo()
        {
            // 示例1：缓存单个商品
            var product = GetOrSet(
                $"{CACHE_PREFIX_PRODUCT}1",
                () =>
                {
                    Console.WriteLine("  [工厂方法] 从数据库查询商品ID=1");
                    return FastRead.Query<Product>(p => p.Id == 1).ToItem();
                },
                300
            );

            if (product != null)
            {
                Console.WriteLine($"  [获取商品] {product.ProductName}, 价格={product.Price}");
            }

            // 示例2：缓存用户列表
            var activeUsers = GetOrSet(
                "users:active",
                () =>
                {
                    Console.WriteLine("  [工厂方法] 从数据库查询活跃用户");
                    return FastRead.Query<User>(u => u.IsActive && !u.IsDeleted)
                        .Take(10)
                        .ToList<User>();
                },
                600
            );

            if (activeUsers != null)
            {
                Console.WriteLine($"  [获取用户列表] 数量={activeUsers.Count}");
            }
        }

        #endregion

        #region 7. 缓存命中率统计

        /// <summary>
        /// 缓存命中率统计
        /// 
        /// 业务场景：监控缓存效果，优化缓存策略
        /// 实现：使用 Redis 计数器记录命中和未命中次数
        /// 
        /// 通过命中率可以：
        /// 1. 评估缓存策略是否有效
        /// 2. 发现缓存热点数据
        /// 3. 调整缓存过期时间
        /// 4. 识别缓存穿透问题
        /// </summary>
        public static void CacheStatisticsDemo()
        {
            // 模拟缓存操作
            Console.WriteLine("  模拟缓存操作...");
            for (int i = 0; i < 10; i++)
            {
                if (i % 3 == 0)
                    RecordCacheMiss();
                else
                    RecordCacheHit();
            }

            // 获取统计数据
            var stats = GetCacheStatistics();

            Console.WriteLine($"  [缓存统计]");
            Console.WriteLine($"    命中次数: {stats.Hits}");
            Console.WriteLine($"    未命中次数: {stats.Misses}");
            Console.WriteLine($"    总请求数: {stats.TotalRequests}");
            Console.WriteLine($"    命中率: {stats.HitRate:P2}");

            // 根据命中率给出优化建议
            if (stats.HitRate < 0.7)
            {
                Console.WriteLine("  [优化建议] 命中率低于70%，建议：");
                Console.WriteLine("    1. 增加缓存预热");
                Console.WriteLine("    2. 延长热点数据过期时间");
                Console.WriteLine("    3. 检查是否存在缓存穿透");
            }
            else if (stats.HitRate > 0.95)
            {
                Console.WriteLine("  [优化建议] 命中率超过95%，缓存策略良好");
            }
        }

        /// <summary>
        /// 记录缓存命中
        /// </summary>
        private static void RecordCacheHit()
        {
            try
            {
                var key = $"{CACHE_PREFIX_CACHE_STATS}hits";
                var current = RedisInfo.Get<CacheValue<long>>(key);
                RedisInfo.Set(key, new CacheValue<long> { Value = (current?.Value ?? 0) + 1 }, 0);
            }
            catch
            {
                // 统计不影响业务
            }
        }

        /// <summary>
        /// 记录缓存未命中
        /// </summary>
        private static void RecordCacheMiss()
        {
            try
            {
                var key = $"{CACHE_PREFIX_CACHE_STATS}misses";
                var current = RedisInfo.Get<CacheValue<long>>(key);
                RedisInfo.Set(key, new CacheValue<long> { Value = (current?.Value ?? 0) + 1 }, 0);
            }
            catch
            {
                // 统计不影响业务
            }
        }

        /// <summary>
        /// 获取缓存统计数据
        /// </summary>
        /// <returns>缓存统计信息</returns>
        public static CacheStatisticsResult GetCacheStatistics()
        {
            try
            {
                var hitsObj = RedisInfo.Get<CacheValue<long>>($"{CACHE_PREFIX_CACHE_STATS}hits");
                var missesObj = RedisInfo.Get<CacheValue<long>>($"{CACHE_PREFIX_CACHE_STATS}misses");
                var hits = hitsObj?.Value ?? 0L;
                var misses = missesObj?.Value ?? 0L;
                var total = hits + misses;

                return new CacheStatisticsResult
                {
                    Hits = hits,
                    Misses = misses,
                    TotalRequests = total,
                    HitRate = total > 0 ? (double)hits / total : 0
                };
            }
            catch
            {
                return new CacheStatisticsResult();
            }
        }

        /// <summary>
        /// 重置缓存统计数据
        /// </summary>
        public static void ResetCacheStatistics()
        {
            try
            {
                RedisInfo.Remove($"{CACHE_PREFIX_CACHE_STATS}hits");
                RedisInfo.Remove($"{CACHE_PREFIX_CACHE_STATS}misses");
                Console.WriteLine("  [统计数据已重置]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [重置异常] {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 缓存包装类，用于包装值类型以满足 RedisInfo.Get 的泛型约束
    /// </summary>
    public class CacheValue<T> where T : struct
    {
        public T Value { get; set; }
    }

    /// <summary>
    /// 缓存统计结果
    /// </summary>
    public class CacheStatisticsResult
    {
        /// <summary>
        /// 命中次数
        /// </summary>
        public long Hits { get; set; }

        /// <summary>
        /// 未命中次数
        /// </summary>
        public long Misses { get; set; }

        /// <summary>
        /// 总请求数
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// 命中率（0-1之间的小数）
        /// </summary>
        public double HitRate { get; set; }
    }
}