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
│   ├── Repository/                    # Redis 仓储实现
│   ├── Messaging/                     # 消息队列实现
│   │   ├── IMessageProducer.cs        # 生产者接口
│   │   ├── IMessageConsumer.cs        # 消费者接口
│   │   ├── MessageQueueModels.cs      # 队列模型和配置
│   │   ├── ReliableQueueService.cs    # 可信队列实现
│   │   ├── StreamService.cs           # Stream 队列实现
│   │   └── MessageQueueFactory.cs     # 队列工厂
│   └── Services/                      # 集成服务
│       └── MessageQueueIntegrationService.cs
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
var sortedUsers = FastRead.Query<User>(a => true).OrderByDescending<User>(a => a.CreateTime);

// 分页（简化API）
var pageResult = FastRead.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPagination<User>(page: 1, pageSize: 10);
// pageResult.Total, pageResult.TotalPages, pageResult.Data

// 聚合
var count = FastRead.Query<User>(a => a.IsActive).Count();

// 匿名类型投影（Select）
// 只查询需要的字段，减少数据传输
var users = FastRead.Query<User>(u => u.IsActive)
    .Select(u => new { u.Id, u.UserName, u.Email })
    .ToList();
// users 类型为 List<{ int Id, string UserName, string Email }>

// 匿名类型投影 + 分页
// 带过滤条件的分页投影查询
var result = FastRead.Query<User>(u => u.IsActive && u.Age > 18)
    .OrderBy<User>(u => u.Id)
    .Select(u => new { u.Id, u.UserName, u.Department })
    .ToPagination(page: 1, pageSize: 10);
// result.Total = 过滤后的总记录数
// result.TotalPages = 总页数
// result.Data = 当前页的投影数据

// 匿名类型投影 + 条件过滤
var deptUsers = FastRead.Query<User>(u => u.Department == "IT")
    .OrderBy<User>(u => u.UserName)
    .Select(u => new { u.Id, u.UserName, u.Email })
    .ToList();

// 链式 Where/Or 条件（动态查询场景）
// 先定义基础查询，再根据条件追加过滤
var users = FastRead.Query<User>(u => u.IsActive)
    .Where(u => u.Age > 18)           // 不需要 <User>
    .Or(u => u.Role == "Admin")       // 不需要 <User>
    .OrderBy(u => u.Id)
    .ToList();
// 生成 SQL: WHERE IsActive = 1 AND Age > 18 OR Role = 'Admin'

// 链式 And 条件（And 是 Where 的别名）
var users = FastRead.Query<User>(u => u.IsActive)
    .And(u => u.Age > 18)             // 不需要 <User>
    .And(u => u.Department == "IT")   // 不需要 <User>
    .ToList();

// 链式 Like 条件
var users = FastRead.Query<User>()
    .Like(u => u.UserName, "张%")       // LIKE '张%'
    .Contains(u => u.Email, "test")     // LIKE '%test%'
    .StartsWith(u => u.Address, "北京") // LIKE '北京%'
    .EndsWith(u => u.Phone, "8888")     // LIKE '%8888'
    .ToList();

// 链式 In 条件
var users = FastRead.Query<User>()
    .In(u => u.Department, new[] { "IT", "HR", "Finance" })
    .ToList();

// 链式 Between 条件
var users = FastRead.Query<User>()
    .Between(u => u.Age, 18, 65)
    .ToList();

// 组合使用多种链式条件（只需写一次 <User>）
var users = FastRead.Query<User>(u => u.IsActive)
    .And(u => u.Age > 18)
    .Or(u => u.Role == "Admin")
    .Like(u => u.UserName, "张%")
    .In(u => u.Department, new[] { "IT", "HR" })
    .OrderBy(u => u.Id)
    .Select(u => new { u.Id, u.UserName, u.Department })
    .ToList();

// 使用 Where<T> 条件构建器（分开写条件，更清晰）
var where = new Where<User>();
where.Add(u => u.IsActive);
where.And(u => u.Age > 18);
where.Or(u => u.Role == "Admin");
where.Like(u => u.UserName, "张%");
where.In(u => u.Department, new[] { "IT", "HR" });
where.Between(u => u.Age, 18, 65);

var users = FastRead.Query<User>(u => true)
    .Where(where)
    .OrderBy(u => u.Id)
    .ToList();

// 动态条件构建（根据参数决定是否添加条件）
var where = new Where<User>();
where.Add(u => u.IsActive);

if (!string.IsNullOrEmpty(keyword))
    where.Like(u => u.UserName, keyword + "%");

if (minAge > 0)
    where.And(u => u.Age >= minAge);

if (departments != null && departments.Length > 0)
    where.In(u => u.Department, departments.Cast<object>());

var users = FastRead.Query<User>(u => true)
    .Where(where)
    .ToList();
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

### 7. 消息队列（RTU 削峰/多方推送）

基于 NewLife.Redis 实现两种消息队列模式，适用于 RTU 数据上传场景：

| 队列类型 | 适用场景 | 实现方式 | 特点 |
|---------|---------|---------|------|
| **ReliableQueue** | 数据库存储（削峰） | RedisReliableQueue | 单消费、消费确认、消息不丢失 |
| **Stream** | 多方推送（解耦） | RedisStream | 多消费组、广播通知、独立消费 |

#### 配置驱动使用

```csharp
// 配置可信队列（适合数据库存储）
var config = new TableSyncConfig
{
    TableName = "sensor_data",
    EnableMessageQueue = true,
    MessageQueueType = MessageQueueType.ReliableQueue,
    MessageQueueTopic = "rtu:sensor",
    ConsumerConcurrency = 8
};

// 配置 Stream 多消费组（适合多方推送）
var streamConfig = new TableSyncConfig
{
    TableName = "realtime_data",
    EnableMessageQueue = true,
    MessageQueueType = MessageQueueType.Stream,
    MessageQueueTopic = "rtu:realtime",
    ConsumerGroup = "default",
    ConsumerConcurrency = 4
};
```

#### 代码使用示例

```csharp
// 创建工厂
var factory = new MessageQueueFactory(redis);

// 可信队列生产者
var producer = factory.CreateReliableProducer("fastdata");
producer.Publish("rtu:sensor", sensorData);

// 可信队列消费者
var consumer = factory.CreateReliableConsumer("fastdata");
await consumer.ConsumeLoopAsync<SensorData>("rtu:sensor", async (data) =>
{
    // 写入数据库
    await SaveToDatabase(data);
}, cancellationToken, concurrency: 8);

// Stream 多消费组
var streamProducer = factory.CreateStreamProducer("fastdata");
streamProducer.Publish("rtu:realtime", sensorData);

// 多个消费组独立消费
var dbConsumer = factory.CreateStreamConsumer("db-writer", "fastdata");
var alertConsumer = factory.CreateStreamConsumer("alert-system", "fastdata");
```

#### 集成服务使用

```csharp
var mqService = new MessageQueueIntegrationService(redis);

// 发布数据
mqService.PublishData("rtu:sensor", sensorData, MessageQueueType.ReliableQueue);

// 启动消费者
await mqService.StartConsumerAsync<SensorData>("rtu:sensor", async (data) =>
{
    await SaveToDatabase(data);
}, cancellationToken, MessageQueueType.ReliableQueue, concurrency: 8);

// 多消费组
await mqService.StartMultiGroupConsumerAsync("rtu:realtime",
    new[] { "db-writer", "alert-system", "analytics" },
    new Func<SensorData, Task>[]
    {
        async (data) => await SaveToDatabase(data),
        async (data) => { if (data.Temperature > 30) SendAlert(data); },
        async (data) => await AnalyzeData(data)
    }, cancellationToken);
```

### 8. XML Map SQL

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

### 9. 匿名类型支持

#### FastWrite 队列支持匿名类型

```csharp
// 使用匿名类型推送到队列（需要指定表名）
FastWrite.QueueBuilder()
    .Add("SensorData", new { 
        SensorId = "temp-001", 
        Value = 25.5, 
        Timestamp = DateTime.Now 
    })
    .Execute();

// 批量添加匿名类型
var sensors = new[] {
    new { SensorId = "temp-001", Value = 25.5 },
    new { SensorId = "temp-002", Value = 26.0 },
    new { SensorId = "temp-003", Value = 24.8 }
};

FastWrite.QueueBuilder()
    .AddRange("SensorData", sensors)
    .Execute();

// 更新匿名类型
FastWrite.QueueBuilder()
    .Update("SensorData", new { SensorId = "temp-001", Value = 30.0 })
    .Execute();

// 删除匿名类型
FastWrite.QueueBuilder()
    .Delete("SensorData", new { SensorId = "temp-001" })
    .Execute();

// 带元数据的匿名类型
FastWrite.QueueBuilder()
    .AddMetadata("source", "iot-device")
    .AddMetadata("batch", "20260527")
    .Add("SensorData", new { SensorId = "temp-001", Value = 25.5 })
    .Execute();
```

#### FastRead 查询支持匿名类型投影

```csharp
// 只查询需要的字段
var users = FastRead.Query<User>(u => u.IsActive)
    .Select(u => new { u.Id, u.UserName, u.Email })
    .ToList();

// 投影 + 分页
var result = FastRead.Query<User>(u => u.IsActive && u.Age > 18)
    .OrderBy<User>(u => u.Id)
    .Select(u => new { u.Id, u.UserName, u.Department })
    .ToPagination(page: 1, pageSize: 10);
// result.Total = 过滤后的总记录数
// result.TotalPages = 总页数
// result.Data = 当前页的投影数据
```

### 10. 分页查询 API

```csharp
// 基本分页查询（简化API）
// 传入 page 和 pageSize，返回 total、totalPages、data
var result = FastRead.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPagination<User>(page: 1, pageSize: 10);

// 返回结果：
// {
//   "total": 100,
//   "totalPages": 10,
//   "page": 1,
//   "pageSize": 10,
//   "hasPrevious": false,
//   "hasNext": true,
//   "data": [...]
// }

// 使用 PaginationRequest 对象（适合 Web API）
[HttpPost]
public ActionResult<PaginationResult<User>> Search([FromBody] PaginationRequest request)
{
    var result = FastRead.Query<User>(u => u.IsActive)
        .OrderBy<User>(u => u.CreateTime)
        .ToPagination<User>(request);
    return Ok(result);
}

// 异步版本
var result = await FastRead.Query<User>(u => u.Id > 0)
    .OrderBy<User>(u => u.Id)
    .ToPaginationAsync<User>(page: 1, pageSize: 20);

// 带条件的分页查询
var result = FastRead.Query<User>(u => u.Department == "IT")
    .OrderBy<User>(u => u.Id)
    .ToPagination<User>(page: 1, pageSize: 10);

// 返回字典格式（适合动态字段）
var result = FastRead.Query<User>(u => u.Id > 0)
    .OrderBy<User>(u => u.Id)
    .ToPagination(page: 1, pageSize: 10);
// 返回 PaginationResult（非泛型），Data 为 List<Dictionary<string, object>>
```

**PaginationResult 返回字段说明：**

| 字段 | 类型 | 说明 |
|------|------|------|
| Total | int | 总记录数 |
| TotalPages | int | 总页数（自动计算） |
| Page | int | 当前页码 |
| PageSize | int | 每页条数 |
| HasPrevious | bool | 是否有上一页 |
| HasNext | bool | 是否有下一页 |
| Data | List&lt;T&gt; | 数据列表 |

### 10. AOP 拦截器

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

## Model Generator 工具

### XML Map SQL 生成器

Model Generator 内置 XML Map SQL 生成功能，可从数据库表结构自动生成 FastData XML Map SQL。

**支持的 SQL 类型**：
- Select All - 全量查询
- Select By PK - 按主键查询
- Select with Dynamic Conditions - 动态条件查询
- Insert - 插入
- Update - 更新
- Delete - 删除

**使用方式**：
1. 在 Model Generator 中选择数据库表
2. 点击「预览XML」查看生成的 XML Map SQL
3. 点击「生成XML Map」保存为文件

```csharp
// 生成的 XML Map SQL 示例
var sql = XmlMapSqlGenerator.GenerateSelectAllSql("Users", provider);
// 输出: <FastDataSQL Name="Users_SelectAll" SQL="SELECT * FROM Users" />
```

## 9. 分表（数据分片）

FastData 支持多种分表策略，适用于大数据量场景。

### 分表策略

| 策略 | 说明 | 适用场景 |
|------|------|---------|
| **时间分表** | 按时间字段分表（日/周/月/季/年） | 日志、订单、交易记录 |
| **哈希分表** | 按字段哈希值分表 | 用户表、订单表 |
| **列表分表** | 按枚举值分表 | 状态、类型、地区 |
| **组合键分表** | 多字段组合分表 | 区域+类型、年份+月份 |

### 配置示例

```csharp
using FastData.Sharding;

// 时间分表配置
var config = new ShardingConfig
{
    BaseTableName = "UserLog",
    ShardingType = ShardingType.Time,
    TimeConfig = new TimeShardingConfig
    {
        TimeField = "CreateTime",
        Granularity = TimeGranularity.Month,
        StartTime = new DateTime(2026, 1, 1),
        AutoCreateTable = true
    }
};

// 注册配置
ShardingManager.Configure<UserLog>(config);
```

### 哈希分表配置

```csharp
var hashConfig = new ShardingConfig
{
    BaseTableName = "Order",
    ShardingType = ShardingType.Hash,
    HashConfig = new HashShardingConfig
    {
        HashField = "OrderNo",
        ShardCount = 16,
        Algorithm = HashAlgorithm.Consistent
    }
};
ShardingManager.Configure<Order>(hashConfig);
```

### 列表分表配置

```csharp
var listConfig = new ShardingConfig
{
    BaseTableName = "Order",
    ShardingType = ShardingType.List,
    ListConfig = new ListShardingConfig
    {
        ListField = "Region",
        ValueMapping = new Dictionary<string, string>
        {
            { "Beijing", "order_beijing" },
            { "Shanghai", "order_shanghai" },
            { "Guangzhou", "order_guangzhou" }
        }
    }
};
ShardingManager.Configure<Order>(listConfig);
```

### 组合键分表配置

```csharp
var compositeConfig = new ShardingConfig
{
    BaseTableName = "Order",
    ShardingType = ShardingType.Composite,
    CompositeConfig = new CompositeShardingConfig
    {
        CompositeFields = new List<string> { "Region", "CustomerType" },
        UseHash = true,
        ShardCount = 64
    }
};
ShardingManager.Configure<Order>(compositeConfig);
```

### 分表查询

```csharp
// 分表查询
var queryParams = new Dictionary<string, object>
{
    { "CreateTime_Start", new DateTime(2026, 1, 1) },
    { "CreateTime_End", new DateTime(2026, 12, 31) }
};

var logs = ShardingReadHelper.Query<UserLog>(
    log => log.Level == "Error",
    queryParams
);

// 分页查询
var pagedResult = ShardingReadHelper.QueryPage<UserLog>(
    log => log.Level == "Error",
    queryParams,
    pageIndex: 1,
    pageSize: 20
);
```

### 分表写入

```csharp
// 单条写入
var log = new UserLog
{
    Message = "Test",
    Level = "Info",
    CreateTime = DateTime.Now
};
ShardingWriteHelper.Add(log);

// 批量写入
var logs = new List<UserLog> { log1, log2, log3 };
ShardingWriteHelper.AddList(logs);

// 更新
ShardingWriteHelper.Update(
    log,
    l => l.Id == 1,
    l => new { l.Message, l.Level }
);

// 删除
ShardingWriteHelper.Delete<UserLog>(
    l => l.CreateTime < DateTime.Now.AddYears(-1),
    queryParams
);
```

### 自定义分表策略

```csharp
// 实现 IShardingStrategy 接口
public class GeoShardingStrategy : IShardingStrategy
{
    public string Name => "Geo";
    public ShardingType Type => ShardingType.Geo;

    public string GetTableName(ShardingConfig config, object entity)
    {
        // 自定义逻辑
        return $"{config.BaseTableName}_geo";
    }

    public List<string> GetTableNames(ShardingConfig config, Dictionary<string, object> queryParams)
    {
        return new List<string> { $"{config.BaseTableName}_geo" };
    }

    public List<string> GetAllTableNames(ShardingConfig config)
    {
        return new List<string> { $"{config.BaseTableName}_geo" };
    }

    public bool CreateTable(ShardingConfig config, string tableName)
    {
        return true;
    }
}

// 注册自定义策略
ShardingManager.RegisterStrategy(new GeoShardingStrategy());
```

## 示例项目

FastData.Example 包含完整的场景化教程示例：

| 文件 | 示例内容 |
|------|----------|
| BasicCrudExample.cs | 基本 CRUD 操作 |
| LambdaQueryExample.cs | Lambda 查询（Where/OrderBy/GroupBy） |
| RawSqlExample.cs | 原始 SQL 查询 |
| MapSqlExample.cs | XML Map SQL 使用 |
| TransactionExample.cs | 事务使用 |
| MultiDbExample.cs | 多数据库使用 |
| DataSyncExample.cs | 数据同步 |
| MessageQueueExample.cs | 消息队列 |

## 文档导航

| 文档 | 说明 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | 版本变更记录 |
| [DEVELOPMENT_PROGRESS.md](DEVELOPMENT_PROGRESS.md) | 开发进度与技术细节 |
| [FastData.Demo/README.md](FastData.Demo/README.md) | Demo 项目说明 |
| [FastData.SyncTool.WinForms/REFACTOR_README.md](FastData.SyncTool.WinForms/REFACTOR_README.md) | 同步工具重构说明 |

## 许可证

MIT License
