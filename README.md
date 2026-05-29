# FastData

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-Fast.Data-blue.svg)](https://www.nuget.org/packages/Fast.Data/)

FastData 是一个轻量级多目标框架 ORM，支持 .NET Framework 4.5 / .NET 6.0 / .NET 8.0 / .NET 10.0，提供 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis、消息队列和数据同步。

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
| **分页查询** | ToPagination 简化分页 API |
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
// 查询
var users = FastRead.Query<User>(u => u.IsActive);
var user = FastRead.Query<User>(u => u.Id == 1).FirstOrDefault();

// 分页
var page = FastRead.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPagination<User>(page: 1, pageSize: 10);

// 新增
FastWrite.Add(new User { Name = "张三", IsActive = true });

// 更新
user.Name = "李四";
FastWrite.Update(user);

// 删除
FastWrite.Delete<User>(u => u.Id == 1);
```

---

## 常用 API

### Lambda 查询

```csharp
// 基础查询
var users = FastRead.Query<User>(u => u.Age > 18);

// 链式条件
var result = FastRead.Query<User>(u => u.IsActive)
    .Where(u => u.Age > 18)
    .Or(u => u.Role == "Admin")
    .Like(u => u.UserName, "张%")
    .In(u => u.Department, new[] { "IT", "HR" })
    .OrderBy(u => u.Id)
    .Select(u => new { u.Id, u.UserName })
    .ToList();
```

### 多数据库切换

```csharp
// 指定库
var reports = FastRead.Use("ReportDb").Query<Report>(r => r.Year == 2026);

// 作用域切换
using (FastDb.Use("ArchiveDb"))
{
    var logs = FastRead.Query<Log>(l => l.CreatedTime >= start);
    FastWrite.Add(new ArchiveLog());
}

// Repository 工厂
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();
var defaultRepo = factory.Default();
var reportRepo = factory.Use("ReportDb");
```

### Redis 缓存

```csharp
RedisInfo.Set("user:1", user, 3600);
var cached = RedisInfo.Get<User>("user:1");
RedisInfo.Remove("user:1");

// 缓存穿透防护
var user = RedisInfo.GetOrAdd("user:1", () => db.FindUser(1), hours: 24);
```

### 消息队列

```csharp
var factory = new MessageQueueFactory(redis);

// ReliableQueue（单消费，削峰）
var producer = factory.CreateReliableProducer("fastdata");
producer.Publish("topic:sensor", data);

// Stream（多消费组，解耦）
var streamProducer = factory.CreateStreamProducer("fastdata");
streamProducer.Publish("topic:realtime", data);
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
var users = FastRead.Query<User>("GetActiveUsers", new[]
{
    new SqlParameter("@IsActive", true),
    new SqlParameter("@MinAge", 18)
});
```

### 分表

```csharp
// 时间分表
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

// 链式分表查询
var logs = FastRead.Query<UserLog>(l => l.Level == "Error")
    .UseSharding()
    .WithTimeRange("CreateTime", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
    .ToList();
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
| net45 / net462 | Windows/Linux | ✅ | ✅ 73/73 |
| net6.0 / net6.0-windows | Windows/Linux | ✅ | - |
| net8.0 | Windows/Linux | ✅ | - |
| net10.0 | Windows/Linux | ✅ | ✅ |

### 支持的数据库

| 数据库 | 最低版本 |
|--------|---------|
| SQL Server | 2008 R2+ |
| MySQL | 5.7+ |
| PostgreSQL | 9.6+ |
| Oracle | 11g+ |
| SQLite | 3.x |

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
