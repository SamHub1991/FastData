using System;
using FastData.Example.Example;

namespace FastData.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  FastData ORM Business Examples");
            Console.WriteLine("========================================");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("--- 业务场景示例 ---");
                Console.WriteLine("  1. 用户管理（注册/登录/缓存/分页/软删除）");
                Console.WriteLine("  2. 订单业务（下单/支付/发货/取消/事务）");
                Console.WriteLine("  3. 缓存策略（穿透/雪崩/预热/分布式锁）");
                Console.WriteLine("  4. 消息队列（IoT削峰/订单通知/日志异步）");
                Console.WriteLine("  5. 多数据库（读写分离/跨库查询）");
                Console.WriteLine("  6. 分表业务（按时间/按哈希/跨片查询）");
                Console.WriteLine("  7. 仓储模式（DI集成/泛型仓储）");
                Console.WriteLine("  8. 报表统计（Join/GroupBy/JSON/DataTable导出）");
                Console.WriteLine("  9. 动态查询（条件构建器/Any/All/First/Single）");
                Console.WriteLine("  10. 数据校验（空值安全/字段验证/异常降级）");
                Console.WriteLine("  11. API认证（Token/JWT/RSA/AES/统一返回格式）");
                Console.WriteLine("  12. 可信队列（连接池满/超时自动降级/批量写入）");
                Console.WriteLine("  13. FastDataClient消息队列（统一入口/链式API）");
                Console.WriteLine("  14. 高频数据缓存（Web缓存/Redis/数据一致性）");
                Console.WriteLine();
                Console.WriteLine("--- API 参考示例 ---");
                Console.WriteLine("  20. 基本 CRUD 操作");
                Console.WriteLine("  21. Lambda 查询");
                Console.WriteLine("  22. 原始 SQL 查询");
                Console.WriteLine("  23. XML Map SQL");
                Console.WriteLine("  24. 事务操作");
                Console.WriteLine("  25. 分页查询 API");
                Console.WriteLine("  26. AOP 拦截器");
                Console.WriteLine("  27. Code First 建表");
                Console.WriteLine("  28. 批量操作（BulkInsert/批量更新）");
                Console.WriteLine("  29. Redis 缓存高级用法");
                Console.WriteLine();
                Console.WriteLine("  99. 运行所有业务场景示例");
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
                        UserManagementExample.Run();
                        break;
                    case "2":
                        OrderBusinessExample.Run();
                        break;
                    case "3":
                        CacheStrategyExample.Run();
                        break;
                    case "4":
#if !NETFRAMEWORK
                        MessageQueueExample.Run();
#else
                        Console.WriteLine("消息队列功能仅支持 .NET 6.0+");
#endif
                        break;
                    case "5":
                        MultiDbExample.Run();
                        break;
                    case "6":
                        ShardingExample.Run();
                        break;
                    case "7":
                        RepositoryExample.Run();
                        break;
                    case "8":
                        ReportExample.Run();
                        break;
                    case "9":
                        DynamicQueryExample.Run();
                        break;
                    case "20":
                        BasicCrudExample.Run();
                        break;
                    case "21":
                        LambdaQueryExample.Run();
                        break;
                    case "22":
                        RawSqlExample.Run();
                        break;
                    case "23":
                        MapSqlExample.Run();
                        break;
                    case "24":
                        TransactionExample.Run();
                        break;
                    case "25":
                        PaginationExample.Run();
                        break;
                    case "26":
                        AopExample.Run();
                        break;
                    case "27":
                        CodeFirstExample.Run();
                        break;
                    case "28":
                        BulkOperationsExample.Run();
                        break;
                    case "29":
                        RedisAdvancedExample.Run();
                        break;
                    case "10":
                        DataValidationExample.Run();
                        break;
                    case "11":
                        ApiAuthenticationExample.Run();
                        break;
                    case "12":
                        ReliableQueueExample.Run();
                        break;
                    case "13":
                        FastDataClientQueueExample.Run();
                        break;
                    case "14":
                        HighFrequencyCacheExample.Run();
                        break;
                    case "99":
                        UserManagementExample.Run();
                        OrderBusinessExample.Run();
                        CacheStrategyExample.Run();
#if !NETFRAMEWORK
                        MessageQueueExample.Run();
#endif
                        MultiDbExample.Run();
                        ShardingExample.Run();
                        RepositoryExample.Run();
                        ReportExample.Run();
                        DynamicQueryExample.Run();
                        DataValidationExample.Run();
                        ApiAuthenticationExample.Run();
                        ReliableQueueExample.Run();
                        FastDataClientQueueExample.Run();
                        HighFrequencyCacheExample.Run();
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
