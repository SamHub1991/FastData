using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Property;
using FastRedis;

namespace FastData.Example.Example
{
    /// <summary>
    /// Redis 缓存使用示例
    /// 
    /// 演示两种缓存模式：
    /// 1. 主动缓存：手动管理缓存的读写（灵活控制）
    /// 2. 自动缓存：通过 CacheAttribute 自动管理（声明式）
    /// 
    /// 缓存最佳实践：
    /// 3. Key 设计模式（错误 vs 正确示例）
    /// 4. CacheHelper 工具类（GetOrSet / Remove）
    /// 5. CacheKey 生成器（ForEntity / ForList / ForCount）
    /// </summary>
    public static class RedisCacheExample
    {
        #region Model 定义

        /// <summary>
        /// 用户模型（主动缓存示例）
        /// 不使用 CacheAttribute，完全手动控制缓存
        /// </summary>
        [Table(Name = "Users")]
        public class User
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 50)]
            public string Name { get; set; }

            public int Age { get; set; }

            [Column(Length = 100)]
            public string Email { get; set; }

            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 商品模型（自动缓存示例）
        /// 使用 CacheAttribute 声明缓存策略
        /// </summary>
        [Table(Name = "Products")]
        [Cache(IsEnable = true, ExpireTime = 300, Key = "product:{Id}", CacheType = "Redis")]
        public class Product
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 100)]
            public string ProductName { get; set; }

            public decimal Price { get; set; }

            public int Stock { get; set; }

            public DateTime UpdateTime { get; set; }
        }

        /// <summary>
        /// 订单模型（自动缓存示例）
        /// </summary>
        [Table(Name = "Orders")]
        [Cache(IsEnable = true, ExpireTime = 600, Key = "order:{Id}", CacheType = "Redis")]
        public class Order
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            public int UserId { get; set; }

            [Column(Length = 50)]
            public string OrderNo { get; set; }

            public decimal TotalAmount { get; set; }

            [Column(Length = 20)]
            public string Status { get; set; }

            public DateTime CreateTime { get; set; }
        }

        #endregion

        /// <summary>
        /// 运行示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Redis 缓存使用示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 初始化 Redis（程序启动时调用一次）
            // RedisInfo.Init("db.config");

            RunActiveCache();
            Console.WriteLine();
            RunAutoCache();
            Console.WriteLine();
            RunCachePatterns();
            Console.WriteLine();
            RunKeyDesignPatterns();
            Console.WriteLine();
            RunCustomModelCache();
            Console.WriteLine();
            RunCacheHelperDemo();
        }

        #region 主动缓存示例

        /// <summary>
        /// 主动缓存示例
        /// 
        /// 特点：
        /// 1. 完全控制缓存的读写时机
        /// 2. 可自定义缓存 key 和过期时间
        /// 3. 适合复杂业务逻辑和特殊缓存策略
        /// </summary>
        private static void RunActiveCache()
        {
            Console.WriteLine("【1】主动缓存（手动管理）");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 1.1 基本读写
            Console.WriteLine("1.1 基本读写:");
            Console.WriteLine(@"
    // 查询并缓存
    var users = FastRead.Query<User>(u => u.Age > 18).ToList();
    RedisInfo.Set(""users:age_gt_18"", users, 300);  // 缓存 5 分钟

    // 从缓存读取
    var cachedUsers = RedisInfo.Get<List<User>>(""users:age_gt_18"");
    if (cachedUsers != null)
    {
        // 缓存命中，直接使用
        Console.WriteLine($""从缓存获取 {cachedUsers.Count} 条数据"");
    }
    else
    {
        // 缓存未命中，查询数据库
        users = FastRead.Query<User>(u => u.Age > 18).ToList();
        RedisInfo.Set(""users:age_gt_18"", users, 300);
    }
");

            // 1.2 缓存穿透防护
            Console.WriteLine("1.2 缓存穿透防护:");
            Console.WriteLine(@"
    // 查询单条数据（可能不存在）
    var user = FastRead.Query<User>(u => u.Id == 99999).ToItem();
    if (user != null)
    {
        RedisInfo.Set($""user:99999"", user, 300);
    }
    else
    {
        // 缓存空值，防止穿透（较短过期时间）
        RedisInfo.Set($""user:99999"", """", 60);
    }
");

            // 1.3 更新时清除缓存
            Console.WriteLine("1.3 更新时清除缓存（Cache-Aside 模式）:");
            Console.WriteLine(@"
    // 更新用户
    var user = new User { Id = 1, Name = ""新名字"", Age = 26 };
    FastWrite.Update(user);

    // 清除相关缓存
    RedisInfo.Remove($""user:{user.Id}"");
    RedisInfo.Remove(""users:age_gt_18"");  // 清除列表缓存
");

            // 1.4 批量操作
            Console.WriteLine("1.4 批量缓存操作:");
            Console.WriteLine(@"
    // 批量设置
    var dict = new Dictionary<string, User>();
    foreach (var user in users)
    {
        dict[$""user:{user.Id}""] = user;
    }
    RedisInfo.SetDic(dict, db: 0);

    // 批量获取
    var keys = users.Select(u => $""user:{u.Id}"").ToArray();
    var cachedDict = RedisInfo.GetDic<User>(keys, db: 0);
");
        }

        #endregion

        #region 自动缓存示例

        /// <summary>
        /// 自动缓存示例
        /// 
        /// 特点：
        /// 1. 通过 CacheAttribute 声明缓存策略
        /// 2. 框架自动管理缓存读写
        /// 3. 代码简洁，适合标准 CRUD 场景
        /// </summary>
        private static void RunAutoCache()
        {
            Console.WriteLine("【2】自动缓存（CacheAttribute）");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 2.1 定义带缓存的模型
            Console.WriteLine("2.1 定义带缓存的模型:");
            Console.WriteLine(@"
    [Table(Name = ""Products"")]
    [Cache(IsEnable = true, ExpireTime = 300, Key = ""product:{Id}"", CacheType = ""Redis"")]
    public class Product
    {
        [Primary]
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        [Column(Length = 100)]
        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public DateTime UpdateTime { get; set; }
    }
");

            // 2.2 使用自动缓存
            Console.WriteLine("2.2 使用自动缓存:");
            Console.WriteLine(@"
    // 查询时自动读取缓存
    // 如果缓存存在且未过期，直接返回缓存数据
    // 如果缓存不存在或已过期，查询数据库并写入缓存
    var products = FastRead.Query<Product>(p => p.Price > 100).ToList();

    // 更新时自动清除/更新缓存
    var product = new Product { Id = 1, Price = 200 };
    FastWrite.Update(product);
");

            // 2.3 CacheAttribute 参数说明
            Console.WriteLine("2.3 CacheAttribute 参数说明:");
            Console.WriteLine(@"
    [Cache(
        IsEnable = true,      // 是否启用缓存
        ExpireTime = 300,     // 过期时间（秒）
        Key = ""product:{Id}"", // 缓存键模板（支持 {PropertyName} 占位符）
        CacheType = ""Redis""   // 缓存类型：Redis 或 Local
    )]
");
        }

        #endregion

        #region 缓存模式示例

        /// <summary>
        /// 常用缓存模式示例
        /// </summary>
        private static void RunCachePatterns()
        {
            Console.WriteLine("【3】常用缓存模式");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 3.1 Cache-Aside（旁路缓存）
            Console.WriteLine("3.1 Cache-Aside（旁路缓存）- 最常用:");
            Console.WriteLine(@"
    // 读取：先查缓存，未命中再查数据库
    public User GetUserById(int id)
    {
        var cacheKey = $""user:{id}"";
        var user = RedisInfo.Get<User>(cacheKey);
        
        if (user == null)
        {
            user = FastRead.Query<User>(u => u.Id == id).ToItem();
            if (user != null)
            {
                RedisInfo.Set(cacheKey, user, 300);
            }
        }
        return user;
    }

    // 更新：先更新数据库，再删除缓存
    public void UpdateUser(User user)
    {
        FastWrite.Update(user);
        RedisInfo.Remove($""user:{user.Id}"");
    }
");

            // 3.2 Write-Through（写穿透）
            Console.WriteLine("3.2 Write-Through（写穿透）:");
            Console.WriteLine(@"
    // 更新时同时更新数据库和缓存
    public void UpdateUser(User user)
    {
        FastWrite.Update(user);
        RedisInfo.Set($""user:{user.Id}"", user, 300);  // 同步更新缓存
    }
");

            // 3.3 缓存预热
            Console.WriteLine("3.3 缓存预热（系统启动时加载热点数据）:");
            Console.WriteLine(@"
    public void WarmUpCache()
    {
        // 加载热门商品到缓存
        var hotProducts = FastRead.Query<Product>(p => p.Stock > 0)
            .OrderBy<Product>(p => p.UpdateTime)
            .Take(100)
            .ToList();

        foreach (var product in hotProducts)
        {
            RedisInfo.Set($""product:{product.Id}"", product, 3600);
        }
        
        Console.WriteLine($""预热完成，加载 {hotProducts.Count} 条数据"");
    }
");

            // 3.4 缓存降级
            Console.WriteLine("3.4 缓存降级（Redis 不可用时降级到数据库）:");
            Console.WriteLine(@"
    public User GetUserByIdSafe(int id)
    {
        try
        {
            var cacheKey = $""user:{id}"";
            var user = RedisInfo.Get<User>(cacheKey);
            
            if (user == null)
            {
                user = FastRead.Query<User>(u => u.Id == id).ToItem();
                if (user != null)
                {
                    RedisInfo.Set(cacheKey, user, 300);
                }
            }
            return user;
        }
        catch (Exception ex)
        {
            // Redis 异常，降级到数据库查询
            Console.WriteLine($""Redis 异常，降级到数据库: {ex.Message}"");
            return FastRead.Query<User>(u => u.Id == id).ToItem();
        }
    }
");

            // 3.5 使用 FastDataClient
            Console.WriteLine("3.5 使用 FastDataClient:");
            Console.WriteLine(@"
    var client = new FastDataClient(""db1"");

    // 查询
    var users = client.Query<User>(u => u.Age > 18).ToList();

    // 手动缓存
    RedisInfo.Set(""users:age_gt_18"", users, 300);

    // 更新并清除缓存
    var user = new User { Id = 1, Name = ""新名字"" };
    client.Update(user);
    RedisInfo.Remove($""user:{user.Id}"");
");
        }

        #endregion

        #region Key 设计模式

        private static void RunKeyDesignPatterns()
        {
            Console.WriteLine("【4】Key 设计模式（错误 vs 正确）");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Console.WriteLine("错误示例 - Key 写死:");
            Console.WriteLine("  [Cache(Key = \"user\")]  // 所有用户都用同一个 key，数据会互相覆盖");
            Console.WriteLine("  RedisInfo.Set(\"users\", userList, 300);  // 查询条件不同时会返回错误数据");
            Console.WriteLine();

            Console.WriteLine("正确示例 - 动态 Key:");
            Console.WriteLine("  [Cache(Key = \"user:{Id}\")]  // 使用主键作为 key 的一部分");
            Console.WriteLine("  var cacheKey = $\"users:age_gt_{age}_active_{isActive}\";  // 使用查询条件");
            Console.WriteLine();

            Console.WriteLine("方案1: 单条数据缓存（按主键）");
            Console.WriteLine("  Key 格式: 表名:主键值");
            Console.WriteLine("  示例: user:1, product:100");
            Console.WriteLine();

            Console.WriteLine("方案2: 列表数据缓存（按查询条件）");
            Console.WriteLine("  Key 格式: 表名:条件1_值1:条件2_值2");
            Console.WriteLine("  示例: users:age_gt_18:active_true");
            Console.WriteLine();

            Console.WriteLine("方案3: 使用 CacheHelper 工具类（推荐）");
            Console.WriteLine("  自动生成规范的缓存 key");
        }

        #endregion

        #region 自定义 Model 缓存

        private static void RunCustomModelCache()
        {
            Console.WriteLine("【5】自定义 Model 缓存");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Console.WriteLine("支持任意类型的缓存:");
            Console.WriteLine("  1. 基本类型: int, string, bool 等");
            Console.WriteLine("  2. 自定义 Model: User, Product 等");
            Console.WriteLine("  3. 列表: List<User>, List<Product>");
            Console.WriteLine("  4. 字典: Dictionary<int, User>");
            Console.WriteLine("  5. 复杂嵌套对象: OrderDetail");
            Console.WriteLine();

            Console.WriteLine("示例代码:");
            Console.WriteLine("  // 自定义 Model");
            Console.WriteLine("  var user = new User { Id = 1, Name = \"张三\" };");
            Console.WriteLine("  RedisInfo.Set(\"user:1\", user, 300);");
            Console.WriteLine("  var cached = RedisInfo.Get<User>(\"user:1\");");
            Console.WriteLine();
            Console.WriteLine("  // 列表");
            Console.WriteLine("  var users = new List<User> { user1, user2 };");
            Console.WriteLine("  RedisInfo.Set(\"users:list\", users, 300);");
            Console.WriteLine("  var cachedList = RedisInfo.Get<List<User>>(\"users:list\");");
        }

        #endregion

        #region CacheHelper 与 CacheKey 工具类

        private static void RunCacheHelperDemo()
        {
            Console.WriteLine("【6】CacheHelper 与 CacheKey 工具类");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Console.WriteLine("CacheHelper 用法:");
            Console.WriteLine();
            Console.WriteLine("  // 获取或设置缓存（带自动加载）");
            Console.WriteLine("  var user = CacheHelper.GetOrSet(");
            Console.WriteLine("      CacheKey.ForEntity<User>(1),");
            Console.WriteLine("      () => client.Query<User>(u => u.Id == 1).ToItem(),");
            Console.WriteLine("      300");
            Console.WriteLine("  );");
            Console.WriteLine();
            Console.WriteLine("  // 列表数据缓存");
            Console.WriteLine("  var activeUsers = CacheHelper.GetOrSet(");
            Console.WriteLine("      CacheKey.ForList<User>(\"active\", \"age_gt_18\"),");
            Console.WriteLine("      () => client.Query<User>(u => u.IsActive && u.Age > 18).ToList(),");
            Console.WriteLine("      300");
            Console.WriteLine("  );");
            Console.WriteLine();
            Console.WriteLine("  // 更新后清除相关缓存");
            Console.WriteLine("  client.Update(user);");
            Console.WriteLine("  CacheHelper.Remove(");
            Console.WriteLine("      CacheKey.ForEntity<User>(user.Id),");
            Console.WriteLine("      CacheKey.ForList<User>(\"active\", \"age_gt_18\")");
            Console.WriteLine("  );");
            Console.WriteLine();

            Console.WriteLine("CacheKey 生成规则:");
            Console.WriteLine("  CacheKey.ForEntity<User>(1)              =>  \"user:1\"");
            Console.WriteLine("  CacheKey.ForList<User>(\"active\")         =>  \"user:list:active\"");
            Console.WriteLine("  CacheKey.ForList<User>(\"a\", \"b\")         =>  \"user:list:a:b\"");
            Console.WriteLine("  CacheKey.ForCount<User>(\"active\")        =>  \"user:count:active\"");
        }

        /// <summary>
        /// 缓存帮助类 - 封装常用的缓存读写模式
        /// </summary>
        public static class CacheHelper
        {
            /// <summary>
            /// 获取缓存，未命中时通过 factory 加载并写入缓存
            /// </summary>
            public static T GetOrSet<T>(string key, Func<T> factory, int expireSeconds = 300) where T : class, new()
            {
                var cached = RedisInfo.Get<T>(key);
                if (cached != null) return cached;

                var value = factory();
                if (value != null) RedisInfo.Set(key, value, expireSeconds);
                return value;
            }

            /// <summary>
            /// 批量删除缓存
            /// </summary>
            public static void Remove(params string[] keys)
            {
                foreach (var key in keys)
                {
                    RedisInfo.Remove(key);
                }
            }
        }

        /// <summary>
        /// 缓存键生成器 - 统一管理缓存 key 的命名规范
        /// </summary>
        public static class CacheKey
        {
            /// <summary>
            /// 单条数据 key: 表名:主键值
            /// </summary>
            public static string ForEntity<T>(object id)
            {
                return $"{typeof(T).Name.ToLower()}:{id}";
            }

            /// <summary>
            /// 列表数据 key: 表名:list:条件
            /// </summary>
            public static string ForList<T>(params string[] conditions)
            {
                var table = typeof(T).Name.ToLower();
                var cond = string.Join(":", conditions);
                return $"{table}:list:{cond}";
            }

            /// <summary>
            /// 计数 key: 表名:count:条件
            /// </summary>
            public static string ForCount<T>(params string[] conditions)
            {
                var table = typeof(T).Name.ToLower();
                var cond = string.Join(":", conditions);
                return $"{table}:count:{cond}";
            }
        }

        #endregion
    }
}
