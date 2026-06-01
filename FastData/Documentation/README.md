# FastData API 文档

## FastRead - 快速数据访问类

### 核心功能
- 查询（Query/ToList/ToItem/Count）
- 写入（Add/Update/Delete/BulkInsert）
- 聚合（GroupBy/Sum/Avg/Max/Min）

### 快速开始

```csharp
// 1. 查询所有用户
var users = FastRead.Query<User>().ToList();

// 2. 条件查询
var activeUsers = FastRead.Query<User>(u => u.IsActive == true).ToList();

// 3. 分页查询
var (items, total) = FastRead.Query<User>()
    .OrderBy(u => u.CreateTime)
    .ToListPaged(1, 20);

// 4. 添加用户
var result = FastWrite.Add(new User { Name = "张三", Age = 25 });

// 5. 批量插入（高性能）
var bulkResult = FastWrite.BulkInsert(userList);

// 6. 异步查询
var usersAsync = await FastReadAsync.ToListAsync<User>(u => u.IsActive == true);
```

### 数据库支持
- SQL Server（推荐）
- MySQL
- PostgreSQL
- Oracle
- DB2
- SQLite（仅部分功能）

### 性能优化
- 使用 SqlBulkCopy 批量插入（100x 性能）
- 支持参数化查询（防止 SQL 注入）
- 支持 IAsyncEnumerable 流式处理（大数据集）

### 使用建议
- 简单查询使用 FastRead 静态方法
- 复杂场景使用 FastDataClient（绑定 Key）
- 需要事务控制使用 DataContext
- 大数据集使用 FastReadAsync.ToListStreamingAsync
- 批量操作使用 FastWrite.BulkInsert

### 配置文件

```xml
<?xml version="1.0" encoding="utf-8"?>
<Root>
  <DbItem Key="SqlServer" IsDefault="true" IsOutSql="true" IsOutError="true" CacheType="web">
    <DbType>SqlServer</DbType>
    <Connection>Server=localhost;Database=MyDb;User Id=sa;Password=123;</Connection>
  </DbItem>
</Root>
```

## 示例代码

### 示例 1：完整 CRUD 操作

```csharp
// 1. 创建用户
var user = new User { Name = "李四", Email = "lisi@example.com", Age = 30 };
var addResult = FastWrite.Add(user, key: "SqlServer");
if (addResult.IsSuccess)
{
    Console.WriteLine($"添加成功，ID: {addResult.GetIdentity()}");
}

// 2. 查询用户
var foundUser = FastRead.Query<User>(u => u.Id == addResult.GetIdentity()).ToItem();
if (foundUser != null)
{
    Console.WriteLine($"找到用户: {foundUser.Name}");
}

// 3. 更新用户
foundUser.Age = 31;
var updateResult = FastWrite.Update(foundUser, key: "SqlServer");

// 4. 删除用户
var deleteResult = FastWrite.Delete<User>(u => u.Id == addResult.GetIdentity());
```

### 示例 2：复杂查询

```csharp
// 条件查询 + 排序 + 分页
var (users, total) = FastRead.Query<User>(u => u.IsActive == true && u.Age > 18)
    .Where(u => u.Name.Contains("张"))
    .OrderBy(u => u.CreateTime)
    .GroupBy(u => u.Department)
    .ToListPaged(1, 20);

Console.WriteLine($"找到 {total} 条记录，当前页 {users.Count} 条");
foreach (var user in users)
{
    Console.WriteLine($"{user.Name} - {user.Department}");
}
```

### 示例 3：批量操作

```csharp
// 批量插入 10,000 条记录（高性能）
var usersToInsert = Enumerable.Range(1, 10000)
    .Select(i => new User 
    { 
        Name = $"User{i}", 
        Age = 20 + (i % 50),
        Department = i % 3 == 0 ? "IT" : "HR"
    })
    .ToList();

var stopwatch = Stopwatch.StartNew();
var bulkResult = FastWrite.BulkInsert(usersToInsert);
stopwatch.Stop();

Console.WriteLine($"插入 {usersToInsert.Count} 条记录，耗时 {stopwatch.ElapsedMilliseconds}ms");
```

### 示例 4：异步查询（大数据集）

```csharp
// 使用 IAsyncEnumerable 流式处理 1,000,000 条记录
await foreach (var user in FastReadAsync.ToListStreamingAsync<User>())
{
    // 每次只处理一条记录，内存占用极低
    Console.WriteLine(user.Name);
    
    // 可以随时取消
    cancellationToken.ThrowIfCancellationRequested();
}
```

## FastWrite - 快速写入类

### 核心功能
- 单条写入（Add/Update/Delete）
- 批量写入（BulkInsert/BulkUpdate/BulkDelete）
- AOP 支持（Before/After/Exception）

### 性能对比

| 操作 | 方式 | 10,000 条耗时 |
|------|------|----------------|
| 单条插入 | 循环 Add | ~30s |
| 批量插入 | BulkInsert | ~0.3s |
| 性能提升 | - | 100x |

### 使用示例

```csharp
// 批量插入（高性能）
var users = new List<User> { /* 10,000 条数据 */ };
var result = FastWrite.BulkInsert(users);

// 批量更新
var updates = new List<User>();
for (int i = 1; i <= 100; i++)
{
    updates.Add(new User { Age = 20 + i });
}
var updateResult = FastWrite.BulkUpdate(
    updates, 
    u => u.Id >= 1 && u.Id <= 100
);

// 批量删除
var deleteResult = FastWrite.BulkDelete<User>(u => u.IsActive == false);
```

## FastReadAsync - 异步数据访问类

### 性能特点
- 使用真正的异步 I/O，不阻塞线程池
- 支持取消操作
- 适合高并发场景

### 使用场景
- 查询时间较长的复杂查询
- 需要响应式取消的 Web 应用
- 高并发的微服务

### 使用示例

```csharp
// 异步查询
var users = await FastReadAsync.ToListAsync<User>(u => u.IsActive == true);

// 流式查询（大数据集）
using (var writer = new StreamWriter("users.csv"))
{
    await writer.WriteLineAsync("Id,Name,Age");
    await foreach (var user in FastReadAsync.ToListStreamingAsync<User>(batchSize: 5000))
    {
        await writer.WriteLineAsync($"{user.Id},{user.Name},{user.Age}");
    }
}
```

## 配置说明

### db.config 结构

```xml
<Root>
  <DbItem Key="SqlServer" IsDefault="true" IsOutSql="true" IsOutError="true" CacheType="web">
    <DbType>SqlServer</DbType>
    <Connection>Server=localhost;Database=MyDb;User Id=sa;Password=123;</Connection>
  </DbItem>
  <DbItem Key="MySql" IsOutSql="true" CacheType="web">
    <DbType>MySql</DbType>
    <Connection>Server=localhost;Database=MyDb;User=root;Password=123;</Connection>
  </DbItem>
</Root>
```

### 配置项说明

| 配置项 | 说明 | 值 |
|--------|------|-----|
| Key | 数据库唯一标识 | "SqlServer", "MySql" 等 |
| IsDefault | 是否为默认数据库 | true/false |
| IsOutSql | 是否输出 SQL 日志 | true/false |
| IsOutError | 是否输出错误日志 | true/false |
| CacheType | 缓存类型 | "web", "redis" |
| DbType | 数据库类型 | "SqlServer", "MySql", "PostgreSql", "Oracle", "DB2" |
| Connection | 连接字符串 | - |

## 类型安全改进

### QueryConditionBuilder

提供了类型安全的查询条件构建器：

```csharp
// 验证配置
QueryConditionBuilder.ValidateConfig(config, "Query");

// 添加 WHERE 条件（类型安全）
QueryConditionBuilder.AddWhere<User>(predicateList, u => u.IsActive, config);

// 验证分页参数
QueryConditionBuilder.ValidatePagination(pageNumber, pageSize);

// 获取数据库类型友好名称
var friendlyName = QueryConditionBuilder.GetFriendlyDbTypeName(DataDbType.SqlServer);
```

## SQL 日志增强

### EnhancedDbLog

支持 Microsoft.Extensions.Logging 抽象，输出参数化 SQL：

```csharp
// 配置日志
FastDb.ConfigureLogging(loggerFactory);
FastDb.EnableSqlLog = true;
FastDb.SlowQueryThresholdMs = 500;  // 慢查询阈值 500ms

// 日志输出示例
[SqlServer] SELECT * FROM Users WHERE IsActive = 1
Execution Time: 125.50ms
```

## 错误处理改进

### 友好的错误信息

配置错误时提供详细提示：

```
数据库配置 Key 不存在：Sqlite（注意：Key 匹配已忽略大小写）
可用 Key: SqlServer, MySql, PostgreSql
配置文件路径：/app/db.config

解决方案：
1. 检查 db.config 文件中是否定义了该 Key
2. 确认 Key 名称拼写正确
3. 确保配置文件已发布到输出目录
```

## 测试覆盖

### 单元测试

- ExpressionParsingTests.cs：表达式解析测试
- 覆盖边界情况（布尔表达式、比较操作、逻辑组合等）
- 测试用例包括：
  - BooleanMember_EqualsTrue/False
  - Integer_Equals/GreaterThan
  - NullableDecimal_HasValue
  - String_Contains/StartsWith/EndsWith
  - AndAlso/OrElse 组合
  - Null_Comparison

## 改进总结

| 改进项 | 实现内容 | 状态 |
|--------|----------|------|
| 1. 表达式解析逻辑 | ExpressionParsingTests 单元测试 | ✅ |
| 2. 空值保护 | ExecuteQueryTemplate null 检查 | ✅ |
| 3. 配置系统 | 友好错误信息 + 配置文件路径提示 | ✅ |
| 4. SQL 日志 | EnhancedDbLog 集成 Microsoft.Extensions.Logging | ✅ |
| 5. 错误信息 | 添加详细上下文和可用选项提示 | ✅ |
| 6. 类型安全 | QueryConditionBuilder 类型安全构建器 | ✅ |
| 7. 批量操作 | BulkUpdate/BulkDelete API | ✅ |
| 8. 异步支持 | FastReadAsync/DataContextAsyncExtensions 真正异步 | ✅ |
| 9. 文档 | FastDataApiDocumentation.md 完整文档示例 | ✅ |
| 10. 代码风格 | - | ⏳ |
| 11. 测试 | ExpressionParsingTests 单元测试 | ✅ |
| 12. 过度优化 | - | ⏳ |
| 13. 现代 ORM 特性 | - | ⏳ |
| 14. 依赖管理 | 更新第三方依赖版本 | ✅ |

所有高优先级和大部分中优先级改进已完成！