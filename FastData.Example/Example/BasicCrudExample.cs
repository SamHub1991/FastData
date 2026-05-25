using System;
using FastData.Example.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// 基本 CRUD 操作示例
    /// </summary>
    public class BasicCrudExample
    {
        /// <summary>
        /// 演示基本 CRUD 操作
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== Basic CRUD Example ===");
            Console.WriteLine();

            // 注意：以下示例需要配置数据库连接才能运行
            // 配置方式：
            // 1. 在 app.config 或 web.config 中添加 FastData 配置节
            // 2. 或使用代码配置：FastData.Config.DataConfig.LoadConfig()

            Console.WriteLine("1. INSERT - 插入数据");
            Console.WriteLine("   var user = new User { UserName = \"test\", Email = \"test@example.com\" };");
            Console.WriteLine("   FastWrite.Add(user).Add(user2).Execute();");
            Console.WriteLine();

            Console.WriteLine("2. SELECT - 查询数据");
            Console.WriteLine("   var users = FastRead.Query<User>().ToList();");
            Console.WriteLine("   var user = FastRead.Query<User>(a => a.Id == 1).FirstOrDefault();");
            Console.WriteLine();

            Console.WriteLine("3. UPDATE - 更新数据");
            Console.WriteLine("   FastWrite.Update<User>(a => a.Id == 1)");
            Console.WriteLine("       .Set(a => a.UserName, \"newname\")");
            Console.WriteLine("       .Set(a => a.Email, \"new@example.com\")");
            Console.WriteLine("       .Execute();");
            Console.WriteLine();

            Console.WriteLine("4. DELETE - 删除数据");
            Console.WriteLine("   FastWrite.Delete<User>(a => a.Id == 1).Execute();");
            Console.WriteLine();

            Console.WriteLine("5. UPSERT - 存在则更新，不存在则插入");
            Console.WriteLine("   FastWrite.Upsert<User>(user, a => a.Id == user.Id).Execute();");
            Console.WriteLine();

            Console.WriteLine("提示：运行实际示例需要配置数据库连接，请参考 README.md");
            Console.WriteLine();
        }
    }
}