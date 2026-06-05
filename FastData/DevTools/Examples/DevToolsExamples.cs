using System;
using System.Collections.Generic;
using FastData;
using FastData.DevTools;
using FastData.Model;

namespace FastData.DevTools.Examples
{
    /// <summary>
    /// DevTools 使用示例
    /// </summary>
    public class DevToolsExamples
    {
        public static void RunAllExamples()
        {
            Console.WriteLine("=== FastData DevTools 示例 ===\n");

            CodeGeneratorExample();
            DatabaseDiagnosticExample();
            DatabaseComparerExample();
            DataImporterExample();
            CacheManagerExample();
            AuditLoggerExample();
            SqlQueryBuilderExample();

            Console.WriteLine("\n=== 所有示例运行完成 ===");
        }

        /// <summary>
        /// 代码生成器示例
        /// </summary>
        public static void CodeGeneratorExample()
        {
            Console.WriteLine("1. 代码生成器示例:");

            // 生成 Model
            var modelCode = CodeGenerator.GenerateModel<User>();
            Console.WriteLine("生成的 Model 代码:");
            Console.WriteLine(modelCode.Substring(0, Math.Min(200, modelCode.Length)) + "...\n");

            // 生成 Repository
            var repoCode = CodeGenerator.GenerateRepository<User>();
            Console.WriteLine("生成的 Repository 代码:");
            Console.WriteLine(repoCode.Substring(0, Math.Min(200, repoCode.Length)) + "...\n");

            // 生成 Service
            var serviceCode = CodeGenerator.GenerateService<User>();
            Console.WriteLine("生成的 Service 代码:");
            Console.WriteLine(serviceCode.Substring(0, Math.Min(200, serviceCode.Length)) + "...\n");

            // 生成 Controller
            var controllerCode = CodeGenerator.GenerateController<User>();
            Console.WriteLine("生成的 Controller 代码:");
            Console.WriteLine(controllerCode.Substring(0, Math.Min(200, controllerCode.Length)) + "...\n");
        }

        /// <summary>
        /// 数据库诊断示例
        /// </summary>
        public static void DatabaseDiagnosticExample()
        {
            Console.WriteLine("2. 数据库诊断示例:");

            try
            {
                // 测试连接
                var (connected, message) = DatabaseDiagnostic.TestConnection("DefaultDb");
                Console.WriteLine(string.Format("连接测试: {0}", (connected ? "成功" : "失败")));
                Console.WriteLine(string.Format("消息: {0}\n", message));

                // 分析性能
                var slowQueries = DatabaseDiagnostic.AnalyzeSlowQueries("DefaultDb", TimeSpan.FromSeconds(1));
                Console.WriteLine(string.Format("慢查询数量: {0}\n", slowQueries.Count));

                // 检查表结构
                var issues = DatabaseDiagnostic.CheckTableStructure("DefaultDb", "User");
                Console.WriteLine(string.Format("表结构问题: {0}", issues.Count));
                foreach (var issue in issues)
                {
                    Console.WriteLine(string.Format("  - {0}", issue));
                }
                Console.WriteLine();

                // 获取诊断报告
                var report = DatabaseDiagnostic.GetDiagnosticReport("DefaultDb");
                Console.WriteLine("诊断报告:");
                Console.WriteLine(string.Format("  数据库大小: {0} MB", report.DatabaseSizeMB));
                Console.WriteLine(string.Format("  表数量: {0}", report.TableCount));
                Console.WriteLine(string.Format("  慢查询数: {0}", report.SlowQueryCount));
                Console.WriteLine(string.Format("  健康评分: {0}", report.HealthScore));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("诊断失败: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// 数据库比较示例
        /// </summary>
        public static void DatabaseComparerExample()
        {
            Console.WriteLine("3. 数据库比较示例:");

            try
            {
                // 比较两个数据库
                var diff = DatabaseComparer.CompareDatabases("SourceDb", "TargetDb");

                Console.WriteLine("数据库差异:");
                Console.WriteLine(string.Format("  有差异: {0}", diff.HasDifferences));
                Console.WriteLine(string.Format("  表差异: {0}", diff.TableDifferences.Count));
                Console.WriteLine(string.Format("  列差异: {0}", diff.ColumnDifferences.Count));
                Console.WriteLine(string.Format("  数据差异: {0}", diff.DataDifferences.Count));
                Console.WriteLine(string.Format("  索引差异: {0}", diff.IndexDifferences.Count));

                // 生成同步脚本
                if (diff.HasDifferences)
                {
                    var syncScript = DatabaseComparer.GenerateSyncScript(diff);
                    Console.WriteLine("\n生成的同步脚本:");
                    Console.WriteLine(syncScript);
                }
                else
                {
                    Console.WriteLine("\n无差异，无需同步");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("比较失败: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// 数据导入导出示例
        /// </summary>
        public static void DataImporterExample()
        {
            Console.WriteLine("4. 数据导入导出示例:");

            try
            {
                // 准备测试数据
                var users = new List<User>
                {
                    new User { Name = "John Doe", Email = "john@example.com", IsActive = true },
                    new User { Name = "Jane Smith", Email = "jane@example.com", IsActive = true },
                    new User { Name = "Bob Wilson", Email = "bob@example.com", IsActive = false }
                };

                // 批量导入
                var (success, failed) = DataImporter.BatchImport(users, batchSize: 10, dbKey: "DefaultDb");
                Console.WriteLine(string.Format("批量导入: 成功 {0}, 失败 {1}", success, failed));

                // 导出为 CSV
                DataImporter.ExportToCSV<User>("users.csv", u => u.IsActive == true, "DefaultDb");
                Console.WriteLine("已导出到 users.csv");

                // 导出为 JSON
                JsonImporter.ExportToJson<User>("users.json", u => u.IsActive == true, "DefaultDb");
                Console.WriteLine("已导出到 users.json");

                // 导出为 Excel（CSV 格式）
                ExcelImporter.ExportToExcel<User>("users.xlsx", u => u.IsActive == true, "DefaultDb");
                Console.WriteLine("已导出到 users.xlsx");

                // 从 CSV 导入
                var (importSuccess, importFailed) = DataImporter.ImportFromCSV<User>("users.csv", "DefaultDb");
                Console.WriteLine(string.Format("从 CSV 导入: 成功 {0}, 失败 {1}", importSuccess, importFailed));

                // 数据同步（增量更新）
                var (inserted, updated, syncFailed) = DataImporter.SyncData(
                    users,
                    keySelector: u => u.Email,
                    dbKey: "DefaultDb"
                );
                Console.WriteLine(string.Format("数据同步: 插入 {0}, 更新 {1}, 失败 {2}", inserted, updated, syncFailed));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("导入导出失败: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// 缓存管理器示例
        /// </summary>
        public static void CacheManagerExample()
        {
            Console.WriteLine("5. 缓存管理器示例:");

            try
            {
                // 设置缓存
                var user = new User { Name = "Test User", Email = "test@example.com", IsActive = true };
                CacheManager.Set("user:1", user, TimeSpan.FromMinutes(30));
                Console.WriteLine("已设置缓存: user:1");

                // 获取缓存
                var cachedUser = CacheManager.Get<User>("user:1");
                Console.WriteLine(string.Format("从缓存获取: {0}", cachedUser?.Name));

                // 检查缓存是否存在
                var exists = CacheManager.Exists("user:1");
                Console.WriteLine(string.Format("缓存存在: {0}", exists));

                // 获取或创建缓存
                var user2 = CacheManager.GetOrAdd("user:2", () => new User
                {
                    Name = "New User",
                    Email = "new@example.com",
                    IsActive = true
                }, TimeSpan.FromMinutes(30));
                Console.WriteLine(string.Format("获取或创建: {0}", user2?.Name));

                // 带缓存的查询
                var cachedUsers = QueryCacheDecorator.QueryWithCache<User>(
                    u => u.IsActive == true,
                    TimeSpan.FromMinutes(10),
                    "DefaultDb"
                );
                Console.WriteLine(string.Format("带缓存的查询: {0} 条记录", cachedUsers?.Count));

                // 缓存自动失效
                var result = CacheInvalidationInterceptor.InterceptAdd(new User
                {
                    Name = "Fresh User",
                    Email = "fresh@example.com",
                    IsActive = true
                }, "DefaultDb");
                Console.WriteLine(string.Format("缓存失效: {0}", result.IsSuccess));

                // 获取缓存统计
                var stats = CacheManager.GetStats();
                Console.WriteLine("\n缓存统计:");
                Console.WriteLine(string.Format("  总键数: {0}", stats.TotalKeys));
                Console.WriteLine(string.Format("  内存限制: {0} bytes", stats.MemoryLimit));
                Console.WriteLine(string.Format("  物理内存限制: {0} bytes", stats.PhysicalMemoryLimit));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("缓存操作失败: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// 审计日志示例
        /// </summary>
        public static void AuditLoggerExample()
        {
            Console.WriteLine("6. 审计日志示例:");

            try
            {
                // 设置日志记录器
                AuditDecorator.SetLogger(new ConsoleAuditLogger());

                // 初始化审计日志表
                AuditLogQuery.InitializeAuditLogTable("DefaultDb");
                Console.WriteLine("已初始化审计日志表");

                // 带审计的添加操作
                var user = new User
                {
                    Name = "Audit User",
                    Email = "audit@example.com",
                    IsActive = true
                };
                var addResult = AuditDecorator.AddWithAudit(user, "DefaultDb");
                Console.WriteLine(string.Format("带审计的添加: {0}", addResult.IsSuccess));

                // 带审计的更新操作
                var oldUser = new User { Id = 1, Name = "Old Name", Email = "old@example.com", IsActive = false };
                var newUser = new User { Id = 1, Name = "New Name", Email = "new@example.com", IsActive = true };
                var updateResult = AuditDecorator.UpdateWithAudit(oldUser, newUser, "DefaultDb");
                Console.WriteLine(string.Format("带审计的更新: {0}", updateResult.IsSuccess));

                // 带审计的删除操作
                var deleteResult = AuditDecorator.DeleteWithAudit(new User { Id = 1 }, "DefaultDb");
                Console.WriteLine(string.Format("带审计的删除: {0}", deleteResult.IsSuccess));

                // 查询审计日志
                var logs = AuditLogQuery.QueryLogs(
                    startDate: DateTime.Today,
                    action: "INSERT",
                    entityType: "User",
                    dbKey: "DefaultDb"
                );
                Console.WriteLine(string.Format("\n审计日志查询结果: {0} 条记录", logs.Count));
                foreach (var log in logs)
                {
                    Console.WriteLine(string.Format("  [{0}] {1} - {2}", log.Timestamp, log.Action, log.EntityType));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("审计日志操作失败: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// SQL 查询构建器示例
        /// </summary>
        public static void SqlQueryBuilderExample()
        {
            Console.WriteLine("7. SQL 查询构建器示例:");

            try
            {
                // 链式查询构建
                var (sql, parameters) = SqlBuilder.Select()
                    .From("User")
                    .Select("Id, Name, Email")
                    .Where("IsActive = @param0", true)
                    .WhereLike("Name", "%John%")
                    .OrderBy("Id", descending: true)
                    .Skip(0)
                    .Take(10)
                    .Build();

                Console.WriteLine("生成的查询 SQL:");
                Console.WriteLine(sql);
                Console.WriteLine(string.Format("参数: {0}\n", string.Join(", ", parameters)));

                // 插入语句构建
                var (insertSql, insertParams) = SqlBuilder.Insert()
                    .Into("User")
                    .Value("Name", "John")
                    .Value("Email", "john@example.com")
                    .Value("IsActive", true)
                    .Build();

                Console.WriteLine("生成的插入 SQL:");
                Console.WriteLine(insertSql);
                Console.WriteLine(string.Format("参数: {0}\n", string.Join(", ", insertParams)));

                // 更新语句构建
                var (updateSql, updateParams) = SqlBuilder.Update()
                    .Table("User")
                    .Set("Name", "John Doe")
                    .Set("Email", "john.doe@example.com")
                    .Set("IsActive", true)
                    .Where("Id = @param0", 1)
                    .Build();

                Console.WriteLine("生成的更新 SQL:");
                Console.WriteLine(updateSql);
                Console.WriteLine(string.Format("参数: {0}\n", string.Join(", ", updateParams)));

                // 删除语句构建
                var (deleteSql, deleteParams) = SqlBuilder.Delete()
                    .From("User")
                    .Where("Id = @param0", 1)
                    .Build();

                Console.WriteLine("生成的删除 SQL:");
                Console.WriteLine(deleteSql);
                Console.WriteLine(string.Format("参数: {0}\n", string.Join(", ", deleteParams)));

                // 复杂查询构建
                (string complexSql, DbParameter[] complexParams) = SqlBuilder.Select()
                    .From("User u")
                    .InnerJoin("Order o", "u.Id = o.UserId")
                    .Select("u.Name, COUNT(o.Id) as OrderCount")
                    .Where("u.IsActive = @param0", true)
                    .WhereBetween("u.CreateTime", DateTime.Now.AddDays(-30), DateTime.Now)
                    .GroupBy("u.Name")
                    .OrderBy("OrderCount", descending: true)
                    .Skip(0)
                    .Take(20)
                    .Build();

                Console.WriteLine("生成的复杂查询 SQL:");
                Console.WriteLine(complexSql);
                Console.WriteLine(string.Format("参数: {0}\n", string.Join(", ", complexParams)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("SQL 构建失败: {0}\n", ex.Message));
            }
        }
    }

    /// <summary>
    /// 测试用户模型
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
