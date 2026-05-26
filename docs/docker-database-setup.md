# Docker 数据库测试环境配置

## 已启动的数据库

### MySQL 8.0
- **主机**: localhost
- **端口**: 3306
- **数据库**: testdb
- **用户名**: fastdata
- **密码**: FastData@Test123
- **连接字符串**: `Server=localhost;Database=testdb;Uid=fastdata;Pwd=FastData@Test123;`
- **Provider**: `MySql.Data.MySqlClient`

### PostgreSQL 15
- **主机**: localhost
- **端口**: 5432
- **数据库**: testdb
- **用户名**: fastdata
- **密码**: FastData@Test123
- **连接字符串**: `Host=localhost;Database=testdb;Username=fastdata;Password=FastData@Test123`
- **Provider**: `Npgsql`

### SQLite
- **文件**: `/tmp/fastdata_test.db`
- **连接字符串**: `Data Source=/tmp/fastdata_test.db;Version=3;`
- **Provider**: `System.Data.SQLite`

## FastData 配置示例

### 使用 app.config 配置

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="fastData" type="FastData.Config.FastDataConfigurationSection, FastData"/>
  </configSections>
  
  <fastData>
    <connections>
      <add name="mysql" 
           provider="MySql.Data.MySqlClient" 
           connectionString="Server=localhost;Database=testdb;Uid=fastdata;Pwd=FastData@Test123;"/>
      <add name="postgres" 
           provider="Npgsql" 
           connectionString="Host=localhost;Database=testdb;Username=fastdata;Password=FastData@Test123"/>
      <add name="sqlite" 
           provider="System.Data.SQLite" 
           connectionString="Data Source=/tmp/fastdata_test.db;Version=3;"/>
    </connections>
  </fastData>
</configuration>
```

### 使用代码配置

```csharp
using FastData.Database;

// 配置 MySQL
var mysqlConfig = new DatabaseConnectionOptions
{
    Name = "mysql",
    Provider = Provider.MySql,
    ConnectionString = "Server=localhost;Database=testdb;Uid=fastdata;Pwd=FastData@Test123;"
};

// 配置 PostgreSQL
var postgresConfig = new DatabaseConnectionOptions
{
    Name = "postgres",
    Provider = "Npgsql",
    ConnectionString = "Host=localhost;Database=testdb;Username=fastdata;Password=FastData@Test123"
};
```

## 测试验证脚本

### 1. 测试数据库连接

```csharp
using FastData;
using FastData.Repository;

// 测试 MySQL 连接
FastDb.Use("mysql");
var mysqlUsers = FastRead.Query<SqliteTestModel>("SELECT * FROM users");
Console.WriteLine($"MySQL: {mysqlUsers.Count} 条记录");

// 测试 PostgreSQL 连接
FastDb.Use("postgres");
var postgresUsers = FastRead.Query<SqliteTestModel>("SELECT * FROM users");
Console.WriteLine($"PostgreSQL: {postgresUsers.Count} 条记录");
```

### 2. 测试多数据库切换

```csharp
// 在 MySQL 中查询
FastDb.Use("mysql");
var mysqlResult = FastRead.ExecuteSql("SELECT COUNT(*) FROM users");

// 切换到 PostgreSQL
FastDb.Use("postgres");
var postgresResult = FastRead.ExecuteSql("SELECT COUNT(*) FROM users");
```

### 3. 测试 ORM 操作

```csharp
using FastData.Repository;

var repo = new FastRepository();

// 插入数据到 MySQL
FastDb.Use("mysql");
repo.Add(new SqliteTestModel { Name = "测试用户", Email = "test@example.com" });

// 查询数据
var users = repo.Query<SqliteTestModel>("SELECT * FROM users WHERE Name = @Name", 
    new[] { new DbParameter("@Name", "测试用户") });
```

### 4. 测试批量插入优化

```csharp
using FastData.Tooling.Sync;

var service = new DataSyncService();

var options = new DataSyncOptions
{
    SourceProvider = Provider.MySql,
    SourceConnectionString = "Server=localhost;Database=testdb;Uid=fastdata;Pwd=FastData@Test123;",
    TargetProvider = "Npgsql",
    TargetConnectionString = "Host=localhost;Database=testdb;Username=fastdata;Password=FastData@Test123",
    SourceTable = "users",
    TargetTable = "users",
    PrimaryKeyColumns = new[] { "id" },
    BatchSize = 500, // 批量插入大小
    AlwaysDeduplicate = true // 始终去重
};

var result = service.SyncTable(options);
Console.WriteLine($"同步完成：成功 {result.SuccessCount}, 失败 {result.FailCount}");
```

### 5. 测试可测试性改进

```csharp
using FastData.Abstractions;
using FastData.Tooling.Abstractions;

// 测试 DateTimeProvider
DateTimeProvider.Current = new TestableDateTimeProvider 
{ 
    Now = new DateTime(2026, 5, 26, 12, 0, 0) 
};
var fixedTime = DateTimeProvider.Now; // 2026-05-26 12:00:00

// 测试 IDataSyncService 接口（依赖注入）
IDataSyncService syncService = new DataSyncService(); // 实际应该通过 DI 容器获取
```

## 验证检查清单

### ✓ 代码质量改进验证

- [x] AsyncHelper 异步工具类
- [x] DateTimeProvider 可测试时间抽象
- [x] DatabaseProviderMappings 统一映射
- [x] Provider 常量类
- [x] IDataSyncService 接口定义

### ✓ 数据库环境验证

- [x] MySQL 8.0 连接测试
- [x] PostgreSQL 15 连接测试
- [x] SQLite 连接测试
- [x] 测试数据创建（各 3 条记录）

### 待验证（需要在项目中配置）

- [ ] 多数据库切换（FastDb.Use）
- [ ] ORM 基本操作（FastRead.Query, FastWrite.Add）
- [ ] 批量插入优化（InsertRowBatch）
- [ ] 端到端同步（MySQL → PostgreSQL）
- [ ] 失败重试机制
- [ ] Repository 模式

## Docker 命令参考

```bash
# 查看所有容器
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# 查看 MySQL 日志
docker logs mysql

# 查看 PostgreSQL 日志
docker logs postgres

# 进入 MySQL Shell
docker exec -it mysql mysql -uroot -p'FastData@Test123' testdb

# 进入 PostgreSQL Shell
docker exec -it postgres psql -U fastdata -d testdb

# 停止所有数据库
docker stop mysql postgres

# 重启数据库
docker start mysql postgres

# 清理并重建
docker rm -f mysql postgres
# 然后重新运行启动命令
```

## 故障排除

### SQL Server 问题
SQL Server 2019 在这个环境中遇到启动问题，可能原因：
1. 需要大量内存（默认 2GB 起步）
2. 持久化卷权限问题
3. 容器资源限制

**解决**: 当前使用 MySQL + PostgreSQL 进行验证，这两个数据库更轻量且已正常工作。

### 网络访问问题
如果无法访问数据库，检查：
```bash
# 查看容器端口
docker ps

# 测试端口连通性
nc -zv localhost 3306  # MySQL
nc -zv localhost 5432  # PostgreSQL
```

### 连接字符串错误
确保：
1. Provider 名称正确（参考 Provider 常量类）
2. 连接字符串格式正确
3. 数据库服务已启动

## 下一步行动

在完整开发环境中：
1. 配置 FastData 项目使用这些数据库
2. 运行完整的集成测试
3. 验证批量插入性能优化效果
4. 测试同步工具的端到端功能
