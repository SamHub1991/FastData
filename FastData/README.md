# FastData

FastData 是一个轻量级多目标框架 ORM，支持 .NET Framework 4.5.2 / .NET 8.0 / .NET 10.0。

## 目标框架

| 框架 | 说明 |
|------|------|
| `net452` | .NET Framework 4.5.2 |
| `net8.0` / `net10.0` | Modern .NET |

## 安装

```bash
dotnet add package FastData
```

## 支持的数据库

| 数据库 | Provider |
|--------|----------|
| SQL Server | `System.Data.SqlClient` |
| MySQL | `MySql.Data.MySqlClient` |
| Oracle | `Oracle.ManagedDataAccess.Client` |
| SQLite | `System.Data.SQLite` |
| DB2 | `IBM.Data.DB2.iSeries` |
| PostgreSQL | `Npgsql` |

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
| **ConditionBuilder** | 条件动态拼接 | 安全的对象化 WHERE 条件构建，详见 [docs/CONDITION_BUILDER.md](./docs/CONDITION_BUILDER.md) |

## 进阶文档

| 文档 | 说明 |
|------|------|
| **[ORM 最佳实践](./docs/ORM_BEST_PRACTICES.md)** | 推荐 API 用法、过时 API 迁移指南 |
| [条件动态拼接（Condition / ConditionBuilder）](./docs/CONDITION_BUILDER.md) | 17 种操作符的对象化拼接，参数化防 SQL 注入，可扩展 |
| [数据库提供程序自动注册机制](./docs/AUTO_PROVIDER_REGISTRATION.md) | .NET Core/.NET 5+ 环境下 Provider 自动扫描注册 |

## 快速开始

### 配置

创建 `db.config`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" ConnStr="Server=.;Database=TestDb;Trusted_Connection=true;" />
  </Connections>
</DataConfig>
```

**简化配置**：连接节点只需填写 `Key`、`Provider`、`ConnStr`，其他属性（`CacheType`、`IsOutSql`、`IsOutError` 等）均有合理默认值，按需覆盖即可。

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
| `CacheType` | web | 缓存类型（web=内存缓存/redis=Redis 缓存） |
| `IsOutSql` | true | 是否输出 SQL 日志 |
| `IsOutError` | true | 是否输出错误日志 |
| `DesignModel` | DbFirst | 设计模式（DbFirst/CodeFirst） |
| `IsPropertyCache` | true | 是否缓存属性 |
| `SqlErrorType` | db | SQL 错误存放类型（db/file） |
| `IsMapSave` | false | Map 文件是否存数据库 |
| `IsEncrypt` | false | Map 文件是否加密 |

**提示**：实际配置中只需填写 `Key`、`Provider`、`ConnStr`，其他属性使用默认值即可，按需覆盖。

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

### 快速开始

```csharp
// 初始化（程序启动时调用一次）
FastMap.InstanceMap();
FastMap.InstanceTable();
FastMap.InstanceProperties(nameSpace: "YourApp.Models");
FastMap.InstanceCheck(nameSpace: "YourApp.Models");

// 使用 FastDataClient（推荐）
var db = new FastDataClient("DefaultDb");

// 链式查询
var users = db.Query<User>(u => u.IsActive)
    .Where<User>(u => u.Age > 18)
    .OrderBy<User>(u => u.CreateTime)
    .ToList();

// 新增
db.Add(new User { Name = "张三", Age = 30 });

// 更新
db.Update(new User { Id = 1, Name = "李四" });
db.Update(new User { Name = "新名字" }, u => u.Id == 1);

// 删除
db.Delete<User>(u => u.Id == 1);
```

---

### 基础 CRUD

```csharp
// 使用 FastDataClient（推荐）
var client = new FastDataClient("DefaultDb");

// 查询
var users = client.Query<User>(u => u.IsActive).ToList();
var user = client.Query<User>(u => u.Id == 1).ToItem();

// 新增
client.Add(new User { Name = "张三", Age = 30 });

// 更新
user.Name = "李四";
client.Update(user);

// 删除
client.Delete<User>(u => u.Id == 1);
```

### CodeFirst 建表（多数据库兼容）

```csharp
// 根据实体类创建表结构，自动适配 SQL Server/MySQL/PostgreSQL/Oracle/SQLite 方言
FastWrite.CodeFirst<User>(key: "DefaultDb", isDropExists: false);

// 异步版本
await FastWrite.CodeFirstAsync<User>(key: "DefaultDb", isDropExists: false);
```

### Lambda 查询

```csharp
// 链式查询
var result = client.Query<Order>(o => o.UserId == 1)
    .Where<Order>(o => o.Total > 100)
    .OrderBy<Order>(o => o.CreateTime)
    .Take(20)
    .ToList();

// 分页查询
var page = client.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });
```

### 条件动态拼接（ConditionBuilder）

面向"配置驱动 / 规则化查询"场景，基于对象化方式构建 WHERE 子句，
**所有值走参数化**，天然防 SQL 注入。完整文档见 [docs/CONDITION_BUILDER.md](./docs/CONDITION_BUILDER.md)，
这里给出最小示例：

```csharp
using FastData.Base;

var config = FastDataConfig.GetConfig("DefaultDb");

// 链式构建：Age >= 18 AND Name 含"张" OR Status IN (1,2,3)
var sql = new ConditionBuilder(config)
    .Equal<User>(u => u.Age, 18)
    .And()
    .Contains<User>(u => u.Name, "张")
    .Or()
    .In<User>(u => u.Status, new object[] { 1, 2, 3 })
    .Build(out var parameters);

// sql ≈ "Age = @p0 AND Name LIKE @p1 OR Status IN (@p2,@p3,@p4)"
// parameters : 5 个 DbParameter，按出现顺序填入
```

**支持的操作符**：`Equal` / `NotEqual` / `GreaterThan` / `GreaterThanOrEqual` /
`LessThan` / `LessThanOrEqual` / `Like` / `NotLike` / `Contains` / `StartsWith` /
`EndsWith` / `In` / `NotIn` / `Between` / `NotBetween` / `IsNull` / `IsNotNull`（17 种）。

> **安全提示**：永远不要手工拼接带用户输入的 SQL 字符串。所有用户输入值必须走
> `ConditionBuilder` / `Condition` 体系或显式 `DbParameter` 绑定。

### XML Map SQL

```csharp
// 加载 XML 映射
FastMap.InstanceMap(dbKey: "DefaultDb", mapFile: "SqlMap.config");

// 执行映射查询
var users = client.MapQuery<User>("GetActiveUsers", new[]
{
    new SqlParameter("@IsActive", true),
    new SqlParameter("@MinAge", 18)
});
```

### 分表

```csharp
// 配置分表
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

// 分片写入会自动路由到物理分表
client.ShardAdd(order);
client.ShardUpdate(
    new Order { OrderNo = order.OrderNo, Status = "paid" },
    o => o.OrderNo == order.OrderNo,
    o => new { o.Status });
client.ShardDelete<Order>(
    o => o.OrderNo == order.OrderNo,
    queryParams: new Dictionary<string, object> { { "CreateTime", order.CreateTime } });
```

### 消息队列 (.NET 6+)

```csharp
// 配置写入队列（降级模式）
FastWrite.ConfigureQueue<User>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    EnableFallback = true,
    Topic = "users:write"
});

// 使用链式 API 写入
var result = client.WriteQueue()
    .WithMetadata(new Dictionary<string, object> { { "source", "app" } })
    .Add(user)
    .Execute();

// 链式 API 可显式指定 Redis，队列消费会按原数据库 Key 回写
var queued = FastWrite.QueueBuilder("SqlServer")
    .WithRedis("127.0.0.1:6379", redisDb: 7)
    .WithQueue(new WriteBehindConfig
    {
        QueueType = WriteBehindQueueType.ReliableQueue,
        Topic = "users:write",
        EnableFallback = true
    })
    .Add(user)
    .Execute();
```

### AOP 拦截

```csharp
public class LoggingAop : IFastAop
{
    public void OnBefore(BeforeContext context)
        => Console.WriteLine($"Executing: {context.Sql}");
    
    public void OnAfter(AfterContext context)
        => Console.WriteLine($"Completed: {context.Sql}");
    
    public void OnException(ExceptionContext context)
        => Console.WriteLine($"Error: {context.Exception.Message}");
}

// 注册 AOP
FastMap.InstanceMap(aop: new LoggingAop());
```

---

## DataDbType 数据库类型枚举

`DataDbType` 是数据库类型的枚举定义，用于指定数据库类型：

```csharp
public enum DataDbType
{
    Oracle = 1,
    MySql = 2,
    SqlServer = 3,
    DB2 = 4,
    SQLite = 5,
    PostgreSql = 6
}
```

**使用场景**：
- 配置数据库适配器
- 动态切换数据库类型
- CodeFirst 建表时指定数据库类型

```csharp
// 获取数据库适配器
var adapter = DatabaseAdapterFactory.Create(DataDbType.SqlServer);

// 配置数据库类型
var config = new ConfigModel
{
    Key = "DefaultDb",
    DbType = DataDbType.MySql,
    ConnStr = "Server=localhost;Database=TestDb;"
};
```

---

## FastReadDb/FastWriteDb 绑定 Key 的读写

`FastReadDb` 和 `FastWriteDb` 是绑定数据库 Key 的读写类，避免重复传递 key 参数：

### FastReadDb

```csharp
// 创建绑定 Key 的读取实例
var readDb = new FastReadDb("DefaultDb");

// 查询（无需再传递 key）
var users = readDb.Query<User>(u => u.IsActive).ToList();
var user = readDb.Query<User>(u => u.Id == 1).ToItem();

// 原生 SQL 查询
var results = readDb.ExecuteSql<User>("SELECT * FROM Users WHERE Age > @Age", 
    new SqlParameter("@Age", 18));

// XML Map 查询
var mapResults = readDb.MapQuery<User>("GetActiveUsers", new[]
{
    new SqlParameter("@IsActive", true)
});
```

### FastWriteDb

```csharp
// 创建绑定 Key 的写入实例
var writeDb = new FastWriteDb("DefaultDb");

// 添加（无需再传递 key）
writeDb.Add(new User { Name = "张三", Age = 30 });

// 批量添加
writeDb.AddList(users);

// 更新
user.Name = "李四";
writeDb.Update(user);

// 删除
writeDb.Delete<User>(u => u.Age < 18);

// CodeFirst 建表
writeDb.CodeFirst<User>(isDropExists: false);
```

---

## ShardingReadHelper/ShardingWriteHelper 分片读写帮助类

分片读写帮助类用于大数据量的分表查询和写入：

### ShardingReadHelper

```csharp
// 创建分片读取帮助类
var shardRead = new ShardingReadHelper("DefaultDb");

// 分片查询
var results = shardRead.ShardQuery<Order>(
    predicate: o => o.CreateTime > DateTime.Now.AddMonths(-3),
    queryParams: new Dictionary<string, object> 
    { 
        { "CreateTime", DateTime.Now.AddMonths(-3) } 
    }
);

// 分片查询（带分页）
var page = shardRead.ShardQuery<User>(
    predicate: u => u.IsActive,
    queryParams: new Dictionary<string, object> { { "IsActive", true } }
);
```

### ShardingWriteHelper

```csharp
// 创建分片写入帮助类
var shardWrite = new ShardingWriteHelper("DefaultDb");

// 分片添加
shardWrite.ShardAdd(new Order 
{ 
    UserId = 1, 
    Total = 100, 
    CreateTime = DateTime.Now 
});

// 分片批量添加
shardWrite.ShardAddList(orders);

// 分片删除
shardWrite.ShardDelete<Order>(
    predicate: o => o.CreateTime < DateTime.Now.AddYears(-1),
    queryParams: new Dictionary<string, object> 
    { 
        { "CreateTime", DateTime.Now.AddYears(-1) } 
    }
);
```

---

## 缓存体系

FastData 提供完整的缓存解决方案，支持内存缓存和 Redis 缓存两种模式。

### 缓存架构

```
┌─────────────────────────────────────────────────────────────┐
│                      应用层                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ CacheService │  │ UserCache    │  │ 业务代码     │      │
│  │ (Demo示例)   │  │ Service      │  │              │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
├─────────┼─────────────────┼─────────────────┼───────────────┤
│         │        API层    │                 │               │
│  ┌──────▼─────────────────▼─────────────────▼───────┐      │
│  │              RedisInfo (静态方法)                  │      │
│  │  Get/Set/Remove/Exists/Increment/SetExpire        │      │
│  └──────────────────────┬────────────────────────────┘      │
├─────────────────────────┼───────────────────────────────────┤
│                    实现层                                    │
│  ┌──────────────────────▼────────────────────────────┐      │
│  │              DbCache (内部路由)                    │      │
│  │     根据 CacheType 路由到不同实现                  │      │
│  └──────┬────────────────────────────┬───────────────┘      │
│  ┌──────▼───────┐              ┌─────▼────────┐             │
│  │  BaseCache   │              │  RedisInfo   │             │
│  │  (内存缓存)   │              │  (Redis缓存) │             │
│  │  web=默认    │              │  redis=配置  │             │
│  └──────────────┘              └──────────────┘             │
└─────────────────────────────────────────────────────────────┘
```

### 缓存组件说明

| 组件 | 说明 | 使用场景 |
|------|------|----------|
| **CacheAttribute** | 声明式缓存配置，标注在实体类上 | 自动缓存管理 |
| **DbCache** | 内部路由层，根据 CacheType 路由到不同实现 | 框架内部使用 |
| **BaseCache** | 内存缓存（MemoryCache），CacheType="web" | 单机部署、开发调试 |
| **RedisInfo** | Redis 缓存，CacheType="redis" | 多机部署、生产环境 |

### 缓存类型

| 类型 | CacheType值 | 说明 | 适用场景 |
|------|-------------|------|----------|
| 内存缓存 | `web` (默认) | 进程内 MemoryCache | 单机部署、开发调试 |
| Redis 缓存 | `redis` | Redis 分布式缓存 | 多机部署、生产环境 |

### 核心 API

#### RedisInfo（推荐使用）

```csharp
using FastRedis;

// 初始化（程序启动时调用一次）
RedisInfo.Init("db.config");

// 基本操作
RedisInfo.Set("user:1", user, 300);              // 设置（300小时过期）
var user = RedisInfo.Get<User>("user:1");         // 获取
RedisInfo.Remove("user:1");                       // 删除
bool exists = RedisInfo.Exists("user:1");         // 是否存在

// 异步操作
await RedisInfo.SetAsync("user:1", user, 300);
var user = await RedisInfo.GetAsync<User>("user:1");
await RedisInfo.RemoveAsync("user:1");

// 计数器
long count = RedisInfo.Increment("page:views", 1);
long count = RedisInfo.Increment("user:1:login_count", 1);

// 设置过期时间
RedisInfo.SetExpire("user:1", TimeSpan.FromMinutes(30));

// 批量操作
var dict = new Dictionary<string, User> { ["user:1"] = user1, ["user:2"] = user2 };
RedisInfo.SetDic(dict, db: 0);
var cachedDict = RedisInfo.GetDic<User>(new[] { "user:1", "user:2" }, db: 0);
```

#### BaseCache（内存缓存）

```csharp
using FastUntility.Cache;

// 基本操作
BaseCache.Set("key", "value", 24);                // 24小时过期
var value = BaseCache.Get("key");
BaseCache.Remove("key");
bool exists = BaseCache.Exists("key");

// 泛型操作
BaseCache.Set<User>("user:1", user, 24);
var user = BaseCache.Get<User>("user:1");
```

### CacheAttribute（声明式缓存）

在实体类上标注缓存策略，框架自动管理缓存读写：

```csharp
[Table(Name = "Products")]
[Cache(IsEnable = true, ExpireTime = 300, Key = "product:{Id}", CacheType = "Redis")]
public class Product
{
    [Primary]
    [Column(IsIdentity = true)]
    public int Id { get; set; }

    [Column(Length = 100)]
    public string ProductName { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public DateTime UpdateTime { get; set; }
}
```

**CacheAttribute 参数说明**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsEnable` | bool | false | 是否启用缓存 |
| `ExpireTime` | int | 300 | 过期时间（秒） |
| `Key` | string | null | 缓存键模板，支持 `{PropertyName}` 占位符 |
| `CacheType` | string | "Redis" | 缓存类型：`Redis` 或 `Local` |

**Key 模板示例**：
- `"product:{Id}"` → `"product:123"`
- `"user:{Id}:profile"` → `"user:456:profile"`
- `"order:{UserId}:{Id}"` → `"order:789:1001"`

### 缓存键设计规范

**错误示例**（Key 写死）：
```csharp
// 所有用户都用同一个 key，数据会互相覆盖
[Cache(Key = "user")]
RedisInfo.Set("users", userList, 300);  // 查询条件不同时会返回错误数据
```

**正确示例**（动态 Key）：
```csharp
// 单条数据：表名:主键值
var cacheKey = $"user:{userId}";
var user = RedisInfo.Get<User>(cacheKey);

// 列表数据：表名:条件1_值1:条件2_值2
var listKey = $"users:age_gt_{age}:active_{isActive}";
var users = RedisInfo.Get<List<User>>(listKey);

// 计数数据：表名:count:条件
var countKey = $"users:count:active_{isActive}";
var count = RedisInfo.Get<int>(countKey);
```

### 缓存模式

#### 1. Cache-Aside（旁路缓存）- 最常用

```csharp
// 读取：先查缓存，未命中再查数据库
public User GetUserById(int id)
{
    var cacheKey = $"user:{id}";
    var user = RedisInfo.Get<User>(cacheKey);
    
    if (user == null)
    {
        user = FastRead.Query<User>(u => u.Id == id).ToItem();
        if (user != null)
        {
            RedisInfo.Set(cacheKey, user, 300);
        }
    }
    return user;
}

// 更新：先更新数据库，再删除缓存
public void UpdateUser(User user)
{
    FastWrite.Update(user);
    RedisInfo.Remove($"user:{user.Id}");
}
```

#### 2. Write-Through（写穿透）

```csharp
// 更新时同时更新数据库和缓存
public void UpdateUser(User user)
{
    FastWrite.Update(user);
    RedisInfo.Set($"user:{user.Id}", user, 300);  // 同步更新缓存
}
```

#### 3. 缓存穿透防护

```csharp
// 查询可能不存在的数据
var user = FastRead.Query<User>(u => u.Id == 99999).ToItem();
if (user != null)
{
    RedisInfo.Set($"user:99999", user, 300);
}
else
{
    // 缓存空值，防止穿透（较短过期时间）
    RedisInfo.Set($"user:99999", "", 60);
}
```

#### 4. 缓存降级

```csharp
public User GetUserByIdSafe(int id)
{
    try
    {
        var cacheKey = $"user:{id}";
        var user = RedisInfo.Get<User>(cacheKey);
        
        if (user == null)
        {
            user = FastRead.Query<User>(u => u.Id == id).ToItem();
            if (user != null)
            {
                RedisInfo.Set(cacheKey, user, 300);
            }
        }
        return user;
    }
    catch (Exception ex)
    {
        // Redis 异常，降级到数据库查询
        Console.WriteLine($"Redis 异常，降级到数据库: {ex.Message}");
        return FastRead.Query<User>(u => u.Id == id).ToItem();
    }
}
```

#### 5. 缓存预热

```csharp
public void WarmUpCache()
{
    // 加载热门商品到缓存
    var hotProducts = FastRead.Query<Product>(p => p.Stock > 0)
        .OrderBy<Product>(p => p.UpdateTime)
        .Take(100)
        .ToList();

    foreach (var product in hotProducts)
    {
        RedisInfo.Set($"product:{product.Id}", product, 3600);
    }
    
    Console.WriteLine($"预热完成，加载 {hotProducts.Count} 条数据");
}
```

### 缓存服务封装（Demo 示例）

```csharp
// 缓存服务接口
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new();
    Task<bool> SetAsync<T>(string key, T value, int hours = 24) where T : class;
    Task<T> GetAsync<T>(string key) where T : class, new();
    Task<bool> RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key, int value = 1);
}

// 缓存服务实现
public class CacheService : ICacheService
{
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int hours = 24) where T : class, new()
    {
        // 尝试从缓存获取
        try
        {
            var cached = RedisInfo.Get<T>(key);
            if (cached != null && !EqualityComparer<T>.Default.Equals(cached, default))
                return cached;
        }
        catch
        {
            // Redis not available, fall through to factory
        }

        // 从工厂方法获取
        var value = await factory();
        if (value != null)
        {
            try
            {
                RedisInfo.Set(key, value, hours);
            }
            catch
            {
                // Redis not available, skip caching
            }
        }

        return value;
    }
}

// 用户缓存服务
public class UserCacheService : IUserCacheService
{
    private readonly ICacheService _cacheService;

    public UserCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<AppUser> GetUserAsync(int userId, Func<Task<AppUser>> factory)
    {
        var key = $"user:{userId}";
        return await _cacheService.GetOrSetAsync(key, factory, hours: 2);
    }

    public async Task<List<AppUser>> GetActiveUsersAsync(Func<Task<List<AppUser>>> factory)
    {
        var key = "users:active";
        return await _cacheService.GetOrSetAsync(key, factory, hours: 1);
    }

    public async Task RemoveUserAsync(int userId)
    {
        await _cacheService.RemoveAsync($"user:{userId}");
        await _cacheService.RemoveAsync("users:active");
    }

    public async Task IncrementViewCountAsync(int userId)
    {
        await _cacheService.IncrementAsync($"user:{userId}:views");
    }
}
```

### 配置文件示例

```xml
<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" 
         ConnStr="Server=.;Database=TestDb;Trusted_Connection=true;"
         CacheType="redis" />
  </Connections>
  <Redis>
    <Add Server="127.0.0.1:6379" Db="0" Password="" />
  </Redis>
</DataConfig>
```

**注意**：
- `CacheType="web"`（默认）：使用内存缓存，无需配置 Redis 节点
- `CacheType="redis"`：使用 Redis 缓存，必须配置 Redis 节点

---

## LINQ 扩展方法

DataQueryExtensions 提供了额外的 LINQ 扩展方法：

```csharp
// Take - 限制返回数量
var top10 = client.Query<User>(u => u.IsActive)
    .OrderByDescending<User>(u => u.CreateTime)
    .Take(10)
    .ToList();

// Skip - 跳过指定数量
var skipFirst20 = client.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .Skip(20)
    .Take(10)
    .ToList();

// ToPage - 分页查询
var page = client.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPage(new PageModel { PageIndex = 2, PageSize = 20 });

// ToCount - 计数
var count = client.Query<User>(u => u.Age > 18).ToCount();

// ToListAsync - 异步查询
var users = await client.Query<User>(u => u.IsActive).ToListAsync();
```

---

## 实体特性（Attributes）

### PrimaryAttribute

标记主键字段：

```csharp
[Table("Users")]
public class User
{
    [Column("Id"), Primary]
    public long Id { get; set; }
    
    [Column("Name")]
    public string Name { get; set; }
}
```

### ColumnAttribute

映射数据库列名：

```csharp
[Table("Users")]
public class User
{
    [Column("user_id")]
    public long UserId { get; set; }
    
    [Column("user_name")]
    public string UserName { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
}
```

### CacheAttribute

标记缓存配置：

```csharp
[Table("Users")]
[Cache(IsEnable = true, ExpireTime = 300, Key = "user:{Id}", CacheType = "Redis")]
public class User
{
    [Column("Id"), Primary]
    public long Id { get; set; }
    
    [Column("Name")]
    public string Name { get; set; }
}
```

**参数说明**：
- `IsEnable`: 是否启用缓存（默认 false）
- `ExpireTime`: 过期时间，秒（默认 300）
- `Key`: 缓存键模板，支持 `{PropertyName}` 占位符
- `CacheType`: 缓存类型，`Redis` 或 `Local`（默认 Redis）

---

## 消息队列

FastData 提供完整的消息队列解决方案，支持 RTU 削峰、多方推送、数据库降级等场景。

### 消息队列架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        应用层                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ FastWrite    │  │ FastRead     │  │ 业务代码     │          │
│  │ .QueueBuilder│  │ .QueueBuilder│  │              │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
├─────────┼─────────────────┼─────────────────┼───────────────────┤
│         │        API层    │                 │                   │
│  ┌──────▼─────────────────▼─────────────────▼───────┐          │
│  │           WriteBehindExecutor / ReadQueueExecutor │          │
│  │           执行器（自动路由到数据库或队列）         │          │
│  └──────────────────────┬────────────────────────────┘          │
├─────────────────────────┼───────────────────────────────────────┤
│                    配置层                                        │
│  ┌──────────────────────▼────────────────────────────┐          │
│  │           WriteBehindRegistry (注册表)             │          │
│  │           管理表级别的队列配置                      │          │
│  └──────────────────────┬────────────────────────────┘          │
├─────────────────────────┼───────────────────────────────────────┤
│                    实现层                                        │
│  ┌──────────────────────▼────────────────────────────┐          │
│  │           MessageQueueIntegrationService           │          │
│  └──────┬────────────────────────────┬───────────────┘          │
│  ┌──────▼───────┐              ┌─────▼────────┐                 │
│  │ ReliableQueue│              │    Stream    │                 │
│  │ (可信队列)   │              │ (多消费组)   │                 │
│  │ 单消费确认   │              │ 多方推送     │                 │
│  └──────────────┘              └──────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

### 队列类型

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| **ReliableQueue** | 可信队列，单消费确认，消息不丢失 | RTU 削峰、数据库存储 |
| **Stream** | 多消费组队列，多方独立消费 | 多方推送、事件广播 |

### 核心组件

| 组件 | 说明 |
|------|------|
| **WriteBehindConfig** | 队列配置（QueueType/Topic/EnableFallback/EnableAutoRecovery） |
| **WriteBehindRegistry** | 注册表，管理表级别的队列配置 |
| **WriteBehindExecutor** | 写入执行器，支持数据库降级到队列 |
| **ReadQueueExecutor** | 读取执行器，将查询请求推送到队列 |
| **QueueFlushService** | 后台服务，监控队列并在数据库恢复后自动刷写 |
| **FastWriteQueueBuilder** | 写入链式 API |
| **FastReadQueueBuilder** | 读取链式 API |

### 使用场景

#### 场景 1: RTU 削峰

大量 RTU 数据上传时，通过队列缓冲，异步批量写入数据库。

```csharp
// 配置表级别队列
FastWrite.ConfigureQueue<SensorData>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    EnableFallback = true,
    EnableAutoRecovery = true,
    Topic = "rtu:sensor"
});

// 使用链式 API 写入
var result = FastWrite.QueueBuilder()
    .WithMetadata(new Dictionary<string, object>
    {
        {"source", "RTU-001"},
        {"batchId", "BATCH-2026053001"}
    })
    .Add(sensorData1)
    .Add(sensorData2)
    .Execute();

// 结果
Console.WriteLine($"直接写入: {result.DirectWriteCount}");
Console.WriteLine($"队列降级: {result.QueuedCount}");
Console.WriteLine($"失败: {result.FailedCount}");
```

#### 场景 2: 多方推送

使用 Stream 将数据推送到多个消费组，每个组独立消费。

```csharp
// 配置 Stream 队列
FastWrite.ConfigureQueue<RealtimeData>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.Stream,
    Topic = "rtu:realtime"
});

// 发布数据
var mqService = new MessageQueueIntegrationService(redis);
var count = await mqService.PublishDataAsync("rtu:realtime", dataList, MessageQueueType.Stream);

// 启动多个消费组
await mqService.StartMultiGroupConsumerAsync(
    "rtu:realtime",
    new[] { "db-writer", "alert-system", "analytics" },
    new Func<RealtimeData, Task>[]
    {
        async (data) => { /* 数据库存储 */ },
        async (data) => { /* 告警系统 */ },
        async (data) => { /* 数据分析 */ }
    },
    cancellationToken,
    concurrency: 1);
```

#### 场景 3: 数据库降级

数据库异常时自动降级到队列，恢复后自动刷写。

```csharp
// 配置降级模式
FastWrite.ConfigureQueue<Order>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    EnableFallback = true,        // 启用降级
    EnableAutoRecovery = true,    // 启用自动恢复
    RecoveryIntervalSeconds = 30, // 恢复检查间隔
    Topic = "orders"
});

// 启动后台刷写服务
var flushService = new QueueFlushService(mqService);
flushService.Start();

// 写入操作（自动降级）
var result = FastWrite.QueueBuilder()
    .Add(order1)
    .Add(order2)
    .Execute();

// 查看队列状态
var status = flushService.GetStatus();
```

#### 场景 4: 查询审计

将查询请求推送到队列，实现异步查询或查询审计。

```csharp
// 配置查询队列
FastRead.ConfigureQueue<User>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    Topic = "user-queries"
});

// 推送查询请求
var result = FastRead.QueueBuilder<User>()
    .WithMetadata(new Dictionary<string, object>
    {
        {"requestId", Guid.NewGuid().ToString()},
        {"source", "web-ui"},
        {"userId", 1001}
    })
    .QueryList(u => u.IsActive)
    .QueryCount(u => u.Age > 25)
    .QueryPaging(1, 10, u => u.IsActive, u => u.CreateTime, false)
    .Execute();
```

### API 参考

#### FastWrite 链式 API

```csharp
// 基本用法
FastWrite.QueueBuilder()
    .Add(entity)                    // 添加
    .Update(entity)                 // 更新
    .Delete(entity)                 // 删除
    .Execute();                     // 执行

// 带元数据
FastWrite.QueueBuilder()
    .WithMetadata(dict)             // 全局元数据
    .Add(entity, metadata)          // 操作级元数据
    .Execute();

// 批量操作
FastWrite.QueueBuilder()
    .AddRange(entities)             // 批量添加
    .Execute();

// 异步执行
var result = await FastWrite.QueueBuilder()
    .Add(entity)
    .ExecuteAsync();
```

#### FastRead 链式 API

```csharp
// 基本用法
FastRead.QueueBuilder<User>()
    .QuerySingle(u => u.Id == 1)   // 查询单条
    .QueryList(u => u.IsActive)    // 查询列表
    .QueryCount(u => u.Age > 25)   // 查询数量
    .QueryPaging(1, 10)            // 分页查询
    .Execute();

// 带排序
FastRead.QueueBuilder<User>()
    .QueryList(u => u.IsActive, u => u.CreateTime, false)
    .Execute();

// 带元数据
FastRead.QueueBuilder<User>()
    .WithMetadata(dict)
    .QueryList(metadata: metadata)
    .Execute();
```

#### 配置管理

```csharp
// 注册表级别配置
WriteBehindRegistry.Register<User>(new WriteBehindConfig
{
    QueueType = WriteBehindQueueType.ReliableQueue,
    Topic = "users",
    EnableFallback = true,
    EnableAutoRecovery = true,
    BatchFlushSize = 100,
    RecoveryIntervalSeconds = 30
});

// 检查是否启用队列
bool isEnabled = WriteBehindRegistry.IsQueueEnabled<User>();

// 获取配置
var config = WriteBehindRegistry.GetConfig<User>();

// 从配置文件加载
WriteBehindRegistry.LoadFromConfig("writebehind.json");

// 保存配置到文件
WriteBehindRegistry.SaveToConfig("writebehind.json");
```

### 配置示例

#### writebehind.json

```json
[
  {
    "TableName": "SensorData",
    "Config": {
      "QueueType": 1,
      "Topic": "rtu:sensor",
      "EnableFallback": true,
      "EnableAutoRecovery": true,
      "BatchFlushSize": 100,
      "RecoveryIntervalSeconds": 30,
      "RedisDb": 7
    }
  },
  {
    "TableName": "RealtimeData",
    "Config": {
      "QueueType": 2,
      "Topic": "rtu:realtime",
      "EnableFallback": false,
      "EnableAutoRecovery": false
    }
  }
]
```

### 消息模型

#### WriteOperation（写入操作）

| 属性 | 类型 | 说明 |
|------|------|------|
| `OperationType` | WriteOperationType | 操作类型（Add/Update/Delete） |
| `TableName` | string | 表名 |
| `EntityType` | string | 实体类型全名 |
| `Data` | string | 序列化后的数据（JSON） |
| `DatabaseKey` | string | 数据库 Key |
| `Timestamp` | DateTime | 操作时间戳 |
| `OperationId` | string | 操作唯一标识 |
| `RetryCount` | int | 重试次数 |
| `MaxRetries` | int | 最大重试次数 |
| `Metadata` | Dictionary | 扩展元数据 |

#### ReadOperation（读取操作）

| 属性 | 类型 | 说明 |
|------|------|------|
| `OperationType` | ReadOperationType | 操作类型 |
| `TableName` | string | 表名 |
| `EntityType` | string | 实体类型全名 |
| `Predicate` | string | 查询条件（JSON） |
| `Fields` | string | 查询字段 |
| `OrderBy` | string | 排序字段 |
| `PageIndex` | int | 页码 |
| `PageSize` | int | 每页大小 |
| `Metadata` | Dictionary | 扩展元数据 |

---

## 命名空间

| 命名空间 | 用途 |
|----------|------|
| `FastData` | 顶层入口（FastRead, FastWrite, FastMap, FastDb, FastDataClient） |
| `FastData.Base` | SQL 构建、表达式访问、缓存 |
| `FastData.Model` | 数据模型（ConfigModel, DataQuery, WriteReturn） |
| `FastData.Context` | 数据库上下文 |
| `FastData.Config` | XML 配置加载 |
| `FastData.Adapter` | SQL 方言实现 |
| `FastData.Sharding` | 分表策略 |
| `FastData.Queue` | 消息队列 |
| `FastData.Repository` | Repository 模式 |
| `FastData.Aop` | AOP 拦截接口 |
| `FastData.DbTypes` | 数据库类型枚举（DataDbType） |
| `FastData.Extensions` | LINQ 扩展方法 |
| `FastData.Property` | 实体属性映射（Primary, Column, Cache） |

---

## 依赖

- FastRedis
- FastUntility
- System.Configuration.ConfigurationManager 8.0.0

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
