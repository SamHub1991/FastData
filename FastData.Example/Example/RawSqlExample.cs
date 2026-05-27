using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace FastData.Example.Example
{
    /// <summary>
    /// 原始 SQL 使用示例
    /// 场景：复杂查询、存储过程、批量操作
    /// </summary>
    public static class RawSqlExample
    {
        /// <summary>
        /// 运行所有原始 SQL 示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- 原始 SQL 使用示例 ---");
            Console.WriteLine();

            DemoBasicRawSql();
            DemoParameterizedQuery();
            DemoStoredProcedure();
            DemoBulkInsert();
            DemoSqlWithOutput();
        }

        /// <summary>
        /// 示例 1: 基本原始 SQL
        /// 场景：执行简单的 SQL 查询
        /// </summary>
        private static void DemoBasicRawSql()
        {
            Console.WriteLine("=== 示例 1: 基本原始 SQL ===");
            Console.WriteLine("场景：执行简单的 SQL 查询");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 查询
  var sql = ""SELECT * FROM User WHERE IsActive = 1 ORDER BY CreateTime DESC"";
  var users = FastRead.ExecuteSql<User>(sql, null, null, ""DefaultDb"");

  // 返回 DataTable
  var dataTable = FastRead.ExecuteSql(sql, null, null, ""DefaultDb"");

  // 返回 JSON
  var json = FastRead.ExecuteSqlToJson(sql, null, null, ""DefaultDb"");");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - ExecuteSql<T>: 返回强类型列表");
            Console.WriteLine("  - ExecuteSql: 返回 DataTable");
            Console.WriteLine("  - ExecuteSqlToJson: 返回 JSON 字符串");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: 参数化查询
        /// 场景：使用参数防止 SQL 注入
        /// </summary>
        private static void DemoParameterizedQuery()
        {
            Console.WriteLine("=== 示例 2: 参数化查询 ===");
            Console.WriteLine("场景：使用参数防止 SQL 注入");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 创建参数
  var param = new List<DbParameter>();

  var p1 = DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"").CreateParameter();
  p1.ParameterName = ""UserName"";
  p1.Value = ""admin"";
  param.Add(p1);

  var p2 = DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"").CreateParameter();
  p2.ParameterName = ""MinAge"";
  p2.Value = 18;
  param.Add(p2);

  // 执行参数化查询
  var sql = @""SELECT * FROM User
              WHERE UserName = ?UserName
              AND Age >= ?MinAge
              ORDER BY CreateTime DESC"";

  var users = FastRead.ExecuteSql<User>(sql, param.ToArray(), null, ""DefaultDb"");

  // 使用 DbProviderFactories 简化参数创建
  var factory = DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"");
  var param2 = new List<DbParameter>
  {
      CreateParameter(factory, ""Status"", 1),
      CreateParameter(factory, ""PageSize"", 10),
      CreateParameter(factory, ""Offset"", 0)
  };

  var sql2 = @""SELECT * FROM Order
               WHERE Status = ?Status
               LIMIT ?PageSize OFFSET ?Offset"";

  var orders = FastRead.ExecuteSql<Order>(sql2, param2.ToArray(), null, ""DefaultDb"");");
            Console.WriteLine();

            Console.WriteLine("参数化查询最佳实践：");
            Console.WriteLine("  - 始终使用参数化查询，避免 SQL 注入");
            Console.WriteLine("  - 参数名使用有意义的名称");
            Console.WriteLine("  - 不同数据库的参数前缀可能不同（? 或 @）");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: 存储过程
        /// 场景：调用数据库存储过程
        /// </summary>
        private static void DemoStoredProcedure()
        {
            Console.WriteLine("=== 示例 3: 存储过程 ===");
            Console.WriteLine("场景：调用数据库存储过程");
            Console.WriteLine();

            Console.WriteLine("存储过程定义（MySQL）：");
            Console.WriteLine(@"  CREATE PROCEDURE sp_GetUserStats(IN p_UserId INT)
  BEGIN
      SELECT
          u.UserName,
          COUNT(o.Id) AS OrderCount,
          SUM(o.Amount) AS TotalAmount
      FROM User u
      LEFT JOIN [Order] o ON u.Id = o.UserId
      WHERE u.Id = p_UserId
      GROUP BY u.UserName;
  END");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 调用存储过程
  var factory = DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"");
  var param = new List<DbParameter>
  {
      CreateParameter(factory, ""p_UserId"", 1)
  };

  var sql = ""CALL sp_GetUserStats(?p_UserId)"";
  var result = FastRead.ExecuteSql(sql, param.ToArray(), null, ""DefaultDb"");
  // result 是 DataTable，包含存储过程返回的结果集");
            Console.WriteLine();

            Console.WriteLine("存储过程说明：");
            Console.WriteLine("  - MySQL: CALL procedure_name(params)");
            Console.WriteLine("  - SQL Server: EXEC procedure_name params");
            Console.WriteLine("  - Oracle: BEGIN procedure_name(params); END;");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: 批量插入
        /// 场景：大批量数据快速插入
        /// </summary>
        private static void DemoBulkInsert()
        {
            Console.WriteLine("=== 示例 4: 批量插入 ===");
            Console.WriteLine("场景：大批量数据快速插入");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 方式 1: 使用 AddList 批量插入
  var users = new List<User>();
  for (int i = 0; i < 1000; i++)
  {
      users.Add(new User
      {
          UserName = $""user_{i}"",
          Email = $""user_{i}@example.com"",
          Age = 20 + (i % 50),
          IsActive = true,
          CreateTime = DateTime.Now
      });
  }

  var result = FastWrite.AddList(users, key: ""DefaultDb"", IsTrans: true);
  Console.WriteLine($""插入 {result.AffectCount} 条记录"");

  // 方式 2: 使用原生 SQL 批量插入（更快）
  var sql = @""INSERT INTO User (UserName, Email, Age, IsActive, CreateTime)
              VALUES (?UserName, ?Email, ?Age, ?IsActive, ?CreateTime)"";

  using (var db = new DataContext(""DefaultDb""))
  {
      foreach (var user in users)
      {
          var param = new List<DbParameter>
          {
              CreateParameter(factory, ""UserName"", user.UserName),
              CreateParameter(factory, ""Email"", user.Email),
              CreateParameter(factory, ""Age"", user.Age),
              CreateParameter(factory, ""IsActive"", user.IsActive),
              CreateParameter(factory, ""CreateTime"", user.CreateTime)
          };
          db.ExecuteSql(sql, param.ToArray());
      }
      db.Commit();
  }");
            Console.WriteLine();

            Console.WriteLine("批量插入性能对比：");
            Console.WriteLine("  - AddList: 1000 条约 1-2 秒（自动生成 SQL）");
            Console.WriteLine("  - 原生 SQL: 1000 条约 0.5-1 秒（跳过 ORM 解析）");
            Console.WriteLine("  - 建议：小批量用 AddList，大批量用原生 SQL");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 5: 带输出参数的 SQL
        /// 场景：获取插入后的自增 ID
        /// </summary>
        private static void DemoSqlWithOutput()
        {
            Console.WriteLine("=== 示例 5: 带输出参数的 SQL ===");
            Console.WriteLine("场景：获取插入后的自增 ID");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 插入并获取自增 ID
  var sql = @""INSERT INTO User (UserName, Email, Age, IsActive, CreateTime)
              VALUES (?UserName, ?Email, ?Age, ?IsActive, ?CreateTime);
              SELECT LAST_INSERT_ID();"";

  var factory = DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"");
  var param = new List<DbParameter>
  {
      CreateParameter(factory, ""UserName"", ""newuser""),
      CreateParameter(factory, ""Email"", ""newuser@example.com""),
      CreateParameter(factory, ""Age"", 25),
      CreateParameter(factory, ""IsActive"", true),
      CreateParameter(factory, ""CreateTime"", DateTime.Now)
  };

  var result = FastRead.ExecuteSql(sql, param.ToArray(), null, ""DefaultDb"");
  if (result.Rows.Count > 0)
  {
      var newId = result.Rows[0][0];
      Console.WriteLine($""新用户 ID: {newId}"");
  }");
            Console.WriteLine();

            Console.WriteLine("不同数据库获取自增 ID：");
            Console.WriteLine("  - MySQL: SELECT LAST_INSERT_ID()");
            Console.WriteLine("  - SQL Server: SELECT SCOPE_IDENTITY()");
            Console.WriteLine("  - Oracle: 使用 RETURNING 子句");
            Console.WriteLine();
        }
    }
}
