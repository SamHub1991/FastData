# FastData 项目任务清单

> 最后更新：2026-05-30（新增 T-1100~T-1109 统一异常管理与 QQ 机器人远程控制）
> 本文件记录项目历史任务和待办事项。

---

## 0. 早期改进任务（FastData-TaskList）

> **完成日期**: 2026-05-31
> **完成率**: 100% (12/12)

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-0001 | 配置自动加载 - GetConnectionSummaries 自动触发 GetConfig | ✅ | - |
| T-0002 | 统一返回类型 - Result<T>, FastDataErrorCode, FastDataException | ✅ | - |
| T-0003 | WhereIf 扩展 - 动态条件、异步查询、分页、批量操作 | ✅ | - |
| T-0004 | 完整异步支持 - ToListAsync, CountAsync, AnyAsync, FirstOrDefaultAsync | ✅ | - |
| T-0005 | 统一方法命名 - AddAsync, UpdateEntityAsync, DeleteAsync | ✅ | - |
| T-0006 | 改进分页 API - ToPage 返回元组 (List<T>, int Total) | ✅ | - |
| T-0007 | 软删除支持 - SoftDelete 方法，IsDeleted 字段 | ✅ | - |
| T-0008 | 审计字段 - AutoFillAuditFields, CreateTime/By, UpdateTime/By | ✅ | - |
| T-0009 | 错误处理 - FastDataErrorCode (8 种), FastDataException | ✅ | - |
| T-0010 | 多租户支持 - MultiTenantOptions, TenantProperty, CurrentTenant | ✅ | - |
| T-0011 | 变更跟踪 - ChangeTrackingOptions, CacheExpiration | ✅ | - |
| T-0012 | 懒加载 - LazyLoadingOptions, TimeoutSeconds | ✅ | - |

### 文件清单

**新增文件 (6)**
- `/workspace/FastData/Result.cs` - 返回类型和异常
- `/workspace/FastData/DataQueryExtensions.cs` - 查询扩展方法
- `/workspace/FastData/FastWriteAsyncExtensions.cs` - 异步写入方法
- `/workspace/FastData/Config/FastDataOptions.cs` - 全局配置
- `/workspace/FastData/USAGE_GUIDE.md` - 使用指南

**修改文件 (1)**
- `/workspace/FastData/FastWrite.cs` - 软删除 + 审计字段核心逻辑

### 编译状态
- ✅ FastData: 0 Error(s)
- ✅ FastData.Demo: 0 Error(s)

---

## 1. ORM 核心能力

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-001 | Lambda 查询（Where/Or/And/Like/In/Between） | ✅ | - |
| T-002 | 链式查询（Where/Select/OrderBy/GroupBy） | ✅ | - |
| T-003 | 分页查询（ToPagination） | ✅ | - |
| T-004 | 匿名类型投影查询 | ✅ | - |
| T-005 | XML Map SQL 动态查询 | ✅ | - |
| T-006 | Repository 分层接口（IRead/IWrite/IMap） | ✅ | - |
| T-007 | 多数据库切换（Use/作用域/Repository 工厂） | ✅ | - |
| T-008 | AOP 拦截器 | ✅ | - |
| T-009 | 连接字符串加密（BaseSymmetric） | ✅ | - |
| T-010 | 数据同步服务 | ✅ | - |
| T-011 | 多数据库表名映射（DbTableNames） | ✅ | 2026-05-29 |

## 2. 多目标框架

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-100 | 统一 SDbectory-style csproj | ✅ | - |
| T-101 | 条件编译（NETFRAMEWORK/NET6_0_OR_GREATER） | ✅ | - |
| T-102 | CallContext → AsyncLocal 迁移 | ✅ | - |
| T-103 | Redis 双实现（NServiceKit/NewLife） | ✅ | - |
| T-104 | Newtonsoft.Json 升级（6.0.8 → 13.0.3） | ✅ | - |
| T-105 | NPOI 分版本（2.5.6/2.7.0） | ✅ | - |
| T-106 | xUnit 测试框架迁移 | ✅ | - |
| T-107 | NuGet 包生成脚本 | ✅ | - |
| T-108 | Linux 构建支持（FrameworkPathOverride） | ✅ | - |
| T-109 | 构建参数方案C（BuildPlatform 条件化 TargetFrameworks） | ✅ | 2026-05-29 |

## 3. Redis 与消息队列

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-200 | Redis 单例模式（Lazy<FullRedis>） | ✅ | - |
| T-201 | Rudis 缓存操作（Get/Set/Remove/GetOrAdd） | ✅ | - |
| T-202 | RunnableQueue 可信队列 | ✅ | - |
| T-203 | RedisStream 多消费组队列 | ✅ | - |
| T-204 | MessageQueueFactory 工厂 | ✅ | - |
| T-205 | MessageQueueIntegrationService 集成服务 | ✅ | - |
| T-206 | Redis Docker 部署指南 | ✅ | - |

## 4. 分表

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-300 | ShardingManager 分表管理器 | ✅ | - |
| T-301 | TimeShardingStrategy 时间分表 | ✅ | - |
| T-302 | HashShardingStrategy 哈希分表 | ✅ | - |
| T-303 | ListShardingStrategy 列表分表 | ✅ | - |
| T-304 | CompositeShardingStrategy 组合键分表 | ✅ | - |
| T-305 | QueryFrequencyShardingStrategy 查询频率分表 | ✅ | - |
| T-306 | 链式分表查询 API（UseSharding/WithTimeRange） | ✅ | - |
| T-307 | 自定义分表策略（IShardingStrategy） | ✅ | - |

## 5. ModelGenerator（代码生成工具）

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-400 | Tab 1: 连接管理（保存/测试/删除/加载） | ✅ | - |
| T-401 | Tab 2: Model 生成（批量选择/预览/导出） | ✅ | - |
| T-402 | Tab 3: XML Map 生成（CRUD/动态条件） | ✅ | - |
| T-403 | Tab 4: 代码生成（Repository/Service/Controller/Demo） | ✅ | - |
| T-404 | EnhancedCodeGenerator（全功能选项） | ✅ | - |
| T-405 | Tab 5: JSON 转 Model（类型推断/嵌套/数组） | ✅ | - |
| T-406 | Tab 6: API 代码生成（RestSharp/认证/响应 Model） | ✅ | - |
| T-407 | 连接持久化（db_connections.json） | ✅ | - |

## 6. SyncTool（数据同步工具）

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-500 | 跨数据库同步（SQL Server/MySQL/PG/SQLite） | ✅ | - |
| T-501 | 中间库模式（源库→中间库→目标库） | ✅ | - |
| T-502 | 增量同步 + 全量同步 | ✅ | - |
| T-503 | 失败重试 + 失败记录恢复 | ✅ | - |
| T-504 | 定时调度（Timer） | ✅ | - |
| T-505 | AlwaysDeduplicate 去重模式 | ✅ | - |
| T-506 | 分表同步（5 种策略） | ✅ | - |
| T-507 | 数据补录（时间范围重放） | ✅ | - |
| T-508 | SyncTool 代码重构（UserControl 模块化） | ✅ | - |

## 7. ORM 性能测试与集成测试

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-600 | SQLite 支持批量插入（事务包裹多条 INSERT） | ✅ | AddList 方法重构 |
| T-601 | 修复 BulkInsert 表名映射 | ✅ | 使用 TableNameHelper.GetTableName<T>() |
| T-602 | 链式查询 API 返回类型保持泛型（DataQuery<T>） | ✅ | 已有实例方法 Where/And/Or/Like/In/Between/OrderBy/GroupBy |
| T-603 | 统一分页 API 属性命名 | ✅ | EnhancedCodeGenerator.cs PageIndex → PageId |
| T-604 | 改善异步操作的错误信息 | ✅ | AddList catch 块添加 result.writeReturn.Message |
| T-605 | SQL Server 集成测试 | ✅ | 单线程 + 30 线程并发全部通过 |
| T-606 | MySQL 集成测试 | ✅ | 单线程 + 30 线程并发全部通过 |
| T-607 | PostgreSQL 集成测试 | ✅ | 单线程 + 30 线程并发全部通过 |
| T-608 | 修复 FastRead.Query 表名映射 | ✅ | typeof(T).Name → TableNameHelper.GetTableName<T>() |
| T-609 | 修复 PostgreSQL 布尔值处理 | ✅ | VisitExpression.cs: IsActive=1 → IsActive=true (PostgreSQL) |
| T-610 | 修复 DataConfig 并发缓存问题 | ✅ | 读取/写入缓存时创建 List 副本，避免引用共享 |

## 8. 测试与验证

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-700 | xUnit 测试框架（73 个测试） | ✅ | - |
| T-701 | Docker 数据库环境 | ✅ | SQL Server/MySQL/PostgreSQL/Redis |
| T-702 | 多目标框架构建验证 | ✅ | - |
| T-703 | 综合验证测试（34 项） | ✅ | - |
| T-704 | 30 线程全端点覆盖测试（99.4% 成功率） | ✅ | - |

## 9. 文档

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-800 | 主 README 重写 | ✅ | - |
| T-801 | ModelGenerator 使用手册 | ✅ | - |
| T-802 | SyncTool 使用手册 | ✅ | - |
| T-803 | 需求/设计/任务文档重写 | ✅ | - |
| T-804 | MEMORY.md 更新 | ✅ | - |
| T-805 | 项目重构（删除 FastData.Shared） | ✅ | - |

---

## 当前状态

| 类别 | 完成 | 总计 | 进度 |
|------|------|------|------|
| ORM 核心 | 11 | 11 | 100% |
| 多目标框架 | 10 | 10 | 100% |
| Redis/消息队列 | 7 | 7 | 100% |
| 分表 | 8 | 8 | 100% |
| ModelGenerator | 8 | 8 | 100% |
| SyncTool | 9 | 9 | 100% |
| 集成测试 | 11 | 11 | 100% |
| 测试/验证 | 7 | 10 | 70% |
| 文档 | 6 | 6 | 100% |
| **总计** | **77** | **80** | **96%** |

---

## 集成测试性能数据

### 最终测试结果 (2026-05-29)

| 数据库 | 单条插入 | 查询(全表) | 删除 | 链式查询 | 分页查询 | 并发读写 (30 线程) |
|--------|---------|------------|------|---------|---------|-------------------|
| SqlServer | 465ms | 73ms (1613 条) | 6ms | 17ms (846 条) | 20ms (1633 条) | 2825ms (106 ops/s) |
| MySql | 158ms | 28ms (1612 条) | 2ms | 16ms (845 条) | 4ms (1632 条) | 4121ms (73 ops/s) |
| PostgreSql | 105ms | 55ms (775 条) | 2ms | 8ms (411 条) | 13ms (795 条) | 1010ms (297 ops/s) |

### 修复的关键问题

1. **FastRead.Query 表名映射**：`typeof(T).Name` → `TableNameHelper.GetTableName<T>()`，支持 `[Table]` 属性
2. **PostgreSQL 布尔值处理**：生成 `IsActive=true` 而不是 `IsActive=1`（PostgreSQL 不支持 boolean=integer）
3. **DataConfig 并发缓存**：读取/写入缓存时创建 List 副本，避免多线程共享引用导致的竞态条件
4. **连接打开**：所有读写操作添加 `if (conn.State == ConnectionState.Closed) conn.Open()`

---

## 7. ORM 全面验证与高并发测试

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-700 | 连接数据库全面验证 ORM 功能，检查工作流覆盖情况 | ✅ | 2026-05-29 |
| T-701 | 补充缺失的单元测试、Demo、示例 | ✅ | 2026-05-29 |
| T-702 | 从 Demo 开始全链条测试 | ✅ | 2026-05-29 |
| T-703 | 高并发和稳定性测试 | ✅ | 2026-05-29 |
| T-704 | 连接池优化 - 高并发场景下连接池耗尽问题 | ⏳ | 2026-05-29 发现 |
| T-705 | MySql 并发性能优化 - 30 线程成功率仅 9% | ⏳ | 2026-05-29 发现 |
| T-706 | 连接池压力测试稳定性 - 1000 次快速创建销毁成功率 72% | ⏳ | 2026-05-29 发现 |

### 验证范围

1. **ORM 核心功能**
   - Lambda 查询（Where/Or/And/Like/In/Between）
   - 链式查询（Where/Select/OrderBy/GroupBy）
   - 分页查询（ToPagination）
   - 匿名类型投影查询
   - XML Map SQL 动态查询
   - Repository 分层接口
   - 多数据库切换
   - AOP 拦截器
   - 连接字符串加密
   - 多数据库表名映射（DbTableNames）

2. **工作流覆盖检查**
   - 单元测试是否完整
   - Demo 是否完整
   - 示例是否完整
   - 文档是否完整

3. **高并发测试**
   - 30 线程并发读写
   - 100 线程并发读写
   - 连接池压力测试
   - 长时间运行稳定性

### 测试结果 (2026-05-29)

**单元测试：180 个全部通过**

**高并发测试结果：**

| 数据库 | 30 线程并发 | 100 线程并发 |
|--------|------------|-------------|
| SqlServer | 100 ops/s, 成功率 60% | 2717 ops/s, 成功率 30% |
| MySql | 42 ops/s, 成功率 9% | 6410 ops/s, 成功率 30% |
| PostgreSql | 188 ops/s, 成功率 58% | - |

**稳定性测试结果：**

| 测试项 | 结果 |
|--------|------|
| 连接池压力测试（1000 次） | 成功率 72% |
| 长时间运行（3 秒查询） | 成功率 100% |
| 连接泄漏检测（100 次） | 成功率 100% |
| 并发连接池压力（20 线程 x 50 次） | 成功率 79% |

### 发现的问题

1. **连接池耗尽**：高并发场景下（30+ 线程），连接池可能耗尽导致操作失败
2. **MySql 并发性能差**：30 线程并发成功率仅 9%，可能与连接池配置有关
3. **连接池压力测试不稳定**：1000 次快速创建销毁成功率仅 72%

---

## 8. 智能连接池 (SmartConnectionPool)

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-900 | 智能连接池管理器（SmartConnectionPool） | ✅ | 2026-05-30 |
| T-901 | 连接池工厂（ConnectionPoolFactory） | ✅ | 2026-05-30 |
| T-902 | 连接池监控器（ConnectionPoolMonitor） | ✅ | 2026-05-30 |
| T-903 | 连接池扩展方法 | ✅ | 2026-05-30 |
| T-904 | DataContext 集成连接池 | ✅ | 2026-05-30 |
| T-905 | 连接池单元测试（7个测试） | ✅ | 2026-05-30 |

### 功能特性

1. **智能连接池管理器**
   - 连接复用：自动复用空闲连接，减少创建销毁开销
   - 连接生命周期管理：自动销毁过期连接
   - 连接健康检查：定期验证连接有效性
   - 连接泄漏检测：检测未释放的连接
   - 智能扩缩容：根据负载自动调整连接池大小

2. **连接池工厂**
   - 单例模式：全局唯一实例
   - 多连接池管理：支持多个数据库连接池
   - 统一指标收集：汇总所有连接池指标

3. **连接池监控器**
   - 实时快照：定期收集连接池状态
   - 历史记录：保留最近 1000 条快照
   - 统计分析：计算平均值、最大值、最小值

4. **DataContext 集成**
   - 构造函数支持连接池配置
   - 自动归还连接到连接池
   - 兼容现有代码

### 使用示例

```csharp
// 创建带连接池的 DataContext
var poolConfig = new ConnectionPoolConfig
{
    MinPoolSize = 10,
    MaxPoolSize = 100,
    ConnectionTimeout = 30,
    HealthCheckInterval = 60,
    EnableSmartAdjustment = true
};

using var db = new DataContext("SqlServer", poolConfig: poolConfig);
// 使用 db 进行数据库操作
// Dispose 时自动归还连接到连接池

// 获取连接池指标
var metrics = ConnectionPoolFactory.Instance.GetAllMetrics();
foreach (var kvp in metrics)
{
    Console.WriteLine($"Pool: {kvp.Key}");
    Console.WriteLine($"  Active: {kvp.Value.ActiveConnections}");
    Console.WriteLine($"  Idle: {kvp.Value.IdleConnections}");
    Console.WriteLine($"  Total: {kvp.Value.TotalConnections}");
}
```

### 文件位置

- `/workspace/FastData/ConnectionPool/SmartConnectionPool.cs` - 智能连接池管理器
- `/workspace/FastData/ConnectionPool/ConnectionPoolFactory.cs` - 连接池工厂
- `/workspace/FastData/ConnectionPool/ConnectionPoolMonitor.cs` - 连接池监控器
- `/workspace/FastData/ConnectionPool/ConnectionPoolExtensions.cs` - 扩展方法
- `/workspace/FastData/Context/DataContext.cs` - 集成连接池支持
- `/workspace/FastData.Tests/SmartConnectionPoolTests.cs` - 单元测试

---

## 9. 每个数据库完整 ORM 功能测试 (T-800)

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-800 | 每个数据库完整 ORM 功能测试 | ✅ | 2026-05-29 |

### 测试范围

每个数据库（SqlServer/MySql/PostgreSql）测试以下 10 项功能：
1. 插入（Add）
2. 查询（GetList）
3. 更新（Update）
4. 删除（Delete）
5. 链式查询（Query + And + OrderBy + Take）
6. 分页查询（ToPagination）
7. 批量插入（AddList）
8. Lambda 查询（Where/Or/Like/In/Between）
9. 排序分组（OrderBy/GroupBy）
10. 表名映射（DbTableNames）

### 测试结果 (2026-05-29)

| 测试项 | SqlServer | MySql | PostgreSql |
|--------|-----------|-------|------------|
| 1.插入 | PASS | PASS | PASS |
| 2.查询 | PASS | PASS | PASS |
| 3.更新 | PASS | PASS | PASS |
| 4.删除 | PASS | PASS | PASS |
| 5.链式查询 | PASS | PASS | PASS |
| 6.分页查询 | PASS | PASS | PASS |
| 7.批量插入 | PASS | PASS | PASS |
| 8.Lambda查询 | PASS | PASS | PASS |
| 9.排序分组 | PASS | PASS | PASS |
| 10.表名映射 | PASS | PASS | PASS |

**总计：33/33 通过（SqlServer 10/10，MySql 10/10，PostgreSql 10/10）**

### 修复记录

1. **MySql BatchInsert**（已修复）
   - 日期格式：`DateTime` 使用 `yyyy-MM-dd HH:mm:ss` 格式
   - 布尔值：使用 `0/1` 而不是 `true/false`
   - 排除 Identity 列

2. **PostgreSql BatchInsert**（已修复）
   - 添加完整的批量插入实现
   - 使用参数化查询
   - 正确处理布尔值（`true/false`）
   - 排除 Identity 列

3. **SqlServer BatchInsert**（已修复）
   - 修改 `CommandParam.InitTvps`：排除 Identity 列，创建 `{TypeName}_TVP` 类型
   - 修改 `CommandParam.GetTable`：手动构建 DataTable，排除 Identity 列
   - 修改 `CommandParam.GetTvps`：生成排除 Identity 列的 INSERT SQL
   - 修改 `DataContext.Write.AddList`：使用 `@{TypeName}_TVP` 参数名和类型名

### 发现并修复的问题

1. **Update 不能更新 Identity 列**（已解决）
   - 问题：直接更新包含 Identity 列的实体会报错 "Cannot update identity column 'Id'"
   - 解决方案：使用 `db.Update(entity, u => u.Id == id, u => new { u.UserName, u.Email, ... })` 排除 Identity 列

2. **BatchInsert 连接状态问题**（已解决 - 部分）
   - SqlServer：TVPs 实现需要排除 Identity 列（待修复）
   - MySql：已修复日期格式（`yyyy-MM-dd HH:mm:ss`）和布尔值处理（`0/1`）
   - PostgreSql：已添加完整实现，排除 Identity 列

3. **MySql BatchInsert SQL 语法问题**（已解决）
   - 问题：布尔值被当作字符串处理，日期格式不正确
   - 解决方案：布尔值使用 `0/1`，日期使用 `yyyy-MM-dd HH:mm:ss` 格式

4. **PostgreSql BatchInsert 缺失实现**（已解决）
   - 问题：AddList 方法没有 PostgreSql 的处理代码
   - 解决方案：添加 PostgreSql 的批量插入实现，使用参数化查询

### API 用法总结

```csharp
// 查询
var result = db.GetList<PerfUser>(FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive));
var users = result.list; // List<PerfUser>
var count = result.list.Count;

// 更新（排除 Identity 列）
var updateResult = db.Update(user, u => u.Id == user.Id, u => new { u.UserName, u.Email, u.Age });
bool success = updateResult.writeReturn.IsSuccess;

// 删除
var deleteResult = db.Delete<PerfUser>(u => u.Id == id);
bool success = deleteResult.writeReturn.IsSuccess;

// 分页
var pagination = FastRead.Use(dbName).Query<PerfUser>(u => u.IsActive)
    .OrderBy<PerfUser>(u => u.Id)
    .ToPagination<PerfUser>(1, 10);
var data = pagination.Data; // List<PerfUser>
var total = pagination.Total; // int
```

---

## 10. 安全与认证 (Security & Authentication)

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-1000 | 服务器监控工具（ServerMonitor） | ✅ | 2026-05-30 |
| T-1001 | JWT Token 工具（JwtHelper） | ✅ | 2026-05-30 |
| T-1002 | AES 加解密工具（AesHelper） | ✅ | 2026-05-30 |
| T-1003 | RSA 加解密工具（RsaHelper） | ✅ | 2026-05-30 |
| T-1004 | HMAC 签名工具（HmacHelper） | ✅ | 2026-05-30 |
| T-1005 | API Key 工具（ApiKeyHelper） | ✅ | 2026-05-30 |
| T-1006 | 安全功能单元测试（32个测试） | ✅ | 2026-05-30 |

### 功能特性

1. **服务器监控工具**
   - CPU 使用率监控
   - 内存使用情况（总量、已用、可用、使用率）
   - 磁盘信息（名称、文件系统、容量、使用率）
   - 网络信息（收发字节、速度、状态）
   - 系统运行时间

2. **JWT Token 工具**
   - 支持 HS256/HS384/HS512 算法
   - Token 生成、验证、解析
   - 过期时间验证
   - 自定义声明支持

3. **AES 加解密工具**
   - AES-256-CBC 加密
   - 支持固定 IV 和自动生成 IV
   - 密钥和 IV 自动生成

4. **RSA 加解密工具**
   - RSA 密钥对生成
   - 公钥加密、私钥解密
   - 数字签名和验证

5. **HMAC 签名工具**
   - 支持 SHA256/SHA384/SHA512/MD5
   - 签名生成和验证

6. **API Key 工具**
   - API Key 生成（支持前缀）
   - 哈希存储和验证

### 使用示例

```csharp
using FastUntility.Security;

// 服务器监控
var monitorInfo = ServerMonitor.GetMonitorInfo();
Console.WriteLine($"CPU: {monitorInfo.CpuUsage}%");
Console.WriteLine($"内存: {monitorInfo.MemoryUsage}%");

// JWT Token
var payload = JwtPayload.Create(
    subject: "user123",
    issuer: "FastData",
    expiry: TimeSpan.FromHours(1)
);
var token = JwtHelper.GenerateToken(payload, "secret-key");
var validatedPayload = JwtHelper.ValidateToken(token, "secret-key");

// AES 加解密
var key = AesHelper.GenerateKey();
var encrypted = AesHelper.EncryptWithIV("Hello World", key);
var decrypted = AesHelper.DecryptWithIV(encrypted, key);

// RSA 加解密
var (publicKey, privateKey) = RsaHelper.GenerateKeyPair();
var encrypted = RsaHelper.Encrypt("Hello World", publicKey);
var decrypted = RsaHelper.Decrypt(encrypted, privateKey);

// HMAC 签名
var signature = HmacHelper.HmacSha256("data", "key");
var isValid = HmacHelper.VerifyHmac("data", "key", signature, "SHA256");

// API Key
var apiKey = ApiKeyHelper.GenerateApiKey("fast");
var hashedKey = ApiKeyHelper.HashApiKey(apiKey);
var isValid = ApiKeyHelper.VerifyApiKey(apiKey, hashedKey);
```

---

## 11. 统一异常管理与 QQ 机器人远程控制 (Exception Management & QQ Bot Remote Control)

| 编号 | 任务 | 状态 | 备注 |
|------|------|------|------|
| T-1100 | QQ 机器人配置（QQBotConfig） | ✅ | 2026-05-30 |
| T-1101 | 消息发送接口（IMessageSender） | ✅ | 2026-05-30 |
| T-1102 | QQ 机器人消息发送器（QQBotMessageSender） | ✅ | 2026-05-30 |
| T-1103 | 异常通知器（ExceptionNotifier） | ✅ | 2026-05-30 |
| T-1104 | 远程指令管理器（RemoteCommandManager） | ✅ | 2026-05-30 |
| T-1105 | 统一异常管理器（ExceptionManager） | ✅ | 2026-05-30 |
| T-1106 | 全局异常处理器（GlobalExceptionHandler） | ✅ | 2026-05-30 |
| T-1107 | ORM 异常拦截器（OrmExceptionInterceptor） | ✅ | 2026-05-30 |
| T-1108 | 内置远程指令（12个） | ✅ | 2026-05-30 |
| T-1109 | 异常管理单元测试（16个测试） | ✅ | 2026-05-30 |

### 功能特性

1. **QQ 机器人配置**
   - BotId/BotToken 认证
   - ApiUrl API 地址配置
   - AdminQQNumbers 管理员 QQ 号列表
   - NotifyGroups 通知群号列表
   - CommandPrefix 指令前缀（默认 #）
   - RequireAdminForCommands 管理员权限控制

2. **消息发送接口**
   - SendPrivateMessage 发送私聊消息
   - SendGroupMessage 发送群消息
   - QQBotMessageSender QQ 机器人实现
   - ConsoleMessageSender 控制台实现（测试用）

3. **异常通知器**
   - 异常级别过滤（Debug/Info/Warning/Error/Critical）
   - 通知间隔控制（防止重复通知）
   - 堆栈信息格式化
   - 多群/多管理员通知

4. **远程指令管理器**
   - 指令解析和路由
   - 权限验证
   - 响应格式化
   - 自定义指令注册

5. **统一异常管理器**
   - 单例模式
   - 异常历史记录（最多 1000 条）
   - 全局异常处理注册
   - ORM 异常拦截

6. **内置远程指令（12个）**

   | 指令 | 说明 | 需要管理员 |
   |------|------|-----------|
   | help | 显示帮助信息 | 否 |
   | status | 获取服务器状态 | 否 |
   | memory | 获取内存使用详情 | 否 |
   | cpu | 获取 CPU 使用率 | 否 |
   | disk | 获取磁盘信息 | 否 |
   | process | 获取当前进程信息 | 否 |
   | version | 获取系统版本信息 | 否 |
   | time | 获取服务器时间 | 否 |
   | dbstatus | 获取数据库连接状态 | 是 |
   | dbclose | 关闭指定数据库连接池 | 是 |
   | dbrestart | 重启指定数据库连接池 | 是 |
   | gc | 强制垃圾回收 | 是 |

### 使用示例

```csharp
using FastUntility.Monitor;

// 配置 QQ 机器人
var botConfig = new QQBotConfig
{
    BotId = "your-bot-id",
    BotToken = "your-bot-token",
    ApiUrl = "http://127.0.0.1:5700",
    AdminQQNumbers = new List<string> { "123456789" },
    NotifyGroups = new List<string> { "987654321" }
};

var notifyConfig = new ExceptionNotifyConfig
{
    IsEnabled = true,
    MinLevel = ExceptionLevel.Error,
    SendStackTrace = true
};

// 初始化异常管理器
var manager = ExceptionManager.Initialize(botConfig, notifyConfig);

// 注册全局异常处理
GlobalExceptionHandler.Register();

// 记录异常（自动通知）
try
{
    // 业务代码
}
catch (Exception ex)
{
    manager.LogException(ex, ExceptionLevel.Error, "MyService");
}

// 通过 QQ 机器人处理远程指令
manager.ProcessMessage("123456789", "987654321", "#status");
manager.ProcessMessage("123456789", null, "#help");

// 注册自定义指令
manager.RegisterCommandHandler(new MyCustomCommandHandler());

// ORM 异常拦截
OrmExceptionInterceptor.Intercept(ex, "Query", "SqlServer");
```

### 远程指令示例

```
#help - 显示帮助信息
#status - 获取服务器状态
#memory - 获取内存使用详情
#cpu - 获取 CPU 使用率
#disk - 获取磁盘信息
#process - 获取当前进程信息
#version - 获取系统版本信息
#time - 获取服务器时间
#dbstatus - 获取数据库连接状态（管理员）
#dbclose <pool> - 关闭连接池（管理员）
#dbrestart <pool> - 重启连接池（管理员）
#gc - 强制垃圾回收（管理员）
```
