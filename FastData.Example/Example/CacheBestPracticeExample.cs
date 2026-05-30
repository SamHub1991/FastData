using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Property;
using FastRedis;

namespace FastData.Example.Example
{
    /// <summary>
    /// 缓存最佳实践示例
    /// 
    /// 解答：
    /// 1. 缓存 key 写死的问题和解决方案
    /// 2. 自定义 model 缓存的实现方式
    /// </summary>
    public static class CacheBestPracticeExample
    {
        #region Model 定义

        [Table(Name = "Users")]
        public class User
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 50)]
            public string UserName { get; set; }

            [Column(Length = 100)]
            public string Email { get; set; }

            public int Age { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreateTime { get; set; }
        }

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

        #endregion

        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  缓存最佳实践示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunKeyProblemDemo();
            Console.WriteLine();
            RunDynamicKeySolution();
            Console.WriteLine();
            RunCustomModelCache();
            Console.WriteLine();
            RunCacheHelperDemo();
        }

        private static void RunKeyProblemDemo()
        {
            Console.WriteLine("[1] Key 写死的问题");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Console.WriteLine("错误示例 - Key 写死:");
            Console.WriteLine("  [Cache(Key = \"user\")]  // 所有用户都用同一个 key，数据会互相覆盖");
            Console.WriteLine("  RedisInfo.Set(\"users\", userList, 300);  // 查询条件不同时会返回错误数据");
            Console.WriteLine();

            Console.WriteLine("正确示例 - 动态 Key:");
            Console.WriteLine("  [Cache(Key = \"user:{Id}\")]  // 使用主键作为 key 的一部分");
            Console.WriteLine("  var cacheKey = $\"users:age_gt_{age}_active_{isActive}\";  // 使用查询条件");
        }

        private static void RunDynamicKeySolution()
        {
            Console.WriteLine("[2] 动态 Key 解决方案");
            Console.WriteLine("----------------------------------------");
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

        private static void RunCustomModelCache()
        {
            Console.WriteLine("[3] 自定义 Model 缓存");
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
            Console.WriteLine("  var user = new User { Id = 1, UserName = \"张三\" };");
            Console.WriteLine("  RedisInfo.Set(\"user:1\", user, 300);");
            Console.WriteLine("  var cached = RedisInfo.Get<User>(\"user:1\");");
            Console.WriteLine();
            Console.WriteLine("  // 列表");
            Console.WriteLine("  var users = new List<User> { user1, user2 };");
            Console.WriteLine("  RedisInfo.Set(\"users:list\", users, 300);");
            Console.WriteLine("  var cachedList = RedisInfo.Get<List<User>>(\"users:list\");");
        }

        private static void RunCacheHelperDemo()
        {
            Console.WriteLine("[4] CacheHelper 工具类");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Console.WriteLine("封装的缓存帮助类:");
            Console.WriteLine();
            Console.WriteLine("  // 获取或设置缓存（带自动加载）");
            Console.WriteLine("  public static T GetOrSet<T>(string key, Func<T> factory, int expireSeconds = 300)");
            Console.WriteLine("  {");
            Console.WriteLine("      var cached = RedisInfo.Get<T>(key);");
            Console.WriteLine("      if (cached != null) return cached;");
            Console.WriteLine("      var value = factory();");
            Console.WriteLine("      if (value != null) RedisInfo.Set(key, value, expireSeconds);");
            Console.WriteLine("      return value;");
            Console.WriteLine("  }");
            Console.WriteLine();

            Console.WriteLine("缓存键生成器:");
            Console.WriteLine();
            Console.WriteLine("  // 单条数据 key: 表名:主键值");
            Console.WriteLine("  CacheKey.ForEntity<User>(1)  =>  \"user:1\"");
            Console.WriteLine();
            Console.WriteLine("  // 列表数据 key: 表名:list:条件");
            Console.WriteLine("  CacheKey.ForList<User>(\"active\", \"age_gt_18\")  =>  \"user:list:active:age_gt_18\"");
            Console.WriteLine();
            Console.WriteLine("  // 计数 key: 表名:count:条件");
            Console.WriteLine("  CacheKey.ForCount<User>(\"active\")  =>  \"user:count:active\"");
            Console.WriteLine();

            Console.WriteLine("使用示例:");
            Console.WriteLine("  var client = new FastDataClient(\"db1\");");
            Console.WriteLine();
            Console.WriteLine("  // 1. 单条数据缓存");
            Console.WriteLine("  var user = CacheHelper.GetOrSet(");
            Console.WriteLine("      CacheKey.ForEntity<User>(1),");
            Console.WriteLine("      () => client.Query<User>(u => u.Id == 1).ToItem(),");
            Console.WriteLine("      300");
            Console.WriteLine("  );");
            Console.WriteLine();
            Console.WriteLine("  // 2. 列表数据缓存");
            Console.WriteLine("  var activeUsers = CacheHelper.GetOrSet(");
            Console.WriteLine("      CacheKey.ForList<User>(\"active\", \"age_gt_18\"),");
            Console.WriteLine("      () => client.Query<User>(u => u.IsActive && u.Age > 18).ToList(),");
            Console.WriteLine("      300");
            Console.WriteLine("  );");
            Console.WriteLine();
            Console.WriteLine("  // 3. 更新后清除相关缓存");
            Console.WriteLine("  client.Update(user);");
            Console.WriteLine("  CacheHelper.Remove(");
            Console.WriteLine("      CacheKey.ForEntity<User>(user.Id),");
            Console.WriteLine("      CacheKey.ForList<User>(\"active\", \"age_gt_18\")");
            Console.WriteLine("  );");
        }
    }
}
