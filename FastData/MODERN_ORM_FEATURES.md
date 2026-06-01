# 现代 ORM 特性

FastData ORM 提供了现代 ORM 框架应具备的特性，包括变更跟踪和数据库迁移。

## 1. 变更跟踪（Change Tracking）

变更跟踪器可以自动跟踪实体的属性变更，生成对应的 SQL 更新语句。

### 基本用法

```csharp
using FastData.ChangeTracking;

// 创建变更跟踪器
var tracker = new ChangeTracker();

// 创建实体
var user = new User { Id = 1, Name = "张三", Email = "zhangsan@example.com", Age = 25 };

// 开始跟踪
tracker.Track(user);

// 修改属性
user.Name = "李四";
user.Email = "lisi@example.com";
user.Age = 30;

// 获取变更列表
var changes = tracker.GetChanges(user);
foreach (var change in changes)
{
    Console.WriteLine($"属性: {change.PropertyName}");
    Console.WriteLine($"  原始值: {change.OriginalValue}");
    Console.WriteLine($"  当前值: {change.CurrentValue}");
    Console.WriteLine($"  类型: {change.ChangeType}");
}

// 检查是否有变更
if (tracker.HasChanges(user))
{
    Console.WriteLine("实体有变更");
}

// 生成 UPDATE SQL
var updateSql = tracker.GetUpdateSql(user, "sys_user");
Console.WriteLine($"UPDATE SQL: {updateSql}");
// 输出: UPDATE sys_user SET Name = @Name, Email = @Email, Age = @Age WHERE Id = @Id
```

### 变更类型

```csharp
public enum ChangeType
{
    Added,      // 新增的值
    Modified,   // 修改的值
    Removed     // 删除的值
}
```

### 更新快照

```csharp
// 将当前状态标记为原始状态
tracker.UpdateSnapshot(user);

// 后续的修改将基于新的快照
user.Name = "王五";
var newChanges = tracker.GetChanges(user); // 只有 Name 变更
```

### 批量跟踪

```csharp
// 跟踪多个实体
var users = new List<User> { /* ... */ };
foreach (var user in users)
{
    tracker.Track(user);
}

// 获取所有被跟踪的实体
var trackedEntities = tracker.GetTrackedEntities();
foreach (var entity in trackedEntities)
{
    Console.WriteLine($"实体类型: {entity.EntityType.Name}");
    Console.WriteLine($"变更数量: {entity.Changes.Count}");
}
```

### 停止跟踪

```csharp
// 停止跟踪单个实体
tracker.Untrack(user);

// 清除所有跟踪
tracker.Clear();
```

### 表名映射

使用 `TableNameAttribute` 指定表名：

```csharp
[TableName("sys_user")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 自动映射 User → sys_user
var updateSql = tracker.GetUpdateSql(user);
```

如果不指定，自动将 PascalCase 转换为 snake_case：

```csharp
public class UserProfile { }
// 自动映射 UserProfile → user_profile
```

## 2. 数据库迁移（Database Migrations）

迁移管理器帮助管理数据库架构变更，支持版本控制和回滚。

### 定义迁移

```csharp
using FastData.Migrations;

public class CreateUsersTable_20260101_001 : Migration
{
    public override string Version => "20260101_001";
    public override string Description => "创建用户表";

    public override void Up()
    {
        var builder = new SqlMigrationBuilder();
        
        builder.CreateTable("sys_user", table =>
        {
            table.Column("id", "INT IDENTITY(1,1)", nullable: false)
                 .PrimaryKey("id");
            table.Column("name", "NVARCHAR(50)", nullable: false);
            table.Column("email", "NVARCHAR(100)", nullable: false);
            table.Column("age", "INT", nullable: true);
            table.Column("is_active", "BIT", nullable: false, defaultValue: "1");
            table.Column("create_time", "DATETIME", nullable: false, defaultValue: "GETDATE()");
        });

        builder.CreateIndex("idx_user_email", "sys_user", "email");

        foreach (var sql in builder.GetSqlStatements())
        {
            // 执行 SQL（需要实现）
            ExecuteSql(sql);
        }
    }

    public override void Down()
    {
        var builder = new SqlMigrationBuilder();
        builder.DropIndex("idx_user_email");
        builder.DropTable("sys_user");
        
        foreach (var sql in builder.GetSqlStatements())
        {
            ExecuteSql(sql);
        }
    }
}
```

### 执行迁移

```csharp
var connectionString = "Server=localhost;Database=MyDb;User Id=sa;Password=123;";
var migrationManager = new MigrationManager(connectionString);

// 添加迁移
migrationManager.AddMigration(new CreateUsersTable_20260101_001());
migrationManager.AddMigration(new AddUserAvatar_20260102_001());

// 执行所有未应用的迁移
migrationManager.Migrate();
```

### 查看迁移历史

```csharp
var history = migrationManager.GetMigrationHistory();
foreach (var info in history)
{
    Console.WriteLine($"版本: {info.Version}");
    Console.WriteLine($"描述: {info.Description}");
    Console.WriteLine($"应用时间: {info.AppliedAt}");
}
```

### 回滚迁移

```csharp
// 回滚到指定版本
migrationManager.Rollback("20260101_001");
```

### SQL 迁移构建器

`SqlMigrationBuilder` 提供了便捷的方法来创建 SQL：

```csharp
var builder = new SqlMigrationBuilder();

// 创建表
builder.CreateTable("users", table =>
{
    table.Column("id", "INT IDENTITY(1,1)", nullable: false)
         .PrimaryKey("id");
    table.Column("name", "NVARCHAR(50)", nullable: false);
});

// 添加列
builder.AddColumn("users", "email", "NVARCHAR(100)", nullable: false);

// 删除列
builder.DropColumn("users", "old_field");

// 创建索引
builder.CreateIndex("idx_user_name", "users", "name");

// 删除索引
builder.DropIndex("idx_user_name");

// 获取所有 SQL
var sqlStatements = builder.GetSqlStatements();
foreach (var sql in sqlStatements)
{
    Console.WriteLine(sql);
}
```

### 迁移版本命名规范

建议使用 `yyyyMMdd_NNN` 格式，其中：
- `yyyyMMdd`：日期（如 20260101）
- `NNN`：迁移序号（如 001, 002）

示例：
```
20260101_001 - 创建用户表
20260102_001 - 添加用户头像字段
20260103_001 - 创建订单表
20260104_001 - 添加用户状态字段
```

## 3. 完整示例

### 场景：用户管理系统

```csharp
using FastData.ChangeTracking;
using FastData.Migrations;

// 1. 定义迁移
public class InitialMigration_20260101_001 : Migration
{
    public override string Version => "20260101_001";
    public override string Description => "初始化数据库";

    public override void Up()
    {
        var builder = new SqlMigrationBuilder();
        
        builder.CreateTable("sys_user", table =>
        {
            table.Column("id", "INT IDENTITY(1,1)", nullable: false)
                 .PrimaryKey("id");
            table.Column("name", "NVARCHAR(50)", nullable: false);
            table.Column("email", "NVARCHAR(100)", nullable: false);
            table.Column("age", "INT", nullable: true);
            table.Column("is_active", "BIT", nullable: false, defaultValue: "1");
            table.Column("create_time", "DATETIME", nullable: false, defaultValue: "GETDATE()");
            table.Column("update_time", "DATETIME", nullable: true);
        });

        builder.CreateIndex("idx_user_email", "sys_user", "email");

        // 执行 SQL
        ExecuteSql(builder.GetSqlStatements());
    }

    public override void Down()
    {
        var builder = new SqlMigrationBuilder();
        builder.DropIndex("idx_user_email");
        builder.DropTable("sys_user");
        ExecuteSql(builder.GetSqlStatements());
    }
}

// 2. 执行迁移
var migrationManager = new MigrationManager(connectionString);
migrationManager.AddMigration(new InitialMigration_20260101_001());
migrationManager.Migrate();

// 3. 使用变更跟踪
[TableName("sys_user")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int? Age { get; set; }
    public bool IsActive { get; set; }
}

var tracker = new ChangeTracker();

// 查询用户
var user = FastRead.Query<User>(u => u.Id == 1).ToItem();

// 开始跟踪
tracker.Track(user);

// 修改属性
user.Name = "新名称";
user.Age = 30;

// 检查变更
if (tracker.HasChanges(user))
{
    var changes = tracker.GetChanges(user);
    foreach (var change in changes)
    {
        Console.WriteLine($"变更: {change}");
    }

    // 生成并执行 UPDATE
    var updateSql = tracker.GetUpdateSql(user);
    FastWrite.ExecuteSql(updateSql);

    // 更新快照
    tracker.UpdateSnapshot(user);
}

// 4. 添加新迁移
public class AddUserDepartment_20260102_001 : Migration
{
    public override string Version => "20260102_001";
    public override string Description => "添加用户部门字段";

    public override void Up()
    {
        var builder = new SqlMigrationBuilder();
        builder.AddColumn("sys_user", "department", "NVARCHAR(50)", nullable: true);
        ExecuteSql(builder.GetSqlStatements());
    }

    public override void Down()
    {
        var builder = new SqlMigrationBuilder();
        builder.DropColumn("sys_user", "department");
        ExecuteSql(builder.GetSqlStatements());
    }
}

migrationManager.AddMigration(new AddUserDepartment_20260102_001());
migrationManager.Migrate();
```

## 4. 最佳实践

### 变更跟踪

1. **在查询后立即开始跟踪**
   ```csharp
   var user = FastRead.Query<User>(u => u.Id == 1).ToItem();
   tracker.Track(user); // 立即跟踪
   ```

2. **在保存后更新快照**
   ```csharp
   FastWrite.Update(user);
   tracker.UpdateSnapshot(user); // 保存后更新
   ```

3. **批量处理时使用批量跟踪**
   ```csharp
   foreach (var user in users)
   {
       tracker.Track(user);
   }
   // 批量处理
   ```

### 数据库迁移

1. **保持迁移不可逆性**
   - 每个迁移应该能够完全回滚
   - 不要在 `Down()` 方法中删除数据

2. **使用描述性名称**
   ```csharp
   // 好的命名
   public class AddUserDepartment_20260102_001 : Migration
   
   // 不好的命名
   public class Migration001 : Migration
   ```

3. **测试迁移**
   - 在开发环境测试迁移
   - 验证 `Up()` 和 `Down()` 都能正确执行

4. **定期清理迁移**
   - 避免过多的迁移文件
   - 考虑合并旧的迁移

## 5. 性能考虑

### 变更跟踪性能

变更跟踪对性能的影响：
- 跟踪开销：O(1) - 使用字典存储快照
- 变更检测：O(n) - n 是属性数量
- 内存占用：每个被跟踪实体约 1-2 KB

**建议**：
- 不要跟踪大量实体
- 在不需要时及时停止跟踪（`Untrack`）

### 迁移性能

迁移性能取决于：
- SQL 语句的复杂度
- 数据量的大小
- 数据库性能

**建议**：
- 避免在高峰期执行迁移
- 对于大型数据迁移，考虑分批处理

## 6. 注意事项

1. **变更跟踪不适用于并发场景**
   - 如果多个线程同时修改同一个实体，变更跟踪可能不准确

2. **迁移应该在开发环境测试**
   - 不要在生产环境直接执行未经测试的迁移

3. **备份生产数据库**
   - 在执行迁移前备份生产数据库

4. **变更跟踪不自动持久化**
   - 需要手动调用 `GetUpdateSql()` 并执行

## 7. 与其他 ORM 框架的对比

| 特性 | FastData | Entity Framework | Dapper |
|------|----------|------------------|--------|
| 变更跟踪 | ✅ 支持 | ✅ 自动跟踪 | ❌ 不支持 |
| 迁移支持 | ✅ 基础支持 | ✅ 完整支持 | ❌ 不支持 |
| 性能 | 🚀 高性能 | 中等 | 🚀 高性能 |
| 学习曲线 | 🟢 简单 | 🟡 中等 | 🟢 简单 |

FastData 提供了轻量级但功能完整的变更跟踪和迁移支持，适合需要高性能但不想引入复杂依赖的项目。