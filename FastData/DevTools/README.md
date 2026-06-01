# DevTools 开发工具集 - 功能文档

## 概述

FastData DevTools 是一套完整的开发工具集，为 FastData ORM 提供强大的开发、调试和运维支持。

## 工具列表

FastData DevTools 提供了 22 个专业开发工具，覆盖开发、测试、运维全流程：

### 基础工具（9个）
1. CodeGenerator - 代码生成器
2. DatabaseDiagnostic - 数据库诊断
3. DatabaseComparer - 数据库比较
4. DataImporter - 数据导入导出
5. CacheManager - 缓存管理器
6. AuditLogger - 审计日志
7. SqlQueryBuilder - SQL 构建器
8. PerformanceProfiler - 性能分析器
9. DatabaseBackupRestore - 备份恢复工具

### 高级工具（6个）
10. ConnectionPoolManager - 连接池管理器
11. DistributedTransactionManager - 分布式事务管理器
12. QueryOptimizer - 查询优化器
13. ResultCache - 结果缓存工具
14. ApiTester - API 测试工具
15. DatabaseMonitor - 数据库监控工具

### 企业级工具（7个）
16. DistributedLockManager - 分布式锁管理器
17. ApiClient - API 客户端工具
18. LogAggregator - 日志聚合器
19. EventBus - 事件总线
20. ConfigurationManager - 配置管理器
21. TaskScheduler - 任务调度器
22. DevToolsExamples - 使用示例

---

## 快速索引

### 数据库相关
- [DatabaseDiagnostic](#2-databasediagnostic---数据库诊断工具) - 数据库诊断
- [DatabaseComparer](#3-databasecomparer---数据库比较和同步工具) - 数据库比较
- [DatabaseBackupRestore](#9-databasebackuprestore---数据库备份恢复工具) - 备份恢复
- [ConnectionPoolManager](#10-connectionpoolmanager---连接池管理器) - 连接池管理
- [DatabaseMonitor](#15-databasemonitor---数据库监控工具) - 数据库监控

### 性能优化
- [PerformanceProfiler](#8-performanceprofiler---性能分析器) - 性能分析
- [QueryOptimizer](#12-queryoptimizer---查询优化器) - 查询优化
- [ResultCache](#13-resultcache---结果缓存工具) - 结果缓存

### 数据操作
- [DataImporter](#4-dataimporter---数据导入导出工具) - 数据导入导出
- [SqlQueryBuilder](#7-sqlquerybuilder---sql-查询构建器) - SQL 构建

### 缓存和日志
- [CacheManager](#5-cachemanager---缓存管理器) - 缓存管理
- [AuditLogger](#6-auditlogger---审计日志系统) - 审计日志
- [LogAggregator](#18-logaggregator---日志聚合器) - 日志聚合

### 事务和锁
- [DistributedTransactionManager](#11-distributedtransactionmanager---分布式事务管理器) - 分布式事务
- [DistributedLockManager](#16-distributedlockmanager---分布式锁管理器) - 分布式锁

### API 和测试
- [ApiTester](#14-apitester---api-测试工具) - API 测试
- [ApiClient](#17-apiclient---api-客户端工具) - API 客户端

### 事件和配置
- [EventBus](#19-eventbus---事件总线) - 事件总线
- [ConfigurationManager](#20-configurationmanager---配置管理器) - 配置管理

### 任务调度
- [TaskScheduler](#21-taskscheduler---任务调度器) - 任务调度

### 开发工具
- [CodeGenerator](#1-codegenerator---代码生成器) - 代码生成
- [DevToolsExamples](#22-devtoolsexamples---使用示例) - 使用示例

### 1. CodeGenerator - 代码生成器

**功能**：
- Model 代码生成
- Repository 代码生成
- Service 代码生成
- Controller 代码生成

**使用场景**：
- 快速生成实体模型
- 自动创建分层架构代码
- 减少重复编码工作

**示例**：
```csharp
// 生成 Model
var modelCode = CodeGenerator.GenerateModel<User>();

// 生成 Repository
var repoCode = CodeGenerator.GenerateRepository<User>();

// 生成 Service
var serviceCode = CodeGenerator.GenerateService<User>();

// 生成 Controller
var controllerCode = CodeGenerator.GenerateController<User>();
```

---

### 2. DatabaseDiagnostic - 数据库诊断工具

**功能**：
- 数据库连接测试
- 慢查询分析
- 表结构检查
- 索引使用分析
- 完整诊断报告

**使用场景**：
- 数据库健康检查
- 性能问题诊断
- 表结构验证

**示例**：
```csharp
// 测试连接
var (connected, message) = DatabaseDiagnostic.TestConnection("DefaultDb");

// 分析慢查询
var slowQueries = DatabaseDiagnostic.AnalyzeSlowQueries("DefaultDb", TimeSpan.FromSeconds(1));

// 检查表结构
var issues = DatabaseDiagnostic.CheckTableStructure("DefaultDb", "User");

// 获取诊断报告
var report = DatabaseDiagnostic.GetDiagnosticReport("DefaultDb");
```

---

### 3. DatabaseComparer - 数据库比较和同步工具

**功能**：
- 比较两个数据库差异
- 生成同步 SQL 脚本
- 支持表、列、数据、索引对比

**使用场景**：
- 数据库版本同步
- 开发/测试/生产环境对比
- 数据库迁移验证

**示例**：
```csharp
// 比较数据库
var diff = DatabaseComparer.CompareDatabases("SourceDb", "TargetDb");

// 生成同步脚本
var syncScript = DatabaseComparer.GenerateSyncScript(diff);
Console.WriteLine(syncScript);

// 检查差异
if (diff.TableDifferences.Any())
{
    Console.WriteLine($"表差异: {diff.TableDifferences.Count}");
}
```

---

### 4. DataImporter - 数据导入导出工具

**功能**：
- CSV 格式导入导出
- JSON 格式导入导出
- Excel 格式导出
- 批量导入
- 数据同步（增量更新）

**使用场景**：
- 数据迁移
- 数据备份
- 批量数据导入
- 数据交换

**示例**：
```csharp
// CSV 导入
var (success, failed) = DataImporter.ImportFromCSV<User>("data.csv", "DefaultDb");

// CSV 导出
DataImporter.ExportToCSV<User>("output.csv", u => u.IsActive == true, "DefaultDb");

// 批量导入
var result = DataImporter.BatchImport(users, 1000, "DefaultDb");

// 数据同步
var (inserted, updated, failed) = DataImporter.SyncData(
    users,
    keySelector: u => u.Email,
    dbKey: "DefaultDb"
);

// JSON 导入导出
JsonImporter.ImportFromJson<User>("data.json", "DefaultDb");
JsonImporter.ExportToJson<User>("output.json", u => u.IsActive == true, "DefaultDb");
```

---

### 5. CacheManager - 缓存管理器

**功能**：
- 二级缓存管理
- 查询缓存装饰器
- 缓存自动失效拦截
- 缓存统计信息

**使用场景**：
- 提升查询性能
- 减少数据库访问
- 缓存自动更新

**示例**：
```csharp
// 设置缓存
CacheManager.Set("user:1", user, TimeSpan.FromMinutes(30));

// 获取缓存
var cachedUser = CacheManager.Get<User>("user:1");

// 带缓存的查询
var users = QueryCacheDecorator.QueryWithCache<User>(
    u => u.IsActive == true,
    TimeSpan.FromMinutes(10),
    "DefaultDb"
);

// 缓存自动失效
var result = CacheInvalidationInterceptor.InterceptAdd(newUser, "DefaultDb");

// 获取缓存统计
var stats = CacheManager.GetStats();
Console.WriteLine($"总键数: {stats.TotalKeys}");
```

---

### 6. AuditLogger - 审计日志系统

**功能**：
- 审计日志记录（控制台/数据库）
- 审计装饰器
- 审计日志查询
- 自动记录增删改操作

**使用场景**：
- 数据变更追踪
- 操作审计
- 安全合规

**示例**：
```csharp
// 设置日志记录器
AuditDecorator.SetLogger(new DatabaseAuditLogger("DefaultDb"));

// 初始化审计日志表
AuditLogQuery.InitializeAuditLogTable("DefaultDb");

// 带审计的操作
var result = AuditDecorator.AddWithAudit(user, "DefaultDb");

// 查询审计日志
var logs = AuditLogQuery.QueryLogs(
    startDate: DateTime.Today,
    action: "INSERT",
    entityType: "User"
);
```

---

### 7. SqlQueryBuilder - SQL 查询构建器

**功能**：
- 链式查询构建
- 参数化查询
- 支持 INSERT/UPDATE/DELETE
- 复杂查询构建

**使用场景**：
- 动态 SQL 生成
- 参数化查询
- 避免 SQL 注入

**示例**：
```csharp
// 查询构建
var (sql, parameters) = SqlBuilder.Select()
    .From("User")
    .Where("IsActive = @param0", true)
    .OrderBy("Id")
    .Take(10)
    .Build();

// 插入构建
var (insertSql, insertParams) = SqlBuilder.Insert()
    .Into("User")
    .Value("Name", "John")
    .Build();

// 更新构建
var (updateSql, updateParams) = SqlBuilder.Update()
    .Table("User")
    .Set("Name", "John Doe")
    .Where("Id = @param0", 1)
    .Build();
```

---

### 8. PerformanceProfiler - 性能分析器

**功能**：
- 性能监控
- 性能指标统计
- 性能瓶颈分析
- 查询优化建议
- 索引建议

**使用场景**：
- 性能监控
- 瓶颈识别
- 查询优化
- 索引优化

**示例**：
```csharp
// 性能分析
using (PerformanceProfiler.StartProfiling("Query"))
{
    var users = FastRead.Read.Query<User>(dbKey).ToList();
}

// 获取性能指标
var metric = PerformanceProfiler.GetMetric("Query");
Console.WriteLine($"平均执行时间: {metric.AverageExecutionTime} ms");

// 生成性能报告
var report = PerformanceProfiler.GenerateReport();

// 分析瓶颈
var bottlenecks = PerformanceProfiler.AnalyzeBottlenecks(thresholdSeconds: 1.0);

// 查询优化建议
var suggestions = QueryOptimizer.AnalyzeQuery("SELECT * FROM User", 1500);

// 索引建议
var indexSuggestions = QueryOptimizer.SuggestIndexes("User", frequentQueries);
```

---

### 9. DatabaseBackupRestore - 数据库备份恢复工具

**功能**：
- 数据库备份
- 数据库恢复
- 备份文件管理
- 过期备份清理

**使用场景**：
- 数据备份
- 数据恢复
- 灾难恢复
- 备份管理

**示例**：
```csharp
// 创建备份
var backupResult = DatabaseBackupRestore.CreateBackup(
    "DefaultDb",
    "backup.sql",
    new BackupOptions { IncludeData = true, IncludeSchema = true }
);

// 恢复备份
var restoreResult = DatabaseBackupRestore.RestoreBackup(
    "DefaultDb",
    "backup.sql",
    new RestoreOptions { DropExisting = false }
);

// 获取备份列表
var backups = DatabaseBackupRestore.GetBackupFiles("/backups");

// 删除过期备份
var deletedCount = DatabaseBackupRestore.DeleteExpiredBackups("/backups", 30);
```

---

### 10. ConnectionPoolManager - 连接池管理器

**功能**：
- 数据库连接池管理
- 连接复用
- 连接统计
- 自动清理

**使用场景**：
- 提升连接性能
- 减少连接创建开销
- 监控连接使用情况

**示例**：
```csharp
// 创建连接池
var pool = ConnectionPoolManager.CreatePool("DefaultDb",
    new ConnectionPoolOptions { MinPoolSize = 5, MaxPoolSize = 100 });

// 获取连接
using (var connection = ConnectionPoolManager.GetConnection("DefaultDb"))
{
    var result = connection.Command.ExecuteNonQuery();
}

// 获取连接统计
var stats = ConnectionPoolManager.GetStats("DefaultDb");
Console.WriteLine(stats.ToString());

// 清理连接池
ConnectionPoolManager.Cleanup("DefaultDb");
```

---

### 11. DistributedTransactionManager - 分布式事务管理器

**功能**：
- 跨数据库事务
- 两阶段提交
- 事务作用域
- 异步事务支持

**使用场景**：
- 跨数据库数据一致性
- 分布式事务处理
- 复杂业务流程

**示例**：
```csharp
// 开始分布式事务
var transaction = DistributedTransactionManager.BeginTransaction("Db1", "Db2");

// 在事务中执行操作
var result = transaction.ExecuteInTransaction("Db1", cmd =>
{
    cmd.CommandText = "INSERT INTO User (Name) VALUES ('Test')";
    cmd.ExecuteNonQuery();
    return Result.Success();
});

// 提交或回滚
if (result.IsSuccess)
{
    transaction.Commit();
}
else
{
    transaction.Rollback();
}

// 使用事务作用域
using (var scope = new TransactionScope("Db1", "Db2"))
{
    // 执行操作
    scope.Complete();
}
```

---

### 12. QueryOptimizer - 查询优化器

**功能**：
- SQL 查询优化
- 性能分析
- 索引建议
- 问题检测
- 执行计划建议

**使用场景**：
- 查询性能优化
- 索引规划
- 问题诊断

**示例**：
```csharp
// 优化查询
var optimization = QueryOptimizer.Optimize("SELECT * FROM User WHERE Name LIKE '%John%'");
Console.WriteLine($"优化后 SQL: {optimization.OptimizedSql}");
Console.WriteLine($"预估改进: {optimization.EstimatedImprovement}%");

// 分析性能
var analysis = QueryOptimizer.AnalyzePerformance(sql, executionTimeMs, rowsReturned);
Console.WriteLine($"性能评级: {analysis.Rating}");

// 索引建议
var indexSuggestions = QueryOptimizer.SuggestIndexes(sql, "User");
foreach (var suggestion in indexSuggestions)
{
    Console.WriteLine($"建议为 {suggestion.TableName}.{suggestion.ColumnName} 创建索引");
}

// 检测问题
var issues = QueryOptimizer.DetectIssues(sql);
foreach (var issue in issues)
{
    Console.WriteLine($"问题: {issue.Description} - 建议: {issue.Recommendation}");
}

// 执行计划建议
var plan = QueryOptimizer.SuggestExecutionPlan(sql);
Console.WriteLine($"建议并行度: {plan.RecommendedParallelismLevel}");
```

---

### 13. ResultCache - 结果缓存工具

**功能**：
- 多级缓存
- 缓存装饰器
- 失效策略
- 统计报告

**使用场景**：
- 提升查询性能
- 减少数据库负载
- 智能缓存管理

**示例**：
```csharp
// 设置缓存
ResultCache.Set("users:active", activeUsers, TimeSpan.FromMinutes(30));

// 获取缓存
var users = ResultCache.Get<List<User>>("users:active");

// 获取或创建
var cachedUsers = ResultCache.GetOrCreate("users:active",
    () => FastRead.Read.Query<User>().ToList(),
    TimeSpan.FromMinutes(30));

// 缓存装饰器
var result = CacheDecorator.WithCache("user:1",
    () => GetUserById(1),
    TimeSpan.FromMinutes(30));

// 多级缓存
var result = CacheDecorator.WithMultiLevelCache("user:1",
    () => GetUserById(1),
    l1Expiration: TimeSpan.FromMinutes(5),
    l2Expiration: TimeSpan.FromHours(1));

// 生成缓存报告
var report = ResultCache.GenerateReport();
Console.WriteLine($"命中率: {report.HitRate:P2}");
```

---

### 14. ApiTester - API 测试工具

**功能**：
- API 端点测试
- 负载测试
- 性能对比
- 报告生成

**使用场景**：
- API 测试
- 性能测试
- 压力测试

**示例**：
```csharp
// 测试端点
var result = await ApiTester.TestEndpoint("https://api.example.com/users/1");

// 批量测试
var testCases = new List<ApiTestCase>
{
    new ApiTestCase { Url = "https://api.example.com/users" },
    new ApiTestCase { Url = "https://api.example.com/users/1" }
};
var results = await ApiTester.TestEndpoints(testCases);

// 负载测试
var loadTestResult = await ApiTester.LoadTest(
    "https://api.example.com/users",
    requestCount: 100,
    concurrency: 10
);
Console.WriteLine($"RPS: {loadTestResult.RequestsPerSecond}");

// 生成测试报告
var report = ApiTester.GenerateReport(results);
Console.WriteLine($"成功率: {report.SuccessRate:P2}");
```

---

### 15. DatabaseMonitor - 数据库监控工具

**功能**：
- 实时监控
- 性能分析
- 异常检测
- 监控报告

**使用场景**：
- 数据库监控
- 性能分析
- 异常检测

**示例**：
```csharp
// 开始监控
var session = DatabaseMonitor.StartMonitoring("DefaultDb",
    new MonitoringOptions { SlowQueryThresholdMs = 1000 });

// 记录查询
DatabaseMonitor.RecordQuery(session.Id, "SELECT * FROM User", 150, 100);

// 停止监控
var report = DatabaseMonitor.StopMonitoring(session.Id);
Console.WriteLine($"平均查询时间: {report.AverageQueryDuration:F2} ms");

// 实时监控
var report = await DatabaseMonitor.MonitorRealTime("DefaultDb", TimeSpan.FromMinutes(5));

// 检测异常
var anomalies = DatabaseMonitor.DetectAnomalies(report);
foreach (var anomaly in anomalies)
{
    Console.WriteLine($"异常: {anomaly.Description}");
}
```

---

### 16. DevToolsExamples - 使用示例

**功能**：
- 完整的使用示例
- 所有工具的演示代码
- 可直接运行的示例

**使用场景**：
- 学习工具使用
- 快速上手
- 代码参考

**示例**：
```csharp
// 运行所有示例
DevToolsExamples.RunAllExamples();

// 运行特定示例
DevToolsExamples.CodeGeneratorExample();
DevToolsExamples.DatabaseDiagnosticExample();
// ... 等等
```

---

### 10. DevToolsExamples - 使用示例

**功能**：
- 完整的使用示例
- 所有工具的演示代码
- 可直接运行的示例

**使用场景**：
- 学习工具使用
- 快速上手
- 代码参考

**示例**：
```csharp
// 运行所有示例
DevToolsExamples.RunAllExamples();

// 运行特定示例
DevToolsExamples.CodeGeneratorExample();
DevToolsExamples.DatabaseDiagnosticExample();
// ... 等等
```

---

## 最佳实践

### 1. 性能优化

- 使用 `PerformanceProfiler` 监控关键操作
- 使用 `CacheManager` 缓存频繁查询
- 使用 `QueryOptimizer` 优化慢查询
- 根据索引建议创建合适的索引

### 2. 数据安全

- 定期使用 `DatabaseBackupRestore` 备份数据
- 使用 `AuditLogger` 记录关键操作
- 使用 `DatabaseComparer` 验证数据一致性

### 3. 开发效率

- 使用 `CodeGenerator` 自动生成代码
- 使用 `DataImporter` 快速导入测试数据
- 使用 `SqlQueryBuilder` 安全构建 SQL

### 4. 运维管理

- 使用 `DatabaseDiagnostic` 定期检查数据库健康
- 使用 `PerformanceProfiler` 生成性能报告
- 使用 `CacheManager` 管理缓存

---

## 版本历史

### v1.0.0 (2026-06-01)
- 初始版本
- 包含 10 个核心工具
- 完整的使用示例

---

## 支持

如有问题或建议，请通过以下方式联系：
- GitHub Issues
- 官方文档
- 技术支持邮箱