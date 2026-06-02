using FastData;
using FastData.Demo.Models;
using FastData.Demo.Services;
using FastRedis;
using FastUntility.Base;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 缓存功能演示控制器
    /// 
    /// 演示场景：
    /// 1. 基本缓存操作（CRUD）
    /// 2. 缓存穿透防护
    /// 3. 缓存降级策略
    /// 4. 缓存预热
    /// 5. 批量缓存操作
    /// 6. 计数器操作
    /// 7. 缓存键设计规范
    /// </summary>
    [ApiController]
    [Route("api/Cache")]
    public class CacheController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IUserCacheService _userCacheService;

        public CacheController(ICacheService cacheService, IUserCacheService userCacheService)
        {
            _cacheService = cacheService;
            _userCacheService = userCacheService;
        }

        #region 基本缓存操作

        /// <summary>
        /// 基本缓存操作演示
        /// 
        /// 场景：简单的缓存读写操作
        /// 策略：Cache-Aside（旁路缓存）
        /// </summary>
        [HttpGet("basic")]
        public async Task<IActionResult> BasicCacheOperations()
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 1. 设置缓存
                var user = new AppUser
                {
                    Id = 1,
                    UserName = "test_user",
                    Email = "test@example.com",
                    Age = 25,
                    IsActive = true
                };

                // 设置缓存，2小时过期
                await _cacheService.SetAsync("user:1", user, hours: 2);
                result["set"] = "成功";

                // 2. 获取缓存
                var cachedUser = await _cacheService.GetAsync<AppUser>("user:1");
                result["get"] = cachedUser != null ? $"命中: {cachedUser.UserName}" : "未命中";

                // 3. 检查是否存在
                var exists = await _cacheService.ExistsAsync("user:1");
                result["exists"] = exists;

                // 4. 删除缓存
                await _cacheService.RemoveAsync("user:1");
                result["remove"] = "成功";

                // 5. 验证删除
                var afterRemove = await _cacheService.GetAsync<AppUser>("user:1");
                result["after_remove"] = afterRemove == null ? "已删除" : "仍存在";
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 缓存穿透防护

        /// <summary>
        /// 缓存穿透防护演示
        /// 
        /// 场景：查询不存在的数据，防止缓存穿透
        /// 策略：缓存空值，设置较短过期时间
        /// 
        /// 问题：如果查询的数据在数据库中不存在，每次请求都会穿透缓存直接访问数据库
        /// 解决：将空值也缓存起来，设置较短的过期时间（如60秒）
        /// </summary>
        [HttpGet("penetration-protection")]
        public async Task<IActionResult> CachePenetrationProtection()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var userId = 99999; // 不存在的用户ID
                var cacheKey = $"user:{userId}";

                // 1. 查询缓存
                var cachedUser = await _cacheService.GetAsync<AppUser>(cacheKey);
                if (cachedUser != null)
                {
                    result["status"] = "缓存命中";
                    result["user"] = cachedUser;
                    return Ok(result);
                }

                // 2. 缓存未命中，查询数据库
                // var user = await _userRepository.GetByIdAsync(userId);
                AppUser user = null; // 模拟数据库查询结果为空

                if (user != null)
                {
                    // 3. 数据存在，缓存数据
                    await _cacheService.SetAsync(cacheKey, user, hours: 2);
                    result["status"] = "数据库命中，已缓存";
                    result["user"] = user;
                }
                else
                {
                    // 4. 数据不存在，缓存空值（防穿透）
                    // 设置较短的过期时间，避免占用过多内存
                    await _cacheService.SetAsync(cacheKey, new AppUser(), hours: 1);
                    result["status"] = "数据库未命中，已缓存空值";
                    result["note"] = "空值缓存1小时，防止缓存穿透";
                }
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 缓存降级策略

        /// <summary>
        /// 缓存降级策略演示
        /// 
        /// 场景：Redis 不可用时的降级处理
        /// 策略：Redis 异常时降级到内存缓存或直接查询数据库
        /// 
        /// 降级层次：
        /// 1. Redis 缓存（首选）
        /// 2. 内存缓存（备选）
        /// 3. 数据库查询（兜底）
        /// </summary>
        [HttpGet("fallback")]
        public async Task<IActionResult> CacheFallback()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var userId = 1;
                var cacheKey = $"user:{userId}";

                // 第一层：尝试 Redis 缓存
                try
                {
                    var cachedUser = await _cacheService.GetAsync<AppUser>(cacheKey);
                    if (cachedUser != null)
                    {
                        result["source"] = "Redis缓存";
                        result["user"] = cachedUser;
                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    result["redis_error"] = ex.Message;
                }

                // 第二层：尝试内存缓存
                try
                {
                    var memoryCached = FastUntility.Cache.BaseCache.Get<AppUser>(cacheKey);
                    if (memoryCached != null && memoryCached.Id > 0)
                    {
                        result["source"] = "内存缓存";
                        result["user"] = memoryCached;
                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    result["memory_error"] = ex.Message;
                }

                // 第三层：查询数据库（兜底）
                // var user = await _userRepository.GetByIdAsync(userId);
                var user = new AppUser
                {
                    Id = userId,
                    UserName = "fallback_user",
                    Email = "fallback@example.com",
                    Age = 30
                };

                if (user != null)
                {
                    // 尝试回写缓存
                    try
                    {
                        await _cacheService.SetAsync(cacheKey, user, hours: 2);
                    }
                    catch
                    {
                        // Redis 不可用，写入内存缓存
                        FastUntility.Cache.BaseCache.Set(cacheKey, user, 2);
                    }

                    result["source"] = "数据库";
                    result["user"] = user;
                }
                else
                {
                    result["source"] = "数据库";
                    result["status"] = "用户不存在";
                }
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 缓存预热

        /// <summary>
        /// 缓存预热演示
        /// 
        /// 场景：系统启动时加载热点数据到缓存
        /// 策略：批量加载常用数据，减少冷启动时的数据库压力
        /// 
        /// 适用场景：
        /// 1. 系统启动时
        /// 2. 缓存重启后
        /// 3. 定时任务预热
        /// </summary>
        [HttpPost("warmup")]
        public async Task<IActionResult> CacheWarmup()
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 1. 预热活跃用户
                var activeUsers = new List<AppUser>
                {
                    new AppUser { Id = 1, UserName = "user1", IsActive = true },
                    new AppUser { Id = 2, UserName = "user2", IsActive = true },
                    new AppUser { Id = 3, UserName = "user3", IsActive = true }
                };

                foreach (var user in activeUsers)
                {
                    await _cacheService.SetAsync($"user:{user.Id}", user, hours: 24);
                }
                result["active_users"] = $"预热 {activeUsers.Count} 个活跃用户";

                // 2. 预热用户列表
                await _cacheService.SetAsync("users:active", activeUsers, hours: 1);
                result["active_users_list"] = "预热活跃用户列表";

                // 3. 预热计数器
                foreach (var user in activeUsers)
                {
                    await _cacheService.IncrementAsync($"user:{user.Id}:views", 0);
                }
                result["counters"] = "预热用户浏览计数器";

                result["status"] = "缓存预热完成";
                result["note"] = "热点数据已加载到缓存，可减少冷启动时的数据库压力";
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 批量缓存操作

        /// <summary>
        /// 批量缓存操作演示
        /// 
        /// 场景：批量读写缓存数据
        /// 策略：使用批量操作减少网络往返
        /// </summary>
        [HttpGet("batch")]
        public async Task<IActionResult> BatchCacheOperations()
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 1. 批量设置
                var users = new List<AppUser>
                {
                    new AppUser { Id = 101, UserName = "batch_user1", Age = 20 },
                    new AppUser { Id = 102, UserName = "batch_user2", Age = 25 },
                    new AppUser { Id = 103, UserName = "batch_user3", Age = 30 }
                };

                var setTasks = new List<Task<bool>>();
                foreach (var user in users)
                {
                    setTasks.Add(_cacheService.SetAsync($"user:{user.Id}", user, hours: 2));
                }
                await Task.WhenAll(setTasks);
                result["batch_set"] = $"批量设置 {users.Count} 个用户";

                // 2. 批量获取
                var getTasks = new List<Task<AppUser>>();
                foreach (var user in users)
                {
                    getTasks.Add(_cacheService.GetAsync<AppUser>($"user:{user.Id}"));
                }
                var cachedUsers = await Task.WhenAll(getTasks);
                result["batch_get"] = $"批量获取 {cachedUsers.Length} 个用户";

                // 3. 批量删除
                var removeTasks = new List<Task<bool>>();
                foreach (var user in users)
                {
                    removeTasks.Add(_cacheService.RemoveAsync($"user:{user.Id}"));
                }
                await Task.WhenAll(removeTasks);
                result["batch_remove"] = $"批量删除 {users.Count} 个用户";

                result["status"] = "批量操作完成";
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 计数器操作

        /// <summary>
        /// 计数器操作演示
        /// 
        /// 场景：浏览次数、点赞数、库存等计数操作
        /// 策略：使用 Redis 原子递增操作
        /// </summary>
        [HttpGet("counter")]
        public async Task<IActionResult> CounterOperations()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var userId = 1;

                // 1. 递增浏览次数
                var views = await _cacheService.IncrementAsync($"user:{userId}:views", 1);
                result["views_after_increment"] = views;

                // 2. 再次递增
                views = await _cacheService.IncrementAsync($"user:{userId}:views", 1);
                result["views_after_second_increment"] = views;

                // 3. 批量递增
                var likes = await _cacheService.IncrementAsync($"user:{userId}:likes", 10);
                result["likes_after_batch_increment"] = likes;

                // 4. 获取当前值（使用 RedisInfo 直接获取）
                var currentViews = RedisInfo.Get($"user:{userId}:views");
                result["current_views"] = currentViews.ToLong(views);

                result["status"] = "计数器操作完成";
                result["note"] = "Redis 原子递增操作，支持高并发场景";
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 缓存键设计规范

        /// <summary>
        /// 缓存键设计规范演示
        /// 
        /// 场景：展示正确的缓存键设计
        /// 策略：使用动态键，避免键冲突
        /// 
        /// 规范：
        /// 1. 单条数据：表名:主键值
        /// 2. 列表数据：表名:条件1_值1:条件2_值2
        /// 3. 计数数据：表名:count:条件
        /// </summary>
        [HttpGet("key-design")]
        public IActionResult CacheKeyDesign()
        {
            var result = new Dictionary<string, object>();

            // 1. 单条数据缓存键
            var userId = 123;
            var userKey = $"user:{userId}";
            result["entity_key"] = new
            {
                format = "表名:主键值",
                example = userKey,
                usage = "缓存单条用户数据"
            };

            // 2. 列表数据缓存键
            var age = 18;
            var isActive = true;
            var listKey = $"users:age_gt_{age}:active_{isActive}";
            result["list_key"] = new
            {
                format = "表名:条件1_值1:条件2_值2",
                example = listKey,
                usage = "缓存用户列表（按条件筛选）"
            };

            // 3. 计数数据缓存键
            var countKey = $"users:count:active_{isActive}";
            result["count_key"] = new
            {
                format = "表名:count:条件",
                example = countKey,
                usage = "缓存用户计数"
            };

            // 4. 错误示例
            result["wrong_examples"] = new[]
            {
                new { wrong = "[Cache(Key = \"user\")]", reason = "所有用户都用同一个key，数据会互相覆盖" },
                new { wrong = "RedisInfo.Set(\"users\", userList, 300)", reason = "查询条件不同时会返回错误数据" }
            };

            // 5. 正确示例
            result["correct_examples"] = new[]
            {
                new { correct = "[Cache(Key = \"user:{Id}\")]", reason = "使用主键作为key的一部分" },
                new { correct = "var cacheKey = $\"users:age_gt_{age}\"", reason = "使用查询条件作为key" }
            };

            return Ok(result);
        }

        #endregion

        #region 用户缓存服务

        /// <summary>
        /// 用户缓存服务演示
        /// 
        /// 场景：使用封装的用户缓存服务
        /// 策略：业务层缓存封装，提供统一的缓存接口
        /// </summary>
        [HttpGet("user-service/{userId}")]
        public async Task<IActionResult> UserCacheService(int userId)
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 1. 获取用户（带缓存）
                var user = await _userCacheService.GetUserAsync(userId, async () =>
                {
                    // 模拟数据库查询
                    return new AppUser
                    {
                        Id = userId,
                        UserName = $"user_{userId}",
                        Email = $"user{userId}@example.com",
                        Age = 25,
                        IsActive = true
                    };
                });

                result["user"] = user;
                result["source"] = "缓存或数据库";

                // 2. 增加浏览次数
                await _userCacheService.IncrementViewCountAsync(userId);
                result["view_count_incremented"] = true;

                // 3. 获取活跃用户列表
                var activeUsers = await _userCacheService.GetActiveUsersAsync(async () =>
                {
                    // 模拟数据库查询
                    return new List<AppUser>
                    {
                        new AppUser { Id = 1, UserName = "active_user1", IsActive = true },
                        new AppUser { Id = 2, UserName = "active_user2", IsActive = true }
                    };
                });

                result["active_users"] = activeUsers;
                result["active_users_count"] = activeUsers.Count;
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion

        #region 缓存过期策略

        /// <summary>
        /// 缓存过期策略演示
        /// 
        /// 场景：不同数据的过期时间设置
        /// 策略：根据数据更新频率设置不同的过期时间
        /// 
        /// 建议：
        /// 1. 热点数据：较长过期时间（24小时）
        /// 2. 普通数据：中等过期时间（2小时）
        /// 3. 临时数据：较短过期时间（5分钟）
        /// 4. 空值数据：最短过期时间（1分钟）
        /// </summary>
        [HttpGet("expiration")]
        public async Task<IActionResult> CacheExpiration()
        {
            var result = new Dictionary<string, object>();

            try
            {
                // 1. 热点数据（24小时过期）
                var hotData = new AppUser { Id = 1, UserName = "hot_user" };
                await _cacheService.SetAsync("user:1:hot", hotData, hours: 24);
                result["hot_data"] = "过期时间：24小时";

                // 2. 普通数据（2小时过期）
                var normalData = new AppUser { Id = 2, UserName = "normal_user" };
                await _cacheService.SetAsync("user:2:normal", normalData, hours: 2);
                result["normal_data"] = "过期时间：2小时";

                // 3. 临时数据（5分钟过期）
                var tempData = new AppUser { Id = 3, UserName = "temp_user" };
                await _cacheService.SetAsync("user:3:temp", tempData, hours: 1); // 最小单位是小时
                result["temp_data"] = "过期时间：1小时（临时数据）";

                // 4. 空值数据（1分钟过期）
                var emptyData = new AppUser();
                await _cacheService.SetAsync("user:99999:empty", emptyData, hours: 1);
                result["empty_data"] = "过期时间：1小时（空值防穿透）";

                result["status"] = "缓存过期策略演示完成";
                result["note"] = "根据数据更新频率设置不同的过期时间";
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        #endregion
    }
}
