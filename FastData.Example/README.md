# FastData.Example

FastData.Example 是控制台示例程序，提供 FastData ORM 所有主要功能的可运行示例。

**最新更新 (2026-06-13)**:
- ✅ 新增 `ShardingReadHelper` 分表查询示例
- ✅ 新增 `ShardingWriteHelper` 分表写入示例
- ✅ 更新 `FastDataClientQueueExample` 链式 Redis 队列配置
- ✅ 更新 `ReliableQueueExample` 多数据库队列降级
- ✅ 修复 `ReportExample` - 报表统计示例
- ✅ 修复 `DynamicQueryExample` - 动态查询示例
- ✅ 修复 `DataValidationExample` - 数据校验示例

**最新更新 (2026-05-31)**:
- ✅ 新增 `HighFrequencyCacheExample.cs` - 高频数据缓存策略
- ✅ 新增 `ApiAuthenticationExample.cs` - API 认证示例
- ✅ 新增 `ReliableQueueExample.cs` - 可信队列示例
- ✅ 新增 `FastDataClientQueueExample.cs` - FastDataClient 消息队列示例

## 目标框架

| 框架 | 说明 |
|------|------|
| `net8.0` / `net10.0` | Modern .NET |

## 示例列表

| # | 文件 | 说明 |
|---|------|------|
| 1 | `BasicCrudExample.cs` | 基础 CRUD 操作（增删改查） |
| 2 | `LambdaQueryExample.cs` | Lambda 查询链式调用 |
| 3 | `RawSqlExample.cs` | 原生 SQL 查询 |
| 4 | `MapSqlExample.cs` | XML 映射 SQL |
| 5 | `TransactionExample.cs` | 数据库事务处理 |
| 6 | `MultiDbExample.cs` | 多数据库切换 |
| 7 | `DataSyncExample.cs` | 数据同步示例 |
| 8 | `MessageQueueExample.cs` | 消息队列（可靠队列/Stream） |
| 9 | `PaginationExample.cs` | 分页查询 |
| 10 | `ShardingExample.cs` | 分表基础示例 |
| 11 | `ShardingFullExample.cs` | 完整分表示例（SQL Server） |
| 12 | `CodeFirstExample.cs` | CodeFirst 建表 |
| 13 | `BulkOperationsExample.cs` | 批量插入操作 |
| 14 | `RedisAdvancedExample.cs` | Redis 高级特性 |
| 15 | `RedisCacheExample.cs` | Redis 缓存示例 |
| 16 | `FastDataClientExample.cs` | FastDataClient 统一门面 |
| 17 | `CacheBestPracticeExample.cs` | 缓存最佳实践 |
| 18 | `UserManagementExample.cs` | 用户管理（注册/登录/缓存/分页/软删除） |
| 19 | `OrderBusinessExample.cs` | 订单业务（下单/支付/发货/取消/事务） |
| 20 | `CacheStrategyExample.cs` | 缓存策略（穿透/雪崩/预热/分布式锁） |
| 21 | `ReportExample.cs` | 报表统计（GroupBy/Join/聚合/导出） |
| 22 | `DynamicQueryExample.cs` | 动态查询（Where 构建器/Any/All/First/Single） |
| 23 | `DataValidationExample.cs` | 数据校验（NullSafety/字段验证/异常处理） |
| 24 | `ApiAuthenticationExample.cs` | API 认证（Token/JWT/RSA/AES/统一返回格式） |
| 25 | `ReliableQueueExample.cs` | 可信队列（连接池满/超时自动降级/批量写入） |
| 26 | `FastDataClientQueueExample.cs` | FastDataClient 消息队列（统一入口/链式 API） |
| 27 | `HighFrequencyCacheExample.cs` | 高频数据缓存（Web 缓存/Redis/数据一致性） |
| 28 | `ShardingReadHelperExample.cs` | 分表读取（`ShardQuery`/`ShardQueryPage`） |
| 29 | `ShardingWriteHelperExample.cs` | 分表写入（`ShardAdd`/`ShardUpdate`/`ShardDelete`） |
| 30 | `ShardingCrudExample.cs` | 分表 CRUD 综合示例（多数据库） |

## 运行示例

```bash
# 交互模式
dotnet run --project FastData.Example --framework net10.0

# 直接选择示例
echo "1" | dotnet run --project FastData.Example --framework net10.0
```

## 示例详情

### 1. Basic CRUD
演示使用 `FastWrite` 和 `FastRead` 进行增删改查操作。

### 2. Lambda Query
展示 `DataQuery<T>` 链式 API 和条件构建：
```csharp
var users = FastRead.Query<User>(u => u.IsActive)
    .Where<User>(u => u.Age > 18)
    .OrderBy<User>(u => u.Name)
    .Take(100)
    .ToList();
```

### 3. Raw SQL
执行原生 SQL 查询，支持参数化输入。

### 4. XML Map SQL
使用 XML 映射 SQL 语句（类似 MyBatis）：
```csharp
var users = FastMap.Query<List<User>>("GetActiveUsers", new[]
{
    new SqlParameter("@IsActive", true)
});
```

### 5. Transaction
数据库事务处理，支持提交和回滚。

### 6. Multi-Database
使用 `FastDataClient` 或 `FastDb.Use(key)` 切换数据库连接。

### 7. Data Sync
演示数据库之间的数据同步。

### 8. Message Queue (.NET 6+ only)
- **ReliableQueue**: 单消费确认队列
- **Stream**: 多消费组流
- **WriteQueue**: 写入队列（降级模式）

### 9. Pagination
分页 API：
```csharp
var page = FastRead.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });
```

### 10. Basic Sharding
演示分表策略：时间分表、哈希分表、列表分表。

### 11. Full Sharding Example
SQL Server 完整分表：10000 条日志记录、哈希分表、查询频率分表。

### 12. CodeFirst
使用 `FastWrite.CodeFirst<T>()` 自动建表：
```csharp
var result = FastWrite.CodeFirst<User>("DefaultDb", isDropExists: false);
```

### 13. Bulk Operations
高性能批量插入：
```csharp
var result = FastWrite.AddList(users);
```

### 14. Redis Advanced
Redis 高级特性：分布式锁、发布订阅、Lua 脚本。

### 15. Redis Cache
Redis 缓存示例：主动缓存、自动缓存、缓存模式。

### 16. FastDataClient
FastDataClient 统一门面示例：CRUD、缓存、消息队列、高级功能。

### 17. Cache Best Practice
缓存最佳实践：key 写死问题、动态 key、自定义 model 缓存。

### 25. Reliable Queue
可信队列示例：连接池满/超时时自动降级到队列，支持批量写入。

### 26. FastDataClient Queue
FastDataClient 消息队列：统一入口、链式 API：
```csharp
// 链式 API 支持显式指定 Redis，队列消费会按原数据库 Key 回写
var queued = FastWrite.QueueBuilder("MySql")
    .WithRedis("127.0.0.1:6379", redisDb: 7)
    .WithQueue(new WriteBehindConfig { QueueType = WriteBehindQueueType.ReliableQueue, Topic = "users:write" })
    .Add(user)
    .Execute();
```

### 27. High Frequency Cache
高频数据缓存：Web 缓存/Redis/数据一致性。

### 28. ShardingReadHelper
分表读取操作：
```csharp
// 分表查询（自动路由到物理分表）
var results = ShardingReadHelper.Query<Order>(predicate, queryParams);
// 分表分页查询
var page = ShardingReadHelper.QueryPage<Order>(predicate, queryParams, pageIndex, pageSize);
```

### 29. ShardingWriteHelper
分表写入操作：
```csharp
// 分表新增
ShardingWriteHelper.Add(order);
// 分表更新
ShardingWriteHelper.Update(order, predicate);
// 分表删除
ShardingWriteHelper.Delete<Order>(predicate, queryParams);
```

### 30. Sharding CRUD (Multi-Database)
多数据库分表 CRUD 综合示例，覆盖 SQL Server/MySQL/PostgreSQL/SQLite。

## 配置

### appsettings.json (.NET 6+)
```json
{
  "DataConfig": {
    "Default": "DefaultDb",
    "Connections": [
      {
        "Provider": "SqlServer",
        "Key": "DefaultDb",
        "ConnStr": "Server=.;Database=FastDataDemo;Trusted_Connection=true;"
      }
    ]
  }
}
```

### db.config (.NET Framework 4.5)
```xml
<?xml version="1.0" encoding="utf-8"?>
<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" ConnStr="Server=.;Database=TestDb;Trusted_Connection=true;" />
  </Connections>
</DataConfig>
```

## 构建

```bash
# 构建所有目标
dotnet build FastData.Example

# 构建特定目标
dotnet build FastData.Example --framework net10.0
```

## 依赖

- FastData
- FastRedis
- FastUntility
- Microsoft.Extensions.Configuration.FileExtensions (net6+)

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
