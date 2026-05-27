# FastData

[![CI](https://github.com/SamHub1991/FastData/actions/workflows/ci.yml/badge.svg)](https://github.com/SamHub1991/FastData/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/SamHub1991/FastData/blob/master/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-Fast.Data-blue.svg)](https://www.nuget.org/packages/Fast.Data/)

FastData 是一个轻量级 ORM 框架，支持 .NET Framework 4.5 / .NET 6.0 / .NET 8.0 / .NET 10.0 多目标框架，提供 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis 分布式缓存和多数据库连接配置。

## 框架支持

| 框架 | 状态 | 说明 |
|------|------|------|
| .NET Framework 4.5 | ✅ | 使用 NServiceKit.Redis、System.Runtime.Caching |
| .NET 6.0 | ✅ | 使用 NewLife.Redis、Microsoft.Extensions.Caching.Memory |
| .NET 8.0 | ✅ | 使用 NewLife.Redis、Microsoft.Extensions.Caching.Memory |
| .NET 10.0 | ✅ | 使用 NewLife.Redis、Microsoft.Extensions.Caching.Memory |

## 快速安装

```bash
# NuGet Package Manager
Install-Package Fast.Data

# .NET CLI
dotnet add package Fast.Data

# PackageReference
<PackageReference Include="Fast.Data" Version="2.0.0" />
```

## 项目结构

```
FastData/
├── FastData/                          # 核心 ORM 组件（多目标框架）
│   ├── FastRead.cs                    # 查询入口（Lambda/XML SQL）
│   ├── FastWrite.cs                   # 写入入口（INSERT/UPDATE/DELETE）
│   ├── FastDb.cs                      # 数据库上下文切换
│   ├── FastMap.cs                     # XML Map SQL 解析
│   └── Repository/                    # Repository 模式实现
│       ├── IReadRepository.cs         # 读取接口
│       ├── IWriteRepository.cs        # 写入接口
│       ├── IMapRepository.cs          # Map 配置接口
│       └── IFastRepository.cs         # 组合接口
│
├── FastData.Tooling/                  # 公共工具库
│   ├── Database/                      # 数据库适配器
│   ├── CodeGeneration/                # 代码生成器
│   └── Sync/                          # 数据同步服务
│
├── FastRedis/                         # Redis 缓存组件
│   ├── RedisInfo.NewLife.cs           # NewLife.Redis 实现（.NET 6+）
│   ├── RedisInfo.cs                   # NServiceKit.Redis 实现（.NET 4.5）
│   └── Repository/                    # Redis 仓储实现
│
├── FastUntility/                      # 通用工具库
│   └── Base/                          # 日志、Excel、HTTP 等工具类
│
├── FastData.Tests/                    # 单元测试（xUnit）
└── FastData.Example/                  # 使用示例项目
```

## 快速配置

### 数据库配置

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>

<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" 
         Key="DefaultDb" 
         ConnStr="server=.;database=demo;uid=sa;pwd=123456" 
         IsDefault="true" 
         DesignModel="DbFirst" 
         CacheType="web" />
    <Add Provider="MySql" 
         Key="ReportDb" 
         ConnStr="server=127.0.0.1;database=report;uid=root;pwd=123456" 
         DesignModel="DbFirst" 
         CacheType="web" />
  </Connections>
</DataConfig>
```

### Redis 配置（可选）

```xml
<RedisConfig 
  AutoStart="true"
  ReadServerList="127.0.0.1:6379"
  WriteServerList="127.0.0.1:6379"
  MaxReadPoolSize="60"
  MaxWritePoolSize="60" />
```

### 连接字符串加密

```xml
<!-- 设置 IsEncrypt="true"，连接字符串使用 BaseSymmetric.Encrypto() 加密 -->
<Add Provider="SqlServer" 
     Key="SecureDb" 
     ConnStr="加密后的连接字符串" 
     IsEncrypt="true" />
```

```csharp
// 加密连接字符串
var encrypted = BaseSymmetric.Encrypto("server=.;database=demo;uid=sa;pwd=123456");
```

## 使用示例

### 1. 基础 CRUD 操作

```csharp
// 查询
var users = FastRead.Query<User>(a => a.IsEnabled == true);
var user = FastRead.Query<User>(a => a.Id == 1).FirstOrDefault();

// 新增
var newUser = new User { UserName = "张三", Email = "zhangsan@test.com" };
FastWrite.Add(newUser);

// 更新
user.UserName = "李四";
FastWrite.Update(user);

// 删除
FastWrite.Delete<User>(a => a.Id == 1);
```

### 2. Lambda 查询

```csharp
// 条件查询
var activeUsers = FastRead.Query<User>(a => a.IsActive && a.Age > 18);

// 多条件查询
var users = FastRead.Query<User>(a => a.Department == "IT" && a.Salary > 10000);

// 排序
var sortedUsers = FastRead.Query<User>(a => true).OrderByDescending(a => a.CreateTime);

// 分页
var pageResult = FastRead.QueryPage<User>(new PageModel { PageIndex = 1, PageSize = 20 }, a => a.IsActive);

// 聚合
var count = FastRead.Query<User>(a => a.IsActive).Count();
var maxAge = FastRead.Query<User>(a => true).Max(a => a.Age);

// 关联查询
var orders = FastRead.Query<Order, User>((o, u) => o.UserId == u.Id && u.IsActive);
```

### 3. 多数据库切换

```csharp
// 指定库查询
var reports = FastRead.Use("ReportDb").Query<Report>(a => a.Year == 2026);

// 作用域切换（推荐）
using (FastDb.Use("ArchiveDb"))
{
    var logs = FastRead.Query<Log>(a => a.CreatedTime >= beginTime);
    FastWrite.Add(new ArchiveLog());
}

// Repository 工厂模式
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();

var defaultRepository = factory.Default();
var reportRepository = factory.Use("ReportDb");
```

### 4. Repository 模式（分层接口）

```csharp
// 依赖注入
services.AddTransient<IReadRepository, RedisRepository>();
services.AddTransient<IWriteRepository, RedisRepository>();
services.AddTransient<IMapRepository, RedisRepository>();

// 使用示例
public class UserService
{
    private readonly IReadRepository _readRepo;
    private readonly IWriteRepository _writeRepo;
    
    public UserService(IReadRepository readRepo, IWriteRepository writeRepo)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
    }
    
    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _readRepo.QueryAsy<User>("GetActiveUsers", null);
    }
    
    public async Task<WriteReturn> AddUserAsync(User user)
    {
        return await _writeRepo.AddAsy(user);
    }
}
```

### 5. Redis 缓存使用

```csharp
// 基础操作
RedisInfo.Set("user:1", user, 3600);  // 设置，1 小时过期
var cachedUser = RedisInfo.Get<User>("user:1");
RedisInfo.Remove("user:1");

// 泛型操作
RedisInfo.Set("config:app", config, 24);  // 24 小时过期
var config = RedisInfo.Get<AppConfig>("config:app");

// 批量操作
var dic = new Dictionary<string, User>
{
    ["user:1"] = new User { Id = 1, Name = "张三" },
    ["user:2"] = new User { Id = 2, Name = "李四" }
};
RedisInfo.SetDic(dic);
var users = RedisInfo.GetDic<User>(new[] { "user:1", "user:2" });

// 缓存不存在时添加（推荐用于缓存穿透防护）
var user = RedisInfo.GetOrAdd("user:1", () => 
{
    // 从数据库加载
    return _dbContext.Users.Find(1);
}, hours: 24);

// 计数器操作
RedisInfo.Increment("page:views:home", 1);
var views = RedisInfo.Get<int>("page:views:home");
RedisInfo.Decrement("stock:product:1001", 1);

// 过期时间管理
RedisInfo.SetExpire("session:abc123", TimeSpan.FromMinutes(30));
var remaining = RedisInfo.GetExpire("session:abc123");

// 集合操作
var list = RedisInfo.GetList<string>("queue:tasks");
list.Add("task1");
var first = list[0];

var hash = RedisInfo.GetDictionary<User>("users:active");
hash["user:1"] = new User { Id = 1, Name = "张三" };

var set = RedisInfo.GetSet<string>("tags:programming");
set.Add("csharp");
set.Add("dotnet");
```

### 6. Repository 模式使用 Redis

```csharp
// 依赖注入
services.AddSingleton<IRedisRepository, RedisRepository>();

// 使用示例
public class CacheService
{
    private readonly IRedisRepository _redis;
    
    public CacheService(IRedisRepository redis)
    {
        _redis = redis;
    }
    
    public async Task<User> GetUserAsync(int userId)
    {
        var key = $"user:{userId}";
        
        // 尝试从缓存获取
        var user = await _redis.GetAsy<User>(key);
        if (user != null)
            return user;
        
        // 从数据库加载
        user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            // 写入缓存
            await _redis.SetAsy(key, user, hours: 24);
        }
        
        return user;
    }
    
    public async Task RemoveUserAsync(int userId)
    {
        await _redis.RemoveAsy($"user:{userId}");
    }
}
```

### 7. XML Map SQL

```xml
<!-- Maps/User.xml -->
<Map Name="GetActiveUsers" DbType="SqlServer">
  <Sql>
    SELECT * FROM Users 
    WHERE IsActive = @IsActive
    <If Test="@MinAge != null">
      AND Age >= @MinAge
    </If>
    ORDER BY CreateTime DESC
  </Sql>
  <Param>
    <Add Name="@IsActive" Type="Boolean" />
    <Add Name="@MinAge" Type="Int32" Nullable="true" />
  </Param>
</Map>
```

```csharp
// 调用 XML Map SQL
var users = FastRead.Query<User>("GetActiveUsers", new[] 
{
    new SqlParameter("@IsActive", true),
    new SqlParameter("@MinAge", 18)
});
```

### 8. AOP 拦截器

```csharp
public class SqlLogAop : IFastAop
{
    public void Before(BeforeContext context)
    {
        Console.WriteLine($"执行 SQL: {context.Sql}");
        Console.WriteLine($"参数: {string.Join(", ", context.Param.Select(p => $"{p.ParameterName}={p.Value}"))}");
    }
    
    public void After(AfterContext context)
    {
        Console.WriteLine($"执行耗时: {context.Elapsed}ms");
        Console.WriteLine($"影响行数: {context.RowCount}");
    }
    
    public void Exception(ExceptionContext context)
    {
        Console.WriteLine($"SQL 异常: {context.Exception.Message}");
    }
}

// 注册 AOP
FastMap.fastAop = new SqlLogAop();
```

### 9. 数据同步

```csharp
// 配置同步任务
var syncConfig = new TableSyncConfig
{
    TableName = "Users",
    TargetTableName = "Users_Archive",
    PrimaryKeys = new List<string> { "Id" },
    TimeColumnName = "UpdateTime",
    SyncMode = SyncMode.Upsert,
    RangeDays = 7,
    IsEnabled = true
};

// 执行同步
var syncService = new SyncService();
var result = await syncService.SyncTableAsync(syncConfig);

Console.WriteLine($"同步完成: 读取 {result.ReadCount} 行, 写入 {result.WriteCount} 行, 失败 {result.FailedCount} 行");
```

## 构建验证

### 多目标框架构建

```bash
# 构建所有项目
dotnet build FastUntility/FastUntility.csproj
dotnet build FastRedis/FastRedis.csproj
dotnet build FastData.Tooling/FastData.Tooling.csproj
dotnet build FastData/FastData.csproj
dotnet build FastData.Tests/FastData.Tests.csproj

# 运行测试
dotnet test FastData.Tests/FastData.Tests.csproj --framework net10.0
```

### 构建结果

| 项目 | net45 | net6.0 | net8.0 | net10.0 |
|------|-------|--------|--------|---------|
| FastUntility | ✅ | ✅ | ✅ | ✅ |
| FastData.Tooling | ✅ | ✅ | ✅ | ✅ |
| FastData | ✅ | ✅ | ✅ | ✅ |
| FastRedis | ✅ | ✅ | ✅ | ✅ |
| FastData.Tests | ✅ | - | - | ✅ 73/73 |

### 条件编译说明

```csharp
// .NET Framework 4.5 专用代码
#if NETFRAMEWORK
    using System.Runtime.Remoting.Messaging;
    // 使用 CallContext
#endif

// .NET 6.0+ 专用代码
#if NET6_0_OR_GREATER
    using System.Threading;
    // 使用 AsyncLocal
#endif

// 非 .NET Framework 代码
#if !NETFRAMEWORK
    using NewLife.Caching;
    // 使用 NewLife.Redis
#endif
```

## 依赖库版本

| 依赖库 | net45 | net6.0+ | 说明 |
|--------|-------|---------|------|
| Newtonsoft.Json | 13.0.3 | 13.0.3 | JSON 序列化 |
| NPOI | 2.5.6 | 2.7.0 | Excel 操作 |
| NServiceKit.Redis | 1.0.17 | - | Redis 客户端（net45） |
| NewLife.Redis | - | 6.0.2024.1006 | Redis 客户端（net6.0+） |
| System.CodeDom | - | 8.0.0 | 动态编译 |
| Microsoft.Extensions.Caching.Memory | - | 8.0.0 | 内存缓存 |
| System.Configuration.ConfigurationManager | - | 8.0.0 | 配置管理 |

## 文档导航

| 文档 | 说明 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | 版本变更记录 |
| [DEVELOPMENT_PROGRESS.md](DEVELOPMENT_PROGRESS.md) | 开发进度与技术细节 |
| [FastData.Demo/README.md](FastData.Demo/README.md) | Demo 项目说明 |
| [FastData.SyncTool.WinForms/REFACTOR_README.md](FastData.SyncTool.WinForms/REFACTOR_README.md) | 同步工具重构说明 |

## 许可证

MIT License
