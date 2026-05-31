using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FastData;
using FastData.Example.Model;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 报表统计业务场景
    /// 
    /// 覆盖 ORM 功能：
    /// - GroupBy 分组聚合
    /// - ToJson 返回 JSON
    /// - ToDics 返回字典列表
    /// - ToDataTable 返回 DataTable
    /// - ExecuteSql 原生 SQL 聚合
    /// - Select 投影查询
    /// - ToCount 计数
    /// </summary>
    public static class ReportExample
    {
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  报表统计业务场景");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunGroupByReport();
            Console.WriteLine();
            RunJsonExport();
            Console.WriteLine();
            RunDicExport();
            Console.WriteLine();
            RunDataTableExport();
            Console.WriteLine();
            RunAggregateReport();
        }

        #region 分组聚合

        /// <summary>
        /// 订单统计报表：按状态/用户/时间分组
        /// 
        /// 业务场景：管理层需要查看订单分布、用户消费统计、月度趋势
        /// </summary>
        private static void RunGroupByReport()
        {
            Console.WriteLine("【1】分组聚合 - 订单统计");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 按订单状态分组统计
                var statusReport = FastRead.Query<Order>(o => o.Id > 0)
                    .GroupBy<Order>(o => o.Status)
                    .ToDics();

                Console.WriteLine("  订单状态分布:");
                foreach (var row in statusReport)
                {
                    var status = Convert.ToInt32(row.GetValueOrDefault("Status", 0));
                    var count = Convert.ToInt32(row.GetValueOrDefault("Count", 0));
                    var statusName = status == 0 ? "待支付" :
                                     status == 1 ? "已支付" :
                                     status == 2 ? "已发货" :
                                     status == 3 ? "已完成" : "已取消";
                    Console.WriteLine($"    {statusName}: {count} 单");
                }

                // 2. 按用户分组统计消费金额
                var userReport = FastRead.Query<Order>(o => o.Status >= 1)
                    .GroupBy<Order>(o => o.UserId)
                    .ToDics();

                Console.WriteLine($"  消费用户数: {userReport.Count}");

                // 3. 使用原生 SQL 做复杂聚合
                var monthlyReport = FastRead.ExecuteSql(@"
                    SELECT 
                        YEAR(CreateTime) as Year,
                        MONTH(CreateTime) as Month,
                        COUNT(*) as OrderCount,
                        SUM(TotalAmount) as TotalAmount,
                        AVG(TotalAmount) as AvgAmount
                    FROM Orders 
                    WHERE Status >= 1
                    GROUP BY YEAR(CreateTime), MONTH(CreateTime)
                    ORDER BY Year DESC, Month DESC", null);

                Console.WriteLine("  月度统计:");
                foreach (var row in monthlyReport)
                {
                    var year = row.GetValueOrDefault("Year", "");
                    var month = row.GetValueOrDefault("Month", "");
                    var orderCount = row.GetValueOrDefault("OrderCount", "");
                    var totalAmount = row.GetValueOrDefault("TotalAmount", "");
                    Console.WriteLine($"    {year}-{month}: {orderCount} 单, 总金额 {totalAmount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  统计异常: {ex.Message}");
            }
        }

        #endregion

        #region JSON 导出

        /// <summary>
        /// 数据导出为 JSON 格式
        /// 
        /// 业务场景：前端 API 返回 JSON、数据备份、第三方接口对接
        /// </summary>
        private static void RunJsonExport()
        {
            Console.WriteLine("【2】JSON 导出");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 导出活跃用户为 JSON
                var userJson = FastRead.Query<User>(u => u.IsActive)
                    .OrderBy<User>(u => u.Id)
                    .Take(10)
                    .ToJson();

                Console.WriteLine($"  用户 JSON (前10条): {userJson.Substring(0, Math.Min(200, userJson.Length))}...");

                // 2. 导出订单统计为 JSON
                var orderJson = FastRead.Query<Order>(o => o.Status == 1)
                    .OrderBy<User>(o => o.CreateTime)
                    .Take(5)
                    .ToJson();

                Console.WriteLine($"  订单 JSON (前5条): {orderJson.Substring(0, Math.Min(200, orderJson.Length))}...");

                // 3. 使用原生 SQL 查询后转 JSON
                var statsJson = FastRead.ExecuteSql(@"
                    SELECT 
                        Status,
                        COUNT(*) as Count,
                        SUM(TotalAmount) as TotalAmount
                    FROM Orders 
                    GROUP BY Status", null);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(statsJson);
                Console.WriteLine($"  统计 JSON: {json.Substring(0, Math.Min(200, json.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  JSON 导出异常: {ex.Message}");
            }
        }

        #endregion

        #region 字典导出

        /// <summary>
        /// 数据导出为字典列表
        /// 
        /// 业务场景：动态列报表、Excel 导出、数据透视
        /// </summary>
        private static void RunDicExport()
        {
            Console.WriteLine("【3】字典列表导出");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 导出为字典列表（适合动态列场景）
                var userDics = FastRead.Query<User>(u => u.IsActive)
                    .OrderBy<User>(u => u.Id)
                    .Take(5)
                    .ToDics();

                Console.WriteLine($"  导出 {userDics.Count} 条用户数据:");
                foreach (var dic in userDics)
                {
                    Console.WriteLine($"    ID={dic.GetValueOrDefault("Id")}, " +
                                      $"Name={dic.GetValueOrDefault("UserName")}, " +
                                      $"Email={dic.GetValueOrDefault("Email")}");
                }

                // 2. 导出单条为字典（适合配置查询）
                var configDic = FastRead.Query<User>(u => u.Id == 1)
                    .ToDics();

                if (configDic.Count > 0)
                {
                    Console.WriteLine($"  单条字典: {string.Join(", ", configDic[0].Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  字典导出异常: {ex.Message}");
            }
        }

        #endregion

        #region DataTable 导出

        /// <summary>
        /// 数据导出为 DataTable
        /// 
        /// 业务场景：Excel 导出、报表打印、数据绑定（WinForms/WPF）
        /// </summary>
        private static void RunDataTableExport()
        {
            Console.WriteLine("【4】DataTable 导出");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 导出为 DataTable（适合 Excel 导出）
                var dt = FastRead.Query<User>(u => u.IsActive)
                    .OrderBy<User>(u => u.Id)
                    .Take(10)
                    .ToDataTable();

                Console.WriteLine($"  DataTable: {dt.Rows.Count} 行, {dt.Columns.Count} 列");
                Console.WriteLine($"  列名: {string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                // 2. 遍历 DataTable
                foreach (DataRow row in dt.Rows)
                {
                    Console.WriteLine($"    ID={row["Id"]}, Name={row["UserName"]}, Email={row["Email"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  DataTable 导出异常: {ex.Message}");
            }
        }

        #endregion

        #region 聚合统计

        /// <summary>
        /// 业务数据聚合统计
        /// 
        /// 业务场景：Dashboard 数据看板、KPI 统计
        /// </summary>
        private static void RunAggregateReport()
        {
            Console.WriteLine("【5】聚合统计 - Dashboard");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 用户统计
                var totalUsers = FastRead.Query<User>(u => u.Id > 0).ToCount();
                var activeUsers = FastRead.Query<User>(u => u.IsActive).ToCount();
                var deletedUsers = FastRead.Query<User>(u => u.IsDeleted).ToCount();

                Console.WriteLine($"  用户总数: {totalUsers}");
                Console.WriteLine($"  活跃用户: {activeUsers}");
                Console.WriteLine($"  已删除用户: {deletedUsers}");
                Console.WriteLine($"  活跃率: {(totalUsers > 0 ? (activeUsers * 100.0 / totalUsers).ToString("F1") : "0")}%");

                // 2. 订单统计
                var totalOrders = FastRead.Query<Order>(o => o.Id > 0).ToCount();
                var paidOrders = FastRead.Query<Order>(o => o.Status == 1).ToCount();
                var completedOrders = FastRead.Query<Order>(o => o.Status == 3).ToCount();

                Console.WriteLine($"  订单总数: {totalOrders}");
                Console.WriteLine($"  已支付: {paidOrders}");
                Console.WriteLine($"  已完成: {completedOrders}");

                // 3. 使用原生 SQL 做精确统计
                var stats = FastRead.ExecuteSql(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Users WHERE IsActive = 1) as ActiveUsers,
                        (SELECT COUNT(*) FROM Orders WHERE Status = 1) as PaidOrders,
                        (SELECT SUM(TotalAmount) FROM Orders WHERE Status >= 1) as TotalRevenue", null);

                if (stats.Count > 0)
                {
                    var row = stats[0];
                    Console.WriteLine($"  总营收: {row.GetValueOrDefault("TotalRevenue", "0")}");
                }

                // 4. 使用 Select 投影查询
                var userSummary = FastRead.Query<User>(u => u.IsActive)
                    .Select(u => new
                    {
                        u.UserName,
                        u.Email,
                        u.Age
                    })
                    .ToList();

                Console.WriteLine($"  投影查询结果: {userSummary.Count} 条");
                foreach (var item in userSummary.Take(3))
                {
                    Console.WriteLine($"    {item.UserName}, {item.Email}, {item.Age}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  统计异常: {ex.Message}");
            }
        }

        #endregion
    }
}
