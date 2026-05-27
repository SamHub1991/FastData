using System;
using System.Linq;
using FastData.Example.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// Lambda 查询示例
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

            Console.WriteLine("1. 条件查询");
            Console.WriteLine("   var activeUsers = FastRead.Query<User>(a => a.IsActive == true).ToList();");
            Console.WriteLine();

            Console.WriteLine("2. 多条件查询（AND）");
            Console.WriteLine("   var users = FastRead.Query<User>(a => a.IsActive == true && a.CreateTime > DateTime.Now.AddDays(-7)).ToList();");
            Console.WriteLine();

            Console.WriteLine("3. 多条件查询（OR）");
            Console.WriteLine("   var users = FastRead.Query<User>(a => a.UserName == \"admin\" || a.UserName == \"root\").ToList();");
            Console.WriteLine();

            Console.WriteLine("4. 排序");
            Console.WriteLine("   var users = FastRead.Query<User>().OrderBy<User>(a => a.CreateTime, true).ToList();");
            Console.WriteLine();

            Console.WriteLine("5. 分页（简化API）");
            Console.WriteLine("   var result = FastRead.Query<User>()");
            Console.WriteLine("       .OrderBy<User>(a => a.Id)");
            Console.WriteLine("       .ToPagination<User>(page: 1, pageSize: 10);");
            Console.WriteLine("   // result.Total, result.TotalPages, result.Data");
            Console.WriteLine();

            Console.WriteLine("6. 聚合查询");
            Console.WriteLine("   var count = FastRead.Query<User>().Count();");
            Console.WriteLine("   var exists = FastRead.Query<User>(a => a.Id == 1).Any();");
            Console.WriteLine();

            Console.WriteLine("7. 匿名类型投影（Select）");
            Console.WriteLine("   // 只查询需要的字段，减少数据传输");
            Console.WriteLine("   var users = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Email })");
            Console.WriteLine("       .ToList();");
            Console.WriteLine("   // users 类型为 List<{ int Id, string UserName, string Email }>");
            Console.WriteLine();

            Console.WriteLine("8. 匿名类型投影 + 分页");
            Console.WriteLine("   // 带过滤条件的分页投影查询");
            Console.WriteLine("   var result = FastRead.Query<User>(u => u.IsActive && u.Age > 18)");
            Console.WriteLine("       .OrderBy<User>(u => u.Id)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Department })");
            Console.WriteLine("       .ToPagination(page: 1, pageSize: 10);");
            Console.WriteLine("   // result.Total = 过滤后的总记录数");
            Console.WriteLine("   // result.TotalPages = 总页数");
            Console.WriteLine("   // result.Data = 当前页的投影数据");
            Console.WriteLine();

            Console.WriteLine("9. 匿名类型投影 + 条件过滤");
            Console.WriteLine("   // 按部门过滤，只返回部分字段");
            Console.WriteLine("   var deptUsers = FastRead.Query<User>(u => u.Department == \"IT\")");
            Console.WriteLine("       .OrderBy<User>(u => u.UserName)");
            Console.WriteLine("       .Select(u => new { u.Id, u.UserName, u.Email })");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            Console.WriteLine("提示：运行实际示例需要配置数据库连接，请参考 README.md");
            Console.WriteLine();
        }
    }
}