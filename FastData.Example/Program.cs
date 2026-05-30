using System;
using FastData.Example.Example;

namespace FastData.Example
{
    /// <summary>
    /// FastData ORM 使用示例入口
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  FastData ORM Usage Examples");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("FastData 是一个面向 .NET 的轻量 ORM 组件。");
            Console.WriteLine("支持 Lambda 查询、XML Map SQL、Code First、Db First、");
            Console.WriteLine("AOP、缓存、Redis 辅助能力和多数据库连接配置。");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("请选择示例：");
                Console.WriteLine("  1. 基本 CRUD 操作");
                Console.WriteLine("  2. Lambda 查询");
                Console.WriteLine("  3. 原始 SQL 查询");
                Console.WriteLine("  4. XML Map SQL");
                Console.WriteLine("  5. 事务操作");
                Console.WriteLine("  6. 多数据库连接");
                Console.WriteLine("  7. 数据同步工具");
                Console.WriteLine("  8. 消息队列（RTU 削峰/多方推送）");
                Console.WriteLine("  9. FastWrite/FastRead 链式 API（写入后端队列/查询队列）");
                Console.WriteLine("  10. 分页查询 API");
                Console.WriteLine("  11. 分表（数据分片）");
                Console.WriteLine("  12. 分表完整示例（SQL Server）");
                Console.WriteLine("  13. AOP 拦截器");
                Console.WriteLine("  14. 连接字符串加密");
                Console.WriteLine("  15. Repository 模式");
                Console.WriteLine("  16. 多数据库表名映射");
                Console.WriteLine("  17. Code First / Db First（Model 建表/读表结构）");
                Console.WriteLine("  18. 批量操作（BulkInsert/BulkUpdate/软删除）");
                Console.WriteLine("  19. Redis 缓存高级用法（穿透防护/分布式锁/预热）");
                Console.WriteLine("  20. 运行所有示例");
                Console.WriteLine("  0. 退出");
                Console.WriteLine();

                Console.Write("输入选项: ");
                var input = Console.ReadLine();

                if (input == "0")
                    break;

                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        BasicCrudExample.Run();
                        break;
                    case "2":
                        LambdaQueryExample.Run();
                        break;
                    case "3":
                        RawSqlExample.Run();
                        break;
                    case "4":
                        MapSqlExample.Run();
                        break;
                    case "5":
                        TransactionExample.Run();
                        break;
                    case "6":
                        MultiDbExample.Run();
                        break;
                    case "7":
                        DataSyncExample.Run();
                        break;
                    case "8":
#if !NETFRAMEWORK
                        MessageQueueExample.Run();
#else
                        Console.WriteLine("消息队列功能仅支持 .NET 6.0+");
#endif
                        break;
                    case "9":
#if !NETFRAMEWORK
                        MessageQueueExample.Run();
#else
                        Console.WriteLine("FastWrite/FastRead 链式 API 仅支持 .NET 6.0+");
#endif
                        break;
                    case "10":
                        PaginationExample.Run();
                        break;
                    case "11":
                        ShardingExample.Run();
                        break;
                    case "12":
                        ShardingFullExample.Run();
                        break;
                    case "13":
                        AopExample.Run();
                        break;
                    case "14":
                        EncryptionExample.Run();
                        break;
                    case "15":
                        RepositoryExample.Run();
                        break;
                    case "16":
                        MultiDbExample.Run();
                        break;
                    case "17":
                        CodeFirstExample.Run();
                        break;
                    case "18":
                        BulkOperationsExample.Run();
                        break;
                    case "19":
                        RedisAdvancedExample.Run();
                        break;
                    case "20":
                        BasicCrudExample.Run();
                        LambdaQueryExample.Run();
                        RawSqlExample.Run();
                        MapSqlExample.Run();
                        TransactionExample.Run();
                        MultiDbExample.Run();
                        DataSyncExample.Run();
#if !NETFRAMEWORK
                        MessageQueueExample.Run();
#endif
                        PaginationExample.Run();
                        ShardingExample.Run();
                        AopExample.Run();
                        EncryptionExample.Run();
                        RepositoryExample.Run();
                        CodeFirstExample.Run();
                        BulkOperationsExample.Run();
                        RedisAdvancedExample.Run();
                        break;
                    default:
                        Console.WriteLine("无效选项，请重新输入");
                        break;
                }

                Console.WriteLine();
            }

            Console.WriteLine("感谢使用 FastData！");
        }
    }
}
