using System;
using System.Collections.Generic;
using FastData;
using FastData.Property;
using FastRedis;

namespace FastData.Example.Example
{
    /// <summary>
    /// Redis 缓存高级用法示例
    /// 演示缓存穿透防护、缓存预热、缓存更新策略等
    /// </summary>
    public static class RedisAdvancedExample
    {
        #region Model 定义

        /// <summary>
        /// 商品模型（用于缓存示例）
        /// </summary>
        [Table(Name = "Redis_CacheProduct")]
        [Cache(IsEnable = true, ExpireTime = 300, Key = "product:{Id}")]
        public class CacheProduct
        {
            [Primary]
            public int Id { get; set; }

            [Column(Comments = "商品名称")]
            public string ProductName { get; set; }

            [Column(Comments = "价格")]
            public decimal Price { get; set; }

            [Column(Comments = "库存")]
            public int Stock { get; set; }

            [Column(Comments = "更新时间")]
            public DateTime UpdateTime { get; set; }
        }

        #endregion

        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Redis 缓存高级用法示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 1. 基础缓存 API
            Console.WriteLine("【1】基础缓存 API");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("RedisInfo.Set(key, value, seconds)     // 设置缓存（指定过期时间）");
            Console.WriteLine("RedisInfo.Get<T>(key)                  // 获取缓存");
            Console.WriteLine("RedisInfo.Remove(key)                  // 删除缓存");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  var product = FastRead.Query<CacheProduct>(p => p.Id == 1).ToList()[0];");
            Console.WriteLine("  RedisInfo.Set(\"product:1\", product, 300);  // 缓存 5 分钟");
            Console.WriteLine("  var cached = RedisInfo.Get<CacheProduct>(\"product:1\");");
            Console.WriteLine();

            // 2. 缓存穿透防护
            Console.WriteLine("【2】缓存穿透防护");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("问题：查询不存在的数据会穿透缓存直达数据库");
            Console.WriteLine("解决：缓存空值（短时间）");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  var product = FastRead.Query<CacheProduct>(p => p.Id == notExistId).ToList();");
            Console.WriteLine("  if (product.Count == 0)");
            Console.WriteLine("  {");
            Console.WriteLine("      // 缓存空值 60 秒，防止穿透");
            Console.WriteLine("      RedisInfo.Set(\"product:99999\", new { }, 60);");
            Console.WriteLine("  }");
            Console.WriteLine();

            // 3. 缓存预热
            Console.WriteLine("【3】缓存预热");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("在系统启动时将热点数据加载到缓存");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  var hotProducts = FastRead.Query<CacheProduct>(p => true).ToList();");
            Console.WriteLine("  foreach (var product in hotProducts.Take(100))");
            Console.WriteLine("  {");
            Console.WriteLine("      RedisInfo.Set($\"product:{product.Id}\", product, 3600);");
            Console.WriteLine("  }");
            Console.WriteLine();

            // 4. 缓存更新策略
            Console.WriteLine("【4】缓存更新策略");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Cache-Aside 模式（先更新数据库，再删除缓存）:");
            Console.WriteLine("  product.Price = newPrice;");
            Console.WriteLine("  FastWrite.Update(product);");
            Console.WriteLine("  RedisInfo.Remove($\"product:{product.Id}\");  // 删除缓存");
            Console.WriteLine();
            Console.WriteLine("Write-Through 模式（同时更新数据库和缓存）:");
            Console.WriteLine("  product.Price = newPrice;");
            Console.WriteLine("  FastWrite.Update(product);");
            Console.WriteLine("  RedisInfo.Set($\"product:{product.Id}\", product, 300);  // 同步更新缓存");
            Console.WriteLine();

            // 5. 分布式锁
            Console.WriteLine("【5】分布式锁");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("使用 Redis 实现分布式锁：");
            Console.WriteLine("  string lockKey = \"lock:deduction_stock\";");
            Console.WriteLine("  string lockValue = Guid.NewGuid().ToString();");
            Console.WriteLine();
            Console.WriteLine("  // 尝试获取锁（检查是否已存在）");
            Console.WriteLine("  var existingLock = RedisInfo.Get<string>(lockKey);");
            Console.WriteLine("  if (string.IsNullOrEmpty(existingLock))");
            Console.WriteLine("  {");
            Console.WriteLine("      RedisInfo.Set(lockKey, lockValue, 10);  // 10 秒过期");
            Console.WriteLine("      try { /* 执行临界区代码 */ }");
            Console.WriteLine("      finally { RedisInfo.Remove(lockKey); }  // 释放锁");
            Console.WriteLine("  }");
            Console.WriteLine();

            // 6. 复杂对象缓存
            Console.WriteLine("【6】复杂对象缓存");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("缓存列表:");
            Console.WriteLine("  var list = FastRead.Query<CacheProduct>(p => true).ToList();");
            Console.WriteLine("  RedisInfo.Set(\"product:list\", list, 300);");
            Console.WriteLine("  var cachedList = RedisInfo.Get<List<CacheProduct>>(\"product:list\");");
            Console.WriteLine();
            Console.WriteLine("缓存字典:");
            Console.WriteLine("  var dict = new Dictionary<int, CacheProduct>();");
            Console.WriteLine("  RedisInfo.Set(\"product:dict\", dict, 300);");
            Console.WriteLine("  var cachedDict = RedisInfo.Get<Dictionary<int, CacheProduct>>(\"product:dict\");");
            Console.WriteLine();
        }
    }
}
