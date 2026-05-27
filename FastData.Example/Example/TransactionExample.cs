using System;
using System.Collections.Generic;
using FastData.Example.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// 事务使用示例
    /// 场景：多表操作、批量写入、错误回滚
    /// </summary>
    public static class TransactionExample
    {
        /// <summary>
        /// 运行所有事务示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- 事务使用示例 ---");
            Console.WriteLine();

            DemoBasicTransaction();
            DemoBatchTransaction();
            DemoMultiTableTransaction();
        }

        /// <summary>
        /// 示例 1: 基本事务操作
        /// 场景：单表批量插入，失败时回滚
        /// </summary>
        private static void DemoBasicTransaction()
        {
            Console.WriteLine("=== 示例 1: 基本事务操作 ===");
            Console.WriteLine("场景：批量插入用户，失败时回滚所有操作");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 使用 DataContext 手动管理事务
  using (var db = new DataContext(""DefaultDb""))
  {
      try
      {
          // 开始事务（DataContext 自动管理事务）
          var users = new List<User>
          {
              new User { UserName = ""user1"", Email = ""user1@example.com"", Age = 25 },
              new User { UserName = ""user2"", Email = ""user2@example.com"", Age = 30 },
              new User { UserName = ""user3"", Email = ""user3@example.com"", Age = 28 }
          };

          // 批量插入（使用同一个 DataContext，共享事务）
          foreach (var user in users)
          {
              var result = db.Add<User>(user);
              if (!result.IsSuccess)
              {
                  // 插入失败，DataContext 会自动回滚
                  Console.WriteLine($""插入失败: {result.Message}"");
                  return;
              }
          }

          // 所有操作成功，提交事务
          db.Commit();
          Console.WriteLine(""批量插入成功"");
      }
      catch (Exception ex)
      {
          // 发生异常，DataContext 会自动回滚
          Console.WriteLine($""事务失败: {ex.Message}"");
      }
  }");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: 批量写入事务
        /// 场景：大批量数据写入，分批提交
        /// </summary>
        private static void DemoBatchTransaction()
        {
            Console.WriteLine("=== 示例 2: 批量写入事务 ===");
            Console.WriteLine("场景：大批量数据分批写入，每批独立事务");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 模拟大批量数据
  var allUsers = GenerateUsers(10000);
  var batchSize = 1000;
  var successCount = 0;
  var failCount = 0;

  // 分批处理
  for (int i = 0; i < allUsers.Count; i += batchSize)
  {
      var batch = allUsers.GetRange(i, Math.Min(batchSize, allUsers.Count - i));

      // 每批使用独立事务
      var result = FastWrite.AddList(batch, key: ""DefaultDb"", IsTrans: true);
      if (result.IsSuccess)
      {
          successCount += batch.Count;
          Console.WriteLine($""批次 {i / batchSize + 1}: 成功写入 {batch.Count} 条"");
      }
      else
      {
          failCount += batch.Count;
          Console.WriteLine($""批次 {i / batchSize + 1}: 写入失败 - {result.Message}"");
      }
  }

  Console.WriteLine($""总计: 成功 {successCount} 条, 失败 {failCount} 条"");");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - AddList 支持 IsTrans 参数启用事务");
            Console.WriteLine("  - 大批量数据建议分批处理，避免长事务");
            Console.WriteLine("  - 每批大小根据数据库性能调整（建议 500-2000）");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: 多表事务
        /// 场景：创建订单时同时扣减库存
        /// </summary>
        private static void DemoMultiTableTransaction()
        {
            Console.WriteLine("=== 示例 3: 多表事务 ===");
            Console.WriteLine("场景：创建订单 + 扣减库存，保证数据一致性");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  using (var db = new DataContext(""DefaultDb""))
  {
      try
      {
          // 1. 创建订单
          var order = new Order
          {
              OrderNo = ""ORD-"" + DateTime.Now.ToString(""yyyyMMddHHmmss""),
              UserId = 1,
              Amount = 299.00m,
              Status = 1,
              CreateTime = DateTime.Now
          };
          var orderResult = db.Add(order);
          if (!orderResult.IsSuccess)
          {
              Console.WriteLine($""创建订单失败: {orderResult.Message}"");
              return;
          }

          // 2. 扣减库存（假设有 Product 表）
          var productParam = new Dictionary<string, object>
          {
              { ""ProductId"", 101 },
              { ""Quantity"", 1 }
          };
          var stockSql = @""UPDATE Product SET Stock = Stock - ?Quantity
                           WHERE Id = ?ProductId AND Stock >= ?Quantity"";
          var stockResult = db.ExecuteSql(stockSql, productParam);
          if (stockResult.AffectCount == 0)
          {
              Console.WriteLine(""库存不足，回滚事务"");
              return;
          }

          // 3. 记录日志
          var log = new OrderLog
          {
              OrderId = order.Id,
              Action = ""Create"",
              CreateTime = DateTime.Now
          };
          db.Add(log);

          // 4. 提交事务
          db.Commit();
          Console.WriteLine($""订单创建成功: {order.OrderNo}"");
      }
      catch (Exception ex)
      {
          Console.WriteLine($""事务失败: {ex.Message}"");
          // DataContext 会自动回滚
      }
  }");
            Console.WriteLine();

            Console.WriteLine("事务最佳实践：");
            Console.WriteLine("  - 事务尽量短，避免长时间锁定资源");
            Console.WriteLine("  - 在事务中避免远程调用（API、消息队列等）");
            Console.WriteLine("  - 使用 try-catch 处理异常，确保资源释放");
            Console.WriteLine("  - 大批量操作使用分批事务");
            Console.WriteLine("  - 读写分离场景下，写操作使用主库连接");
            Console.WriteLine();
        }
    }
}
