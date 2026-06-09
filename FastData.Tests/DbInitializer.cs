using System;
using FastData;
using FastData.Context;
using FastData.Tests.Integration;
using FastData.Infrastructure;

namespace FastData.Tests
{
    /// <summary>
    /// 数据库初始化脚本 - 为所有数据库创建测试表结构
    /// </summary>
    public static class DbInitializer
    {
        public static void Init()
        {
            // 注册数据库提供程序
            DbProviderAutoRegistrar.Register();

            var databases = new[] { "SqlServer", "MySql", "PostgreSql", "Sqlite" };

            foreach (var dbName in databases)
            {
                try
                {
                    Console.WriteLine($"\n初始化数据库: {dbName}");
                    
                    using var db = new DataContext(dbName);
                    
                    // 通过插入一条记录来触发 CodeFirst 自动建表
                    var user = new PerfUser
                    {
                        UserName = $"InitUser_{dbName}_{DateTime.Now.Ticks}",
                        Email = $"init_{dbName}@example.com",
                        Age = 1,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    var result = db.Add(user);
                    
                    if (result.WriteReturn.IsSuccess)
                    {
                        Console.WriteLine($"  {dbName}: 表创建成功");
                        // 清理初始化数据
                        db.Delete<PerfUser>(u => u.UserName == user.UserName);
                        Console.WriteLine($"  {dbName}: 初始化数据已清理");
                    }
                    else
                    {
                        Console.WriteLine($"  {dbName}: 创建失败 - {result.WriteReturn.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {dbName}: 异常 - {ex.Message}");
                }
            }

            Console.WriteLine("\n初始化完成");
        }
    }
}
