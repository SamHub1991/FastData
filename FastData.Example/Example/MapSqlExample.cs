using System;
using System.Collections.Generic;
using System.Data.Common;
using FastData.Example.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// XML Map SQL 使用示例
    /// 场景：复杂 SQL 查询、动态条件、存储过程调用
    /// </summary>
    public static class MapSqlExample
    {
        /// <summary>
        /// 运行所有 Map SQL 示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- XML Map SQL 示例 ---");
            Console.WriteLine();

            DemoBasicMapSql();
            DemoDynamicConditions();
            DemoForEach();
            DemoMapSqlWithParameters();
        }

        /// <summary>
        /// 示例 1: 基本 Map SQL 查询
        /// 场景：使用 XML 定义复杂 SQL，通过 FastMap.Query 调用
        /// </summary>
        private static void DemoBasicMapSql()
        {
            Console.WriteLine("=== 示例 1: 基本 Map SQL 查询 ===");
            Console.WriteLine("场景：使用 XML 定义 SQL，避免在代码中硬编码");
            Console.WriteLine();

            Console.WriteLine("XML 定义（map/user.xml）：");
            Console.WriteLine(@"  <sqlMap>
    <select id=""User.GetAll"">
      select a.[Id], a.[UserName], a.[Email], a.[Age], a.[IsActive]
      from User a
    </select>

    <select id=""User.GetById"">
      select a.[Id], a.[UserName], a.[Email], a.[Age], a.[IsActive]
      from User a
      where a.[Id] = ?Id
    </select>

    <select id=""User.GetActiveUsers"" log=""true"">
      select a.[Id], a.[UserName], a.[Email], a.[Age]
      from User a
      where a.[IsActive] = 1
      order by a.[CreateTime] desc
    </select>
  </sqlMap>");
            Console.WriteLine();

            Console.WriteLine("C# 调用：");
            Console.WriteLine(@"  // 1. 初始化 Map（在启动时调用一次）
  FastMap.InstanceMap(dbKey: ""DefaultDb"", mapFile: ""SqlMap.config"");

  // 2. 查询所有用户
  var allUsers = FastMap.Query<User>(""User.GetAll"", null, null, ""DefaultDb"");

  // 3. 根据 ID 查询
  var param = new List<DbParameter>
  {
      DbProviderFactories.GetFactory(""MySql.Data.MySqlClient"").CreateParameter()
  };
  param[0].ParameterName = ""Id"";
  param[0].Value = 1;
  var user = FastMap.Query<User>(""User.GetById"", param.ToArray(), null, ""DefaultDb"");

  // 4. 查询活跃用户（带日志）
  var activeUsers = FastMap.Query<User>(""User.GetActiveUsers"", null, null, ""DefaultDb"");");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - id 唯一标识 SQL 语句");
            Console.WriteLine("  - log=\"true\" 记录 SQL 执行日志");
            Console.WriteLine("  - ?ParameterName 表示参数占位符");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: 动态条件查询
        /// 场景：根据参数动态拼接 SQL 条件
        /// </summary>
        private static void DemoDynamicConditions()
        {
            Console.WriteLine("=== 示例 2: 动态条件查询 ===");
            Console.WriteLine("场景：根据传入参数动态拼接 WHERE 条件");
            Console.WriteLine();

            Console.WriteLine("XML 定义：");
            Console.WriteLine(@"  <sqlMap>
    <select id=""User.GetList"" log=""true"">
      select a.[Id], a.[UserName], a.[Email], a.[Age], a.[IsActive]
      from User a
      <dynamic prepend="" where 1=1"">
        <isPropertyAvailable prepend="" and "" property=""UserName"">
          a.[UserName] = ?UserName
        </isPropertyAvailable>
        <isPropertyAvailable prepend="" and "" property=""Email"">
          a.[Email] = ?Email
        </isPropertyAvailable>
        <isPropertyAvailable prepend="" and "" property=""IsActive"">
          a.[IsActive] = ?IsActive
        </isPropertyAvailable>
        <isEqual compareValue=""true"" prepend="" and "" property=""IsAdmin"">
          a.[Role] = 'Admin'
        </isEqual>
        <isNotEqual compareValue=""0"" prepend="" and "" property=""Age"">
          a.[Age] > ?Age
        </isNotEqual>
        <isNullOrEmpty prepend="" and "" property=""Department"">
          a.[Department] is null
        </isNullOrEmpty>
        <isNotNullOrEmpty prepend="" and "" property=""Department"">
          a.[Department] = ?Department
        </isNotNullOrEmpty>
      </dynamic>
    </select>
  </sqlMap>");
            Console.WriteLine();

            Console.WriteLine("C# 调用：");
            Console.WriteLine(@"  // 只传 UserName，其他条件自动忽略
  var param1 = new List<DbParameter>
  {
      CreateParameter(""UserName"", ""admin"")
  };
  var users1 = FastMap.Query<User>(""User.GetList"", param1.ToArray(), null, ""DefaultDb"");

  // 传 UserName 和 Age
  var param2 = new List<DbParameter>
  {
      CreateParameter(""UserName"", ""admin""),
      CreateParameter(""Age"", 25)
  };
  var users2 = FastMap.Query<User>(""User.GetList"", param2.ToArray(), null, ""DefaultDb"");");
            Console.WriteLine();

            Console.WriteLine("动态条件说明：");
            Console.WriteLine("  - isPropertyAvailable: 属性存在时添加条件");
            Console.WriteLine("  - isEqual: 属性值等于指定值时添加条件");
            Console.WriteLine("  - isNotEqual: 属性值不等于指定值时添加条件");
            Console.WriteLine("  - isNullOrEmpty: 属性值为空时添加条件");
            Console.WriteLine("  - isNotNullOrEmpty: 属性值不为空时添加条件");
            Console.WriteLine("  - isGreaterThan: 属性值大于指定值时添加条件");
            Console.WriteLine("  - isLessThan: 属性值小于指定值时添加条件");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: ForEach 批量查询
        /// 场景：根据 ID 列表批量查询关联数据
        /// </summary>
        private static void DemoForEach()
        {
            Console.WriteLine("=== 示例 3: ForEach 批量查询 ===");
            Console.WriteLine("场景：根据用户 ID 列表批量查询订单");
            Console.WriteLine();

            Console.WriteLine("XML 定义：");
            Console.WriteLine(@"  <sqlMap>
    <select id=""Order.GetByUserIds"">
      select o.[Id], o.[OrderNo], o.[Amount], o.[UserId], o.[CreateTime]
      from [Order] o
      <foreach name=""orders"" field=""UserId"" sql=""select * from [Order] where [UserId] = ?UserId"">
      </foreach>
    </select>
  </sqlMap>");
            Console.WriteLine();

            Console.WriteLine("C# 调用：");
            Console.WriteLine(@"  // 查询用户列表
  var users = FastRead.Query<User>().ToList();

  // 使用 ForEach 批量查询每个用户的订单
  var result = FastMap.ForEach(users, ""Order.GetByUserIds"", null, ""DefaultDb"");
  // result 中每个 User 对象会添加 orders 属性，包含该用户的订单列表");
            Console.WriteLine();

            Console.WriteLine("ForEach 说明：");
            Console.WriteLine("  - name: 结果属性名");
            Console.WriteLine("  - field: 关联字段（支持逗号分隔的复合字段）");
            Console.WriteLine("  - sql: 子查询 SQL");
            Console.WriteLine("  - type: 返回类型（可选，用于复杂类型映射）");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: 带参数的 Map SQL
        /// 场景：使用参数化查询防止 SQL 注入
        /// </summary>
        private static void DemoMapSqlWithParameters()
        {
            Console.WriteLine("=== 示例 4: 带参数的 Map SQL ===");
            Console.WriteLine("场景：参数化查询、分页查询、排序");
            Console.WriteLine();

            Console.WriteLine("XML 定义：");
            Console.WriteLine(@"  <sqlMap>
    <select id=""User.GetPaged"">
      select a.[Id], a.[UserName], a.[Email], a.[Age]
      from User a
      <dynamic prepend="" where 1=1"">
        <isPropertyAvailable prepend="" and "" property=""Keyword"">
          (a.[UserName] like ?Keyword or a.[Email] like ?Keyword)
        </isPropertyAvailable>
        <isPropertyAvailable prepend="" and "" property=""MinAge"">
          a.[Age] >= ?MinAge
        </isPropertyAvailable>
        <isPropertyAvailable prepend="" and "" property=""MaxAge"">
          a.[Age] <= ?MaxAge
        </isPropertyAvailable>
      </dynamic>
      order by a.[CreateTime] desc
      limit ?PageSize offset ?Offset
    </select>

    <select id=""User.Count"">
      select count(1)
      from User a
      <dynamic prepend="" where 1=1"">
        <isPropertyAvailable prepend="" and "" property=""Keyword"">
          (a.[UserName] like ?Keyword or a.[Email] like ?Keyword)
        </isPropertyAvailable>
      </dynamic>
    </select>
  </sqlMap>");
            Console.WriteLine();

            Console.WriteLine("C# 调用：");
            Console.WriteLine(@"  // 分页查询
  var pageIndex = 1;
  var pageSize = 10;
  var keyword = ""admin"";

  var param = new List<DbParameter>
  {
      CreateParameter(""Keyword"", ""%"" + keyword + ""%""),
      CreateParameter(""PageSize"", pageSize),
      CreateParameter(""Offset"", (pageIndex - 1) * pageSize)
  };
  var users = FastMap.Query<User>(""User.GetPaged"", param.ToArray(), null, ""DefaultDb"");

  // 查询总数
  var countParam = new List<DbParameter>
  {
      CreateParameter(""Keyword"", ""%"" + keyword + ""%"")
  };
  var total = FastMap.Query<int>(""User.Count"", countParam.ToArray(), null, ""DefaultDb"");");
            Console.WriteLine();

            Console.WriteLine("最佳实践：");
            Console.WriteLine("  - 始终使用参数化查询，避免 SQL 注入");
            Console.WriteLine("  - 复杂查询使用 Map SQL，简单查询使用 Lambda");
            Console.WriteLine("  - 为常用查询建立索引");
            Console.WriteLine("  - 使用 log=\"true\" 监控慢查询");
            Console.WriteLine();
        }
    }
}
