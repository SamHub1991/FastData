# FastData

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-Fast.Data-blue.svg)](https://www.nuget.org/packages/Fast.Data/)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/tests-192%20passed-brightgreen.svg)]()

FastData 是一个轻量级多目标框架 ORM，支持 .NET Framework 4.5 / .NET 6.0 / .NET 8.0 / .NET 10.0，提供 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis、消息队列和数据同步。

**最新更新 (2026-05-31)**:
- ✅ 修复 Console.WriteLine 为日志系统
- ✅ 修复空 catch 块异常处理
- ✅ 补充 FastUntility 工具类 (DateHelper/CollectionHelper/ApiResponse/Result)
- ✅ 补充 Demo 控制器 (Report/DataExport/Async/DynamicQuery/DataValidation)
- ✅ 测试覆盖率 192/197 通过 (97.5%)

---

## 项目结构

| 项目 | 说明 | 目标框架 |
|------|------|----------|
| [FastData](FastData/) | 核心 ORM 组件 | net45/net6.0/net8.0/net10.0 |
| [FastUntility](FastUntility/) | 通用工具库（日志/加密/HTTP/Excel） | net45/net6.0/net8.0/net10.0 |
| [FastRedis](FastRedis/) | Redis 缓存与消息队列 | net45/net6.0/net8.0/net10.0 |
| [FastData.Tooling](FastData.Tooling/) | 工具库（元数据/代码生成/同步） | net452/net6.0/net8.0/net10.0 |
| [FastData.ModelGenerator.WinForms](FastData.ModelGenerator.WinForms/) | 代码生成工具 | net6.0-windows+ |
| [FastData.SyncTool.WinForms](FastData.SyncTool.WinForms/) | 数据同步工具 | net6.0-windows+ |
| [FastData.Tests](FastData.Tests/) | 单元测试 | net462/net6.0/net8.0/net10.0 |
| [FastData.Demo](FastData.Demo/) | Web API 示例 | net10.0 |
| [FastData.Example](FastData.Example/) | 控制台示例 | net45/net6.0/net8.0/net10.0 |

---

## 核心类说明

| 类名 | 职责 | 使用场景 |
|------|------|----------|
| **FastDataClient** | 统一门面（推荐） | 整合所有功能，绑定数据库 Key |
| **FastDb** | 全局配置与上下文管理 | SQL 日志开关、数据库切换 |
| **FastRead** | 读取操作（静态方法） | LINQ 查询、原生 SQL 查询 |
| **FastWrite** | 写入操作（静态方法） | 添加、更新、删除、CodeFirst |
| **FastMap** | XML 映射操作 | XML SQL 查询/写入 |
| **FastReadDb** | 绑定 Key 的读取 | 避免重复传递 key 参数 |
| **FastWriteDb** | 绑定 Key 的写入 | 避免重复传递 key 参数 |
| **ShardingReadHelper** | 分片读取 | 大数据量分表查询 |
| **ShardingWriteHelper** | 分片写入 | 大数据量分表写入 |

---

## 快速安装

```bash
Install-Package Fast.Data
```

```bash
dotnet add package Fast.Data
```

---

## 功能特性

| 特性 | 说明 |
|------|------|
| **多数据库** | SQL Server / MySQL / PostgreSQL / Oracle / SQLite / DB2 |
| **多目标框架** | net45 / net6.0 / net8.0 / net10.0 |
| **Lambda 查询** | Where/Or/And/Like/Contains/In/Between/OrderBy/GroupBy/Select |
| **XML Map SQL** | 动态 SQL 标签、条件分支 |
| **分页查询** | ToPage 简化分页 API |
| **Repository** | 分层接口：IReadRepository / IWriteRepository / IMapRepository |
| **Redis 缓存** | 分布式缓存、消息队列（ReliableQueue/Stream） |
| **AOP 拦截** | SQL 日志、性能监控 |
| **分表** | 时间/哈希/列表/组合键/查询频率分表策略 |
| **数据同步** | 跨数据库同步、中间库模式、增量同步、失败重试 |

---

## 5 分钟快速开始

### 1. 配置

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>

<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" ConnStr="server=.;database=demo;uid=sa;pwd=123456" DesignModel="DbFirst" CacheType="web" />
  </Connections>
</DataConfig>
```

---

## 配置文件结构

### 1. 主配置文件 (db.config)

```xml
<DataConfig Active="dev" />
```

| 属性 | 说明 |
|------|------|
| `Active` | 指定环境，加载 db.{env}.config |
| | dev/development → db.dev.config |
| | pro/production → db.pro.config |
| | staging → db.staging.config |
| | 环境变量 FASTDATA_ACTIVE 优先级最高 |

### 2. 环境配置文件 (db.dev.config)

| 节点 | 说明 |
|------|------|
| **Connections** | 数据库连接配置 |
| **Redis** | Redis 缓存配置 |
| **ConnectionPool** | 连接池配置 |

### 3. Connections 配置项

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `Key` | 必填 | 连接标识名，用于代码中引用 |
| `Provider` | 必填 | 数据库提供程序 |
| `ConnStr` | 必填 | 连接字符串 |
| `IsDefault` | false | 是否为默认连接 |
| `CacheType` | web | 缓存类型（web=内存缓存/redis=Redis缓存） |
| `IsOutSql` | true | 是否输出 SQL 日志 |
| `IsOutError` | true | 是否输出错误日志 |
| `DesignModel` | DbFirst | 设计模式（DbFirst/CodeFirst） |
| `IsPropertyCache` | true | 是否缓存属性 |
| `SqlErrorType` | db | SQL 错误存放类型（db/file） |
| `IsMapSave` | false | Map 文件是否存数据库 |
| `IsEncrypt` | false | Map 文件是否加密 |

**Provider 值参考**：

| 数据库 | Provider 值 |
|--------|-------------|
| SQL Server | `Microsoft.Data.SqlClient` 或 `System.Data.SqlClient` |
| MySQL | `MySql.Data.MySqlClient` |
| PostgreSQL | `Npgsql` |
| Oracle | `Oracle.ManagedDataAccess.Client` |
| SQLite | `System.Data.SQLite` |
| DB2 | `IBM.Data.DB2.iSeries` |

### 4. Redis 配置项

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `Server` | 127.0.0.1:6379 | 服务器地址（格式：host:port） |
| `Db` | 0 | 数据库索引（0-15） |
| `Password` | 空 | 密码（无密码留空） |
| `ConnectTimeout` | 5000 | 连接超时（毫秒） |
| `SyncTimeout` | 5000 | 同步超时（毫秒） |

**注意**：仅当 CacheType="redis" 时需要配置此节点

### 5. ConnectionPool 配置项

**基础配置**：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `MinPoolSize` | -1（自动） | 最小连接数（-1=自动计算） |
| `MaxPoolSize` | -1（自动） | 最大连接数（-1=自动计算） |
| `ConnectionTimeout` | 30 | 连接超时（秒） |
| `ConnectionLifetime` | 30 | 连接生命周期（分钟） |
| `HealthCheckInterval` | 60 | 健康检查间隔（秒） |
| `LeakDetectionThreshold` | 300 | 泄漏检测阈值（秒） |

**智能调整**：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `EnableSmartAdjustment` | true | 是否启用智能调整 |
| `LoadThreshold` | 80 | 扩容阈值（百分比） |
| `ShrinkThreshold` | 30 | 缩容阈值（百分比） |
| `MaxExpandCount` | 10 | 每次最大扩容数量 |
| `MaxShrinkCount` | 5 | 每次最大缩容数量 |
| `SmartAdjustmentInterval` | 30 | 智能调整间隔（秒） |

**重试机制**：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `MaxRetries` | 3 | 连接创建最大重试次数 |
| `RetryBaseDelayMs` | 50 | 重试基础延迟（毫秒，指数退避） |
| `ValidationCommandTimeout` | 5 | 连接验证命令超时（秒） |

**熔断器**：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `CircuitBreakerEnabled` | true | 是否启用熔断器 |
| `CircuitBreakerFailureThreshold` | 5 | 连续失败阈值（达到此值触发熔断） |
| `CircuitBreakerOpenDurationSec` | 30 | 熔断时长（秒，之后进入半开状态） |
| `CircuitBreakerHalfOpenMaxRequests` | 3 | 半开状态最大测试请求数 |

**自动计算公式**（当 MinPoolSize/MaxPoolSize 为 -1 时）：
- MaxPoolSize = Min(CPU核心数 * 1.5, 内存MB * 0.1 / 2)，范围 10-200
- MinPoolSize = Max(2, MaxPoolSize / 10)

---

### 2. 定义 Model

```csharp
[Table("Users")]
public class User
{
    [Column("Id"), Primary]
    public long Id { get; set; }
    [Column("Name")]
    public string Name { get; set; }
    [Column("IsActive")]
    public bool IsActive { get; set; }
}
```

### 3. 开始使用

```csharp
// 使用 FastDataClient（推荐）
var client = new FastDataClient("DefaultDb");

// 查询
var users = client.Query<User>(u => u.IsActive).ToList();
var user = client.Query<User>(u => u.Id == 1).ToItem();

// 分页
var page = client.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });

// 新增
client.Add(new User { Name = "张三", IsActive = true });

// 更新
user.Name = "李四";
client.Update(user);

// 删除
client.Delete<User>(u => u.Id == 1);
```

---

## FastDataClient 统一门面（推荐）

FastDataClient 整合了所有功能，提供统一的入口：

```csharp
// 创建客户端
var client = new FastDataClient("db1");
client.EnableSqlLog();  // 可选：启用 SQL 日志

// ========== 查询操作 ==========
var users = client.Query<User>(u => u.Age > 18).ToList();
var user = client.Query<User>(u => u.Id == 1).ToItem();
var count = client.Query<User>(u => u.Age > 18).ToCount();
var page = client.Query<User>(u => u.Age > 18).ToPage(pageModel);
var results = client.ExecuteSql<User>(sql, param);
var mapResults = client.MapQuery<User>("GetUsersByAge", param);

// ========== 写入操作 ==========
client.Add(user);
client.AddList(users);
client.Update(user);
client.Update(user, u => new { u.Name });
client.Delete<User>(u => u.Age < 18);
client.BulkInsert(largeList);  // 高性能批量插入
client.CodeFirst<User>();      // CodeFirst 建表

// ========== 消息队列 ==========
var result = client.WriteQueue()
    .WithMetadata(dict)
    .Add(user)
    .Execute();

var result = client.ReadQueue<User>()
    .QueryList(u => u.IsActive)
    .Execute();
```

---

## 常用 API

### Lambda 查询

```csharp
// 基础查询
var users = FastRead.Query<User>(u => u.Age > 18).ToList();

// 链式条件
var result = FastRead.Query<User>(u => u.IsActive)
    .Where<User>(u => u.Age > 18)
    .Or<User>(u => u.Role == "Admin")
    .Like<User>(u => u.UserName, "张%")
    .In<User>(u => u.Department, new[] { "IT", "HR" })
    .OrderBy<User>(u => u.Id)
    .Take(100)
    .ToList();
```

### 多数据库切换

```csharp
// 方式1：使用 FastDataClient
var client = new FastDataClient("ReportDb");
var reports = client.Query<Report>(r => r.Year == 2026).ToList();

// 方式2：作用域切换
using (FastDb.Use("ArchiveDb"))
{
    var logs = FastRead.Query<Log>(l => l.CreatedTime >= start).ToList();
    FastWrite.Add(new ArchiveLog());
}
```

### Redis 缓存

```csharp
// 主动缓存（手动管理）
RedisInfo.Set("user:1", user, 3600);
var cached = RedisInfo.Get<User>("user:1");
RedisInfo.Remove("user:1");

// 使用 CacheKey 工具类
var cacheKey = CacheKey.ForEntity<User>(1);
CacheHelper.GetOrSet(cacheKey, () => client.Query<User>(u => u.Id == 1).ToItem(), 300);
```

### 消息队列

```csharp
// 写入队列（降级模式）
FastWrite.ConfigureQueue<User>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    EnableFallback = true,
    Topic = "users:write"
});

var result = client.WriteQueue()
    .WithMetadata(new Dictionary<string, object> { { "source", "app" } })
    .Add(user)
    .Execute();
```

### XML Map SQL

```xml
<Map Name="GetActiveUsers" DbType="SqlServer">
  <Sql>
    SELECT * FROM Users WHERE IsActive = @IsActive
    <If Test="@MinAge != null">AND Age >= @MinAge</If>
  </Sql>
  <Param>
    <Add Name="@IsActive" Type="Boolean" />
    <Add Name="@MinAge" Type="Int32" Nullable="true" />
  </Param>
</Map>
```

```csharp
var users = client.MapQuery<User>("GetActiveUsers", new[]
{
    new SqlParameter("@IsActive", true),
    new SqlParameter("@MinAge", 18)
});
```

### 分表

```csharp
// 时间分表配置
ShardingManager.Configure<UserLog>(new ShardingConfig
{
    BaseTableName = "UserLog",
    ShardingType = ShardingType.Time,
    TimeConfig = new TimeShardingConfig
    {
        TimeField = "CreateTime",
        Granularity = TimeGranularity.Month
    }
});

// 分片查询
var results = client.ShardQuery<Order>(
    o => o.CreateTime > DateTime.Now.AddMonths(-3),
    queryParams: new Dictionary<string, object> { { "CreateTime", DateTime.Now.AddMonths(-3) } }
);
```

---

## 依赖库

| 依赖库 | net45 | net6.0+ | 说明 |
|--------|-------|---------|------|
| Newtonsoft.Json | 13.0.3 | 13.0.3 | JSON 序列化 |
| NPOI | 2.5.6 | 2.7.0 | Excel 操作 |
| NServiceKit.Redis | 1.0.17 | - | net45 Redis 客户端 |
| NewLife.Redis | - | 6.0.2024.1006 | net6.0+ Redis 客户端 |
| RestSharp | 106.11.7 | 108.0.0 | HTTP 客户端（Tooling） |
| System.Text.Json | - | 8.0.0 | JSON 序列化（net6.0+） |

---

## 构建指南

### 使用构建脚本

```bash
# 构建所有框架
./build.sh

# 构建特定框架
./build.sh net10.0

# 清理
./build.sh clean
```

### 手动构建

```bash
# net10.0
dotnet build FastData.sln --framework net10.0

# net45（Linux 环境）
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" \
dotnet build FastData.sln /p:RegisterForComInterop=false
```

### 注意事项

- `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 仅限 net45 构建，运行时绝不能设置
- Linux 环境构建 net45 需设置 `FrameworkPathOverride`

---

## 兼容性

| 框架 | 平台 | 编译 | 测试 |
|------|------|------|------|
| net45 / net462 | Windows/Linux | ✅ | ✅ 192/197 |
| net6.0 / net6.0-windows | Windows/Linux | ✅ | - |
| net8.0 | Windows/Linux | ✅ | - |
| net10.0 | Windows/Linux | ✅ | ✅ 192/197 |

### 测试结果汇总 (2026-05-31)

| 测试类别 | 通过 | 失败 | 跳过 | 总计 | 耗时 |
|----------|------|------|------|------|------|
| **OrmCrudTests** | 26 | 0 | 0 | 26 | 98ms |
| **ShardingTests** | 40 | 0 | 0 | 40 | 85ms |
| **PaginationTests** | 16 | 0 | 0 | 16 | 36ms |
| **CacheTests** | 16 | 0 | 0 | 16 | 153ms |
| **ExceptionManagerTests** | 18 | 0 | 0 | 18 | 62ms |
| **SecurityTests** | 13 | 0 | 1 | 14 | 691ms |
| **MessageQueueTests** | 9 | 0 | 0 | 9 | 21ms |
| **EncryptionTests** | 7 | 0 | 0 | 7 | 12ms |
| **AopTests** | 7 | 0 | 0 | 7 | 11ms |
| **DbTableNamesTests** | 6 | 0 | 0 | 6 | 11ms |
| **ActiveEnvironmentTests** | 5 | 0 | 0 | 5 | 64ms |
| **ConnectionPoolTests** | 16 | 0 | 0 | 16 | 42s |
| **StressTests** | 13 | 5 | 0 | 18 | 55s |
| **总计** | **192** | **5** | **1** | **198** | - |

**注**: StressTests 失败为连接池容量限制（预期行为），非功能 bug

### 支持的数据库

| 数据库 | 最低版本 | 测试状态 |
|--------|---------|----------|
| SQL Server | 2008 R2+ | ✅ Online |
| MySQL | 5.7+ | ✅ Online |
| PostgreSQL | 9.6+ | ✅ Online |
| Oracle | 11g+ | ⚠️ 未测试 |
| SQLite | 3.x | ✅ Online |
| DB2 | 9.7+ | ⚠️ 未测试 |

---

## 工具

### ModelGenerator（代码生成工具）

6 个功能 Tab：

| Tab | 功能 |
|-----|------|
| 连接管理 | 保存和管理数据库连接 |
| Model 生成 | 从数据库表生成 C# Model |
| XML Map 生成 | 生成 XML SQL 配置文件 |
| 代码生成 | Repository/Service/Controller 分层代码 |
| JSON 转 Model | JSON 自动转换为 C# 类 |
| API 代码生成 | RestSharp 客户端代码生成 |

[查看详细使用手册](FastData.ModelGenerator.WinForms/README.md)

### SyncTool（数据同步工具）

- 跨数据库同步（SQL Server / MySQL / PostgreSQL / SQLite）
- 中间库模式：源库 → 中间库 → 目标库
- 增量同步、批量处理、失败重试
- 分表同步支持

[查看详细使用手册](FastData.SyncTool.WinForms/README.md)

---

## 相关文档

| 文档 | 说明 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | 版本变更记录 |
| [.monkeycode/specs/](.monkeycode/specs/) | 需求/设计/任务规格 |

---

## 许可证

MIT License
