using System;
using FastData;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// Code First 示例
    /// </summary>
    public static class CodeFirstExample
    {
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Code First 示例 - 从 Model 创建表");
            Console.WriteLine("========================================");
            Console.WriteLine();

            Console.WriteLine("【1】Code First API");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("// 根据 Model 创建表:");
            Console.WriteLine("var result = FastWrite.CodeFirst<User>();");
            Console.WriteLine();
            Console.WriteLine("// 强制删除已存在的表并重建:");
            Console.WriteLine("var result = FastWrite.CodeFirst<User>(isDropExists: true);");
            Console.WriteLine();

            Console.WriteLine("【2】Model 特性配置");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("[Table(Name = \"Users\")]");
            Console.WriteLine("public class User");
            Console.WriteLine("{");
            Console.WriteLine("    [Primary]                          // 主键");
            Console.WriteLine("    public int Id { get; set; }");
            Console.WriteLine();
            Console.WriteLine("    [Column(IsNull = false, Comments = \"用户名\")]");
            Console.WriteLine("    public string UserName { get; set; }");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine("【3】执行 CodeFirst");
            Console.WriteLine("----------------------------------------");
            try
            {
                Console.WriteLine("正在创建表 CodeFirst_Users...");
                var result = FastWrite.CodeFirst<CodeFirstUser>();
                Console.WriteLine($"  {result.IsSuccess} - {result.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  执行失败：{ex.Message}");
            }
        }
    }

    [Table(Name = "CodeFirst_Users")]
    public class CodeFirstUser
    {
        [Primary]
        public long Id { get; set; }

        [Column(IsNull = false, Comments = "用户名")]
        public string UserName { get; set; }

        [Column(IsNull = false, Comments = "邮箱")]
        public string Email { get; set; }

        [Column(Comments = "年龄")]
        public int Age { get; set; }

        [Column(Comments = "创建时间")]
        public DateTime CreatedTime { get; set; }

        [Column(Comments = "是否激活")]
        public bool IsActive { get; set; }
    }
}
