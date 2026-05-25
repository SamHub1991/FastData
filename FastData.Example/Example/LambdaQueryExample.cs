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
            Console.WriteLine("   var users = FastRead.Query<User>().OrderBy(a => a.CreateTime, true).ToList(); // true = descending");
            Console.WriteLine();

            Console.WriteLine("5. 分页");
            Console.WriteLine("   var users = FastRead.Query<User>().Skip(0).Take(10).ToList();");
            Console.WriteLine();

            Console.WriteLine("6. 聚合查询");
            Console.WriteLine("   var count = FastRead.Query<User>().Count();");
            Console.WriteLine("   var exists = FastRead.Query<User>(a => a.Id == 1).Any();");
            Console.WriteLine();

            Console.WriteLine("7. 关联查询");
            Console.WriteLine("   var orders = FastRead.Query<Order>()");
            Console.WriteLine("       .Join<User>((o, u) => o.UserId == u.Id)");
            Console.WriteLine("       .Select((o, u) => new { o.OrderNo, u.UserName, o.Amount })");
            Console.WriteLine("       .ToList();");
            Console.WriteLine();

            Console.WriteLine("提示：运行实际示例需要配置数据库连接，请参考 README.md");
            Console.WriteLine();
        }
    }
}