using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData;
using FastData.Example.Model;
using Microsoft.Extensions.Caching.Memory;

namespace FastData.Example.Example
{
    /// <summary>
    /// 高频数据缓存策略示例
    /// 
    /// 场景：高频访问的数据需要主动使用缓存，保证性能和数据一致性
    /// 
    /// 缓存类型：
    /// 1. Web 缓存（MemoryCache）- 进程内缓存，速度快，重启丢失
    /// 2. Redis 缓存 - 分布式缓存，多实例共享，持久化
    /// 
    /// 数据一致性策略：
    /// 1. Cache-Aside 模式（旁路缓存）
    /// 2. 写入时删除缓存
    /// 3. 缓存过期策略
    /// 4. 双写一致性
    /// </summary>
    public static class HighFrequencyCacheExample
    {
        #region 缓存管理器

        /// <summary>
        /// 缓存管理器 - 统一管理 Web 缓存和 Redis 缓存
        /// </summary>
        public class CacheManager : IDisposable
        {
            private readonly IMemoryCache _webCache;
            private readonly string _redisKeyPrefix;
            private readonly int _defaultExpirationMinutes;
            private long _webCacheHits;
            private long _webCacheMisses;
            private long _redisCacheHits;
            private long _redisCacheMisses;

            public CacheManager(string redisKeyPrefix = "app:", int defaultExpirationMinutes = 30)
            {
                _webCache = new MemoryCache(new MemoryCacheOptions
                {
                    SizeLimit = 10000 // 最大缓存条目数
                });
                _redisKeyPrefix = redisKeyPrefix;
                _defaultExpirationMinutes = defaultExpirationMinutes;
            }

            /// <summary>
            /// 获取缓存（优先 Web 缓存，其次 Redis）
            /// </summary>
            public T Get<T>(string key) where T : class
            {
                // 1. 先查 Web 缓存
                if (_webCache.TryGetValue(key, out T webValue))
                {
                    Interlocked.Increment(ref _webCacheHits);
                    return webValue;
                }
                Interlocked.Increment(ref _webCacheMisses);

                // 2. 再查 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                var redisValue = RedisHelper.Get<T>(redisKey);
                if (redisValue != null)
                {
                    Interlocked.Increment(ref _redisCacheHits);

                    // 回填 Web 缓存
                    SetWebCache(key, redisValue, TimeSpan.FromMinutes(5));
                    return redisValue;
                }
                Interlocked.Increment(ref _redisCacheMisses);

                return null;
            }

            /// <summary>
            /// 设置缓存（同时写入 Web 缓存和 Redis）
            /// </summary>
            public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
            {
                var exp = expiration ?? TimeSpan.FromMinutes(_defaultExpirationMinutes);

                // 1. 写入 Web 缓存
                SetWebCache(key, value, TimeSpan.FromMinutes(Math.Min(exp.TotalMinutes, 5)));

                // 2. 写入 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                RedisHelper.Set(redisKey, value, (int)exp.TotalSeconds);
            }

            /// <summary>
            /// 删除缓存
            /// </summary>
            public void Remove(string key)
            {
                // 1. 删除 Web 缓存
                _webCache.Remove(key);

                // 2. 删除 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                RedisHelper.Remove(redisKey);
            }

            /// <summary>
            /// 批量删除缓存（按前缀）
            /// </summary>
            public void RemoveByPrefix(string prefix)
            {
                // Redis 按前缀删除
                var redisPrefix = $"{_redisKeyPrefix}{prefix}";
                RedisHelper.RemoveByPrefix(redisPrefix);

                // Web 缓存需要遍历（MemoryCache 不支持按前缀删除）
                // 实际项目中可以维护一个 key 列表
            }

            /// <summary>
            /// 获取或添加缓存（Cache-Aside 模式）
            /// </summary>
            public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiration = null) where T : class
            {
                var value = Get<T>(key);
                if (value != null)
                    return value;

                // 从数据源获取
                value = factory();
                if (value != null)
                {
                    Set(key, value, expiration);
                }
                return value;
            }

            /// <summary>
            /// 获取或添加缓存（支持值类型）
            /// </summary>
            public int GetOrAddInt(string key, Func<int> factory, TimeSpan? expiration = null)
            {
                // 查 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                var redisValue = RedisHelper.Get<object>(redisKey);
                if (redisValue != null && int.TryParse(redisValue.ToString(), out var cachedValue))
                {
                    Interlocked.Increment(ref _redisCacheHits);
                    return cachedValue;
                }
                Interlocked.Increment(ref _redisCacheMisses);

                // 从数据源获取
                var result = factory();
                SetInt(key, result, expiration);
                return result;
            }

            /// <summary>
            /// 设置缓存（支持值类型）
            /// </summary>
            public void SetInt(string key, int value, TimeSpan? expiration = null)
            {
                var exp = expiration ?? TimeSpan.FromMinutes(_defaultExpirationMinutes);

                // 写入 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                RedisHelper.Set(redisKey, value.ToString(), (int)exp.TotalSeconds);
            }

            /// <summary>
            /// 获取缓存（支持值类型）
            /// </summary>
            public int? GetInt(string key)
            {
                // 查 Redis 缓存
                var redisKey = $"{_redisKeyPrefix}{key}";
                var redisValue = RedisHelper.Get<object>(redisKey);
                if (redisValue != null && int.TryParse(redisValue.ToString(), out var value))
                {
                    Interlocked.Increment(ref _redisCacheHits);
                    return value;
                }
                Interlocked.Increment(ref _redisCacheMisses);

                return null;
            }

            /// <summary>
            /// 获取缓存统计信息
            /// </summary>
            public CacheStatistics GetStatistics()
            {
                return new CacheStatistics
                {
                    WebCacheHits = _webCacheHits,
                    WebCacheMisses = _webCacheMisses,
                    RedisCacheHits = _redisCacheHits,
                    RedisCacheMisses = _redisCacheMisses,
                    WebCacheHitRate = _webCacheHits + _webCacheMisses > 0
                        ? (double)_webCacheHits / (_webCacheHits + _webCacheMisses) * 100
                        : 0,
                    RedisCacheHitRate = _redisCacheHits + _redisCacheMisses > 0
                        ? (double)_redisCacheHits / (_redisCacheHits + _redisCacheMisses) * 100
                        : 0
                };
            }

            private void SetWebCache<T>(string key, T value, TimeSpan expiration) where T : class
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration,
                    Size = 1,
                    Priority = CacheItemPriority.Normal
                };

                // 监听缓存过期事件（可选）
                options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    if (reason == EvictionReason.Expired)
                    {
                        // 缓存过期，可以触发异步刷新
                        Console.WriteLine($"    [缓存过期] {evictedKey}");
                    }
                });

                _webCache.Set(key, value, options);
            }

            public void Dispose()
            {
                _webCache?.Dispose();
            }
        }

        /// <summary>
        /// 缓存统计信息
        /// </summary>
        public class CacheStatistics
        {
            public long WebCacheHits { get; set; }
            public long WebCacheMisses { get; set; }
            public long RedisCacheHits { get; set; }
            public long RedisCacheMisses { get; set; }
            public double WebCacheHitRate { get; set; }
            public double RedisCacheHitRate { get; set; }
        }

        #endregion

        #region Redis Helper 模拟

        /// <summary>
        /// Redis Helper 模拟（实际项目中使用真实的 Redis 客户端）
        /// </summary>
        public static class RedisHelper
        {
            private static readonly Dictionary<string, (object value, DateTime expiry)> _store
                = new Dictionary<string, (object, DateTime)>();

            public static T Get<T>(string key) where T : class
            {
                if (_store.TryGetValue(key, out var entry))
                {
                    if (entry.expiry > DateTime.Now)
                    {
                        return entry.value as T;
                    }
                    _store.Remove(key);
                }
                return null;
            }

            public static void Set(string key, object value, int expirySeconds)
            {
                _store[key] = (value, DateTime.Now.AddSeconds(expirySeconds));
            }

            public static void Remove(string key)
            {
                _store.Remove(key);
            }

            public static void RemoveByPrefix(string prefix)
            {
                var keysToRemove = _store.Keys.Where(k => k.StartsWith(prefix)).ToList();
                foreach (var key in keysToRemove)
                {
                    _store.Remove(key);
                }
            }

            public static void Clear()
            {
                _store.Clear();
            }
        }

        #endregion

        #region 缓存策略实现

        /// <summary>
        /// 用户缓存服务 - 高频数据缓存
        /// </summary>
        public class UserCacheService
        {
            private readonly CacheManager _cacheManager;
            private const string CACHE_PREFIX = "user:";
            private const string CACHE_LIST_PREFIX = "user:list:";
            private const string CACHE_COUNT_PREFIX = "user:count:";

            public UserCacheService(CacheManager cacheManager)
            {
                _cacheManager = cacheManager;
            }

            /// <summary>
            /// 获取单个用户（Cache-Aside 模式）
            /// </summary>
            public User GetUserById(int userId)
            {
                var cacheKey = $"{CACHE_PREFIX}{userId}";
                return _cacheManager.GetOrAdd(cacheKey, () =>
                {
                    // 缓存未命中，从数据库查询
                    return FastRead.Query<User>(u => u.Id == userId)
                        .FirstOrDefault();
                }, TimeSpan.FromMinutes(30));
            }

            /// <summary>
            /// 获取活跃用户列表
            /// </summary>
            public List<User> GetActiveUsers()
            {
                var cacheKey = $"{CACHE_LIST_PREFIX}active";
                return _cacheManager.GetOrAdd(cacheKey, () =>
                {
                    return FastRead.Query<User>(u => u.IsActive)
                        .OrderBy(u => u.Id)
                        .ToList();
                }, TimeSpan.FromMinutes(10));
            }

            /// <summary>
            /// 获取用户数量
            /// </summary>
            public int GetUserCount(bool isActive)
            {
                var cacheKey = $"{CACHE_COUNT_PREFIX}{(isActive ? "active" : "inactive")}";
                return _cacheManager.GetOrAddInt(cacheKey, () =>
                {
                    return FastRead.Query<User>(u => u.IsActive == isActive)
                        .Count();
                }, TimeSpan.FromMinutes(5));
            }

            /// <summary>
            /// 更新用户（写入时删除缓存）
            /// </summary>
            public bool UpdateUser(User user)
            {
                // 1. 更新数据库
                var result = FastWrite.Update(user);
                if (!result.IsSuccess)
                    return false;

                // 2. 删除相关缓存
                _cacheManager.Remove($"{CACHE_PREFIX}{user.Id}");
                _cacheManager.RemoveByPrefix(CACHE_LIST_PREFIX);
                _cacheManager.RemoveByPrefix(CACHE_COUNT_PREFIX);

                return true;
            }

            /// <summary>
            /// 添加用户（写入时删除缓存）
            /// </summary>
            public bool AddUser(User user)
            {
                // 1. 写入数据库
                var result = FastWrite.Add(user);
                if (!result.IsSuccess)
                    return false;

                // 2. 删除列表缓存（单条缓存无需删除，因为是新数据）
                _cacheManager.RemoveByPrefix(CACHE_LIST_PREFIX);
                _cacheManager.RemoveByPrefix(CACHE_COUNT_PREFIX);

                return true;
            }

            /// <summary>
            /// 删除用户（写入时删除缓存）
            /// </summary>
            public bool DeleteUser(int userId)
            {
                // 1. 删除数据库记录
                var result = FastWrite.Delete<User>(u => u.Id == userId);
                if (!result.IsSuccess)
                    return false;

                // 2. 删除相关缓存
                _cacheManager.Remove($"{CACHE_PREFIX}{userId}");
                _cacheManager.RemoveByPrefix(CACHE_LIST_PREFIX);
                _cacheManager.RemoveByPrefix(CACHE_COUNT_PREFIX);

                return true;
            }
        }

        /// <summary>
        /// 订单缓存服务 - 高频数据缓存
        /// </summary>
        public class OrderCacheService
        {
            private readonly CacheManager _cacheManager;
            private const string CACHE_PREFIX = "order:";
            private const string CACHE_USER_ORDERS_PREFIX = "order:user:";
            private const string CACHE_STATS_PREFIX = "order:stats:";

            public OrderCacheService(CacheManager cacheManager)
            {
                _cacheManager = cacheManager;
            }

            /// <summary>
            /// 获取订单详情
            /// </summary>
            public Order GetOrderById(int orderId)
            {
                var cacheKey = $"{CACHE_PREFIX}{orderId}";
                return _cacheManager.GetOrAdd(cacheKey, () =>
                {
                    return FastRead.Query<Order>(o => o.Id == orderId)
                        .FirstOrDefault();
                }, TimeSpan.FromMinutes(15));
            }

            /// <summary>
            /// 获取用户订单列表
            /// </summary>
            public List<Order> GetUserOrders(int userId)
            {
                var cacheKey = $"{CACHE_USER_ORDERS_PREFIX}{userId}";
                return _cacheManager.GetOrAdd(cacheKey, () =>
                {
                    return FastRead.Query<Order>(o => o.UserId == userId)
                        .OrderByDescending(o => o.CreateTime)
                        .Take(20)
                        .ToList<Order>();
                }, TimeSpan.FromMinutes(5));
            }

            /// <summary>
            /// 获取订单统计
            /// </summary>
            public Dictionary<int, int> GetOrderStatistics()
            {
                var cacheKey = $"{CACHE_STATS_PREFIX}status";
                return _cacheManager.GetOrAdd(cacheKey, () =>
                {
                    var stats = new Dictionary<int, int>();
                    for (int status = 0; status <= 4; status++)
                    {
                        var currentStatus = status;
                        stats[status] = FastRead.Query<Order>(o => o.Status == currentStatus)
                            .Count();
                    }
                    return stats;
                }, TimeSpan.FromMinutes(2));
            }

            /// <summary>
            /// 创建订单（写入时删除缓存）
            /// </summary>
            public bool CreateOrder(Order order)
            {
                // 1. 写入数据库
                var result = FastWrite.Add(order);
                if (!result.IsSuccess)
                    return false;

                // 2. 删除用户订单列表缓存
                _cacheManager.Remove($"{CACHE_USER_ORDERS_PREFIX}{order.UserId}");

                // 3. 删除统计缓存
                _cacheManager.RemoveByPrefix(CACHE_STATS_PREFIX);

                return true;
            }

            /// <summary>
            /// 更新订单状态（写入时删除缓存）
            /// </summary>
            public bool UpdateOrderStatus(int orderId, int newStatus)
            {
                // 1. 查询订单（获取 UserId）
                var order = GetOrderById(orderId);
                if (order == null)
                    return false;

                // 2. 更新数据库
                order.Status = newStatus;
                order.UpdateTime = DateTime.Now;
                var result = FastWrite.Update(order);
                if (!result.IsSuccess)
                    return false;

                // 3. 删除相关缓存
                _cacheManager.Remove($"{CACHE_PREFIX}{orderId}");
                _cacheManager.Remove($"{CACHE_USER_ORDERS_PREFIX}{order.UserId}");
                _cacheManager.RemoveByPrefix(CACHE_STATS_PREFIX);

                return true;
            }
        }

        #endregion

        #region 运行示例

        /// <summary>
        /// 运行所有缓存示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  高频数据缓存策略示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            using var cacheManager = new CacheManager("demo:", 30);
            var userService = new UserCacheService(cacheManager);
            var orderService = new OrderCacheService(cacheManager);

            RunWebCacheExample(cacheManager);
            Console.WriteLine();
            RunRedisCacheExample(cacheManager);
            Console.WriteLine();
            RunCacheAsideExample(userService);
            Console.WriteLine();
            RunConsistencyExample(userService, orderService);
            Console.WriteLine();
            RunCacheStatisticsExample(cacheManager);
        }

        /// <summary>
        /// Web 缓存示例（MemoryCache）
        /// </summary>
        private static void RunWebCacheExample(CacheManager cacheManager)
        {
            Console.WriteLine("【1】Web 缓存（MemoryCache）");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 设置 Web 缓存
                var user = new User
                {
                    Id = 1,
                    UserName = "张三",
                    Email = "zhangsan@example.com",
                    IsActive = true,
                    CreateTime = DateTime.Now
                };

                cacheManager.Set("user:1", user, TimeSpan.FromMinutes(10));
                Console.WriteLine("  设置缓存: user:1");

                // 2. 获取 Web 缓存
                var cachedUser = cacheManager.Get<User>("user:1");
                Console.WriteLine($"  获取缓存: {cachedUser?.UserName}");

                // 3. 缓存未命中
                var nonExistUser = cacheManager.Get<User>("user:999");
                Console.WriteLine($"  缓存未命中: {nonExistUser == null}");

                // 4. 使用 GetOrAdd 模式
                var user2 = cacheManager.GetOrAdd("user:2", () =>
                {
                    Console.WriteLine("    [缓存未命中] 从数据库查询...");
                    return new User
                    {
                        Id = 2,
                        UserName = "李四",
                        Email = "lisi@example.com",
                        IsActive = true,
                        CreateTime = DateTime.Now
                    };
                }, TimeSpan.FromMinutes(10));

                Console.WriteLine($"  GetOrAdd: {user2.UserName}");

                // 5. 再次获取（命中缓存）
                var user2Again = cacheManager.GetOrAdd("user:2", () =>
                {
                    Console.WriteLine("    [缓存未命中] 从数据库查询...");
                    return new User { Id = 2, UserName = "李四_新" };
                }, TimeSpan.FromMinutes(10));

                Console.WriteLine($"  再次获取: {user2Again.UserName} (命中缓存)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Redis 缓存示例
        /// </summary>
        private static void RunRedisCacheExample(CacheManager cacheManager)
        {
            Console.WriteLine("【2】Redis 缓存");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 设置 Redis 缓存（同时写入 Web 缓存）
                var users = new List<User>
                {
                    new User { Id = 10, UserName = "用户10", Email = "u10@example.com", IsActive = true, CreateTime = DateTime.Now },
                    new User { Id = 11, UserName = "用户11", Email = "u11@example.com", IsActive = true, CreateTime = DateTime.Now },
                    new User { Id = 12, UserName = "用户12", Email = "u12@example.com", IsActive = false, CreateTime = DateTime.Now }
                };

                foreach (var user in users)
                {
                    cacheManager.Set($"user:{user.Id}", user, TimeSpan.FromMinutes(30));
                }
                Console.WriteLine($"  设置 {users.Count} 个用户缓存");

                // 2. 获取 Redis 缓存（优先 Web 缓存）
                var user10 = cacheManager.Get<User>("user:10");
                Console.WriteLine($"  获取缓存: {user10?.UserName}");

                // 3. 删除缓存
                cacheManager.Remove("user:12");
                var user12 = cacheManager.Get<User>("user:12");
                Console.WriteLine($"  删除后获取: {user12 == null}");

                // 4. 批量删除（按前缀）
                cacheManager.Set("user:list:active", users.Where(u => u.IsActive).ToList(), TimeSpan.FromMinutes(5));
                cacheManager.Set("user:list:inactive", users.Where(u => !u.IsActive).ToList(), TimeSpan.FromMinutes(5));
                Console.WriteLine("  设置列表缓存");

                cacheManager.RemoveByPrefix("user:list:");
                Console.WriteLine("  批量删除列表缓存");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Cache-Aside 模式示例
        /// </summary>
        private static void RunCacheAsideExample(UserCacheService userService)
        {
            Console.WriteLine("【3】Cache-Aside 模式");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 第一次查询（缓存未命中，从数据库查询）
                Console.WriteLine("  第一次查询用户:");
                var user = userService.GetUserById(1);
                Console.WriteLine($"    结果: {user?.UserName ?? "未找到"}");

                // 2. 第二次查询（命中缓存）
                Console.WriteLine("  第二次查询用户:");
                var userAgain = userService.GetUserById(1);
                Console.WriteLine($"    结果: {userAgain?.UserName ?? "未找到"}");

                // 3. 查询活跃用户列表
                Console.WriteLine("  查询活跃用户列表:");
                var activeUsers = userService.GetActiveUsers();
                Console.WriteLine($"    数量: {activeUsers.Count}");

                // 4. 查询用户数量
                Console.WriteLine("  查询用户数量:");
                var activeCount = userService.GetUserCount(true);
                var inactiveCount = userService.GetUserCount(false);
                Console.WriteLine($"    活跃: {activeCount}, 非活跃: {inactiveCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 数据一致性示例
        /// </summary>
        private static void RunConsistencyExample(UserCacheService userService, OrderCacheService orderService)
        {
            Console.WriteLine("【4】数据一致性");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 更新用户（写入时删除缓存）
                Console.WriteLine("  更新用户:");
                var user = userService.GetUserById(1);
                if (user != null)
                {
                    var oldName = user.UserName;
                    user.UserName = "张三_已更新";
                    var updateResult = userService.UpdateUser(user);
                    Console.WriteLine($"    更新 {(updateResult ? "成功" : "失败")}: {oldName} -> {user.UserName}");

                    // 重新查询（缓存已删除，从数据库获取最新数据）
                    var updatedUser = userService.GetUserById(1);
                    Console.WriteLine($"    重新查询: {updatedUser?.UserName}");
                }

                // 2. 创建订单（删除相关缓存）
                Console.WriteLine("\n  创建订单:");
                var order = new Order
                {
                    UserId = 1,
                    OrderNo = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                    TotalAmount = 99.99m,
                    Status = 0,
                    CreateTime = DateTime.Now
                };

                var createResult = orderService.CreateOrder(order);
                Console.WriteLine($"    创建 {(createResult ? "成功" : "失败")}");

                // 3. 更新订单状态（删除相关缓存）
                Console.WriteLine("\n  更新订单状态:");
                var userOrders = orderService.GetUserOrders(1);
                if (userOrders.Count > 0)
                {
                    var firstOrder = userOrders[0];
                    Console.WriteLine($"    订单 {firstOrder.OrderNo}: 状态 {firstOrder.Status} -> 1");
                    var statusResult = orderService.UpdateOrderStatus(firstOrder.Id, 1);
                    Console.WriteLine($"    更新 {(statusResult ? "成功" : "失败")}");
                }

                // 4. 查询订单统计
                Console.WriteLine("\n  订单统计:");
                var stats = orderService.GetOrderStatistics();
                foreach (var stat in stats)
                {
                    var statusName = stat.Key == 0 ? "待支付" :
                                     stat.Key == 1 ? "已支付" :
                                     stat.Key == 2 ? "已发货" :
                                     stat.Key == 3 ? "已完成" : "已取消";
                    Console.WriteLine($"    {statusName}: {stat.Value} 单");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 缓存统计示例
        /// </summary>
        private static void RunCacheStatisticsExample(CacheManager cacheManager)
        {
            Console.WriteLine("【5】缓存统计");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 模拟一些缓存操作
                for (int i = 0; i < 10; i++)
                {
                    cacheManager.Set($"test:{i}", new User { Id = i, UserName = $"用户{i}" }, TimeSpan.FromMinutes(10));
                }

                // 命中一些缓存
                for (int i = 0; i < 5; i++)
                {
                    cacheManager.Get<User>($"test:{i}");
                }

                // 未命中一些缓存
                for (int i = 10; i < 15; i++)
                {
                    cacheManager.Get<User>($"test:{i}");
                }

                // 获取统计信息
                var stats = cacheManager.GetStatistics();
                Console.WriteLine("  缓存统计:");
                Console.WriteLine($"    Web 缓存命中: {stats.WebCacheHits}");
                Console.WriteLine($"    Web 缓存未命中: {stats.WebCacheMisses}");
                Console.WriteLine($"    Web 缓存命中率: {stats.WebCacheHitRate:F1}%");
                Console.WriteLine($"    Redis 缓存命中: {stats.RedisCacheHits}");
                Console.WriteLine($"    Redis 缓存未命中: {stats.RedisCacheMisses}");
                Console.WriteLine($"    Redis 缓存命中率: {stats.RedisCacheHitRate:F1}%");

                Console.WriteLine("\n  缓存策略说明:");
                Console.WriteLine("    - Web 缓存（MemoryCache）: 进程内，速度快，重启丢失");
                Console.WriteLine("    - Redis 缓存: 分布式，多实例共享，持久化");
                Console.WriteLine("    - Cache-Aside: 先查缓存，未命中查数据库，回填缓存");
                Console.WriteLine("    - 写入时删除: 更新/删除数据时，删除相关缓存");
                Console.WriteLine("    - 过期策略: 设置合理的过期时间，避免数据不一致");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion
    }
}
