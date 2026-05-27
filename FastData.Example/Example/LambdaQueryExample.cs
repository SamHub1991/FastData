using System;
using System.Linq;
using FastData.Example.Model;
using FastData.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// Lambda 查询示例 - 覆盖 DataQuery&lt;T&gt; 和 Where&lt;T&gt; 新 API
    /// </summary>
    public class LambdaQueryExample
    {
        /// <summary>
        /// 演示 Lambda 查询功能
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== Lambda Query Example ===");
            Console.WriteLine();

            // 1. 基础条件查询
            Console.WriteLine("1. 基础条件查询");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive).ToList();");
            Console.WriteLine();

            // 2. DataQuery<T> 链式调用（只需写一次 <User>）
            Console.WriteLine("2. DataQuery<T> 链式调用（只需写一次 <User>）");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .And(u => u.Age > 18)");
            Console.WriteLine("       .Or(u => u.Role == \"Admin\")");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 3. 链式 Like 条件
            Console.WriteLine("3. 链式 Like 条件");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .Like(u => u.UserName, \"张%\")");
            Console.WriteLine("       .Contains(u => u.Email, \"test\")");
            Console.WriteLine("       .StartsWith(u => u.Address, \"北京\")");
            Console.WriteLine("       .EndsWith(u => u.Phone, \"8888\")");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 4. 链式 In/Between 条件
            Console.WriteLine("4. 链式 In/Between 条件");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .In(u => u.Department, new[] { \"IT\", \"HR\", \"Finance\" })");
            Console.WriteLine("       .Between(u => u.Age, 18, 65)");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 5. 匿名类型投影（Select）
            Console.WriteLine("5. 匿名类型投影（Select）");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Email })");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 6. 投影 + 分页
            Console.WriteLine("6. 投影 + 分页");
            Console.WriteLine("   var result = FastRead.Query<User>(u => u.IsActive && u.Age > 18)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Department })");
            Console.WriteLine("       .ToPagination(page: 1, pageSize: 10);");
            Console.WriteLine("   // result.Total, result.TotalPages, result.Data");
            Console.WriteLine();

            // 7. Where<T> 条件构建器（分开写条件）
            Console.WriteLine("7. Where<T> 条件构建器（分开写条件，更清晰）");
            Console.WriteLine("   var where = new Where<User>();");
            Console.WriteLine("   where.Add(u => u.IsActive);");
            Console.WriteLine("   where.And(u => u.Age > 18);");
            Console.WriteLine("   where.Or(u => u.Role == \"Admin\");");
            Console.WriteLine("   where.Like(u => u.UserName, \"张%\");");
            Console.WriteLine("   where.In(u => u.Department, new[] { \"IT\", \"HR\" });");
            Console.WriteLine("   where.Between(u => u.Age, 18, 65);");
            Console.WriteLine();
            Console.WriteLine("   var users = FastRead.Query<User>(u => true)");
            Console.WriteLine("       .Where(where)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 8. 动态条件构建
            Console.WriteLine("8. 动态条件构建（根据参数决定是否添加条件）");
            Console.WriteLine("   var where = new Where<User>();");
            Console.WriteLine("   where.Add(u => u.IsActive);");
            Console.WriteLine();
            Console.WriteLine("   if (!string.IsNullOrEmpty(keyword))");
            Console.WriteLine("       where.Like(u => u.UserName, keyword + \"%\");");
            Console.WriteLine();
            Console.WriteLine("   if (minAge > 0)");
            Console.WriteLine("       where.And(u => u.Age >= minAge);");
            Console.WriteLine();
            Console.WriteLine("   if (departments != null && departments.Length > 0)");
            Console.WriteLine("       where.In(u => u.Department, departments.Cast<object>());");
            Console.WriteLine();
            Console.WriteLine("   var users = FastRead.Query<User>(u => true)");
            Console.WriteLine("       .Where(where)");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            // 9. 组合使用
            Console.WriteLine("9. 组合使用多种链式条件");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .And(u => u.Age > 18)");
            Console.WriteLine("       .Or(u => u.Role == \"Admin\")");
            Console.WriteLine("       .Like(u => u.UserName, \"张%\")");
            Console.WriteLine("       .In(u => u.Department, new[] { \"IT\", \"HR\" })");
            Console.WriteLine("       .Between(u => u.Salary, 5000, 50000)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Department, u.Salary })");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            Console.WriteLine("提示：运行实际示例需要配置数据库连接，请参考 README.md");
            Console.WriteLine();
        }
    }
}
