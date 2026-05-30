# FastData

FastData 是一个轻量级多目标框架 ORM，支持 .NET Framework 4.5 / .NET 6.0 / .NET 8.0 / .NET 10.0。

## 目标框架

| 框架 | 说明 |
|------|------|
| `net45` | .NET Framework 4.5 |
| `net6.0` / `net8.0` / `net10.0` | Modern .NET |

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

## CacheHelper/CacheKey 缓存工具类

缓存工具类用于简化 Redis 缓存操作：

### CacheKey 生成器

```csharp
// 生成实体缓存 Key（主键查询）
var cacheKey = CacheKey.ForEntity<User>(1);
// 结果: "User:1"

// 生成列表缓存 Key（条件查询）
var listKey = CacheKey.ForList<User>(u => u.IsActive);
// 结果: "User:List:IsActive=True"

// 生成计数缓存 Key
var countKey = CacheKey.ForCount<User>(u => u.Age > 18);
// 结果: "User:Count:Age>18"
```

### CacheHelper

```csharp
// 获取或设置缓存（缓存穿透防护）
var user = CacheHelper.GetOrSet(
    cacheKey, 
    () => client.Query<User>(u => u.Id == 1).ToItem(), 
    expirySeconds: 300
);

// 获取列表缓存
var users = CacheHelper.GetOrSet(
    listKey,
    () => client.Query<User>(u => u.IsActive).ToList(),
    expirySeconds: 600
);

// 删除缓存
CacheHelper.Remove(cacheKey);

// 批量删除缓存
CacheHelper.RemoveByPrefix("User:");
```

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

标记缓存配置（仅定义，未实现自动逻辑）：

```csharp
[Table("Users")]
[Cache(ExpirySeconds = 300)]
public class User
{
    [Column("Id"), Primary]
    public long Id { get; set; }
    
    [Column("Name")]
    public string Name { get; set; }
}
```

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
