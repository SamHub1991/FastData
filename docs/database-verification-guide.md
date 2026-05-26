# FastData 数据库验证指南

## 本环境可用数据库

### SQLite (立即可用)
- **文件位置**: `/tmp/fastdata_test.db`
- **测试表**: `Users` (Id, Name, Email, CreateTime)
- **测试数据**: 3 条记录

使用场景：
- 测试 ORM 基本功能
- 测试 Repository 层 API
- 测试 Model 生成工具

## 远程测试方案

如果有外部数据库服务器，可以配置连接进行验证：

### SQL Server
```xml
<!-- app.config -->
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Server=localhost;Database=FastDataTest;User Id=sa;Password=FastData@Test123;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

### MySQL
```xml
<configuration>
  <connectionStrings>
    <add name="MySqlConnection" 
         connectionString="Server=localhost;Database=testdb;Uid=fastdata;Pwd=FastData@Test123;"
         providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
</configuration>
```

### PostgreSQL
```xml
<configuration>
  <connectionStrings>
    <add name="PostgreConnection" 
         connectionString="Host=localhost;Database=testdb;Username=fastdata;Password=FastData@Test123;"
         providerName="Npgsql" />
  </connectionStrings>
</configuration>
```

## 自动化测试脚本

在 `FastData.Tests` 项目中可以添加集成测试：

```csharp
[TestMethod]
public void Test_Sqlite_Connection()
{
    var connectionString = "Data Source=/tmp/fastdata_test.db";
    using (var connection = new System.Data.SQLite.SQLiteConnection(connectionString))
    {
        connection.Open();
        Assert.IsTrue(connection.State == System.Data.ConnectionState.Open);
    }
}

[TestMethod]
public void Test_FastRead_Query()
{
    FastDb.Use("DefaultConnection");
    var users = FastRead.Query<User>("SELECT * FROM Users WHERE Id = @Id", 
        new[] { new SqlParameter("@Id", 1) });
    Assert.AreEqual(1, users.Count);
}
```

## 验证检查清单

### 7.1 代码质量修复验证
- [x] AsyncHelper 异步包装模式
- [x] 批量插入优化（InsertRowBatch）
- [ ] 端到端数据同步（需要 2 个数据库）
- [ ] 失败重试机制（需要真实网络环境）

### 7.2 可测试性改进验证
- [x] IDataSyncService 接口定义
- [x] DateTimeProvider 可测试时间抽象
- [ ] 依赖注入（MainForm 重构）

### 7.3 代码可读性验证
- [x] DatabaseProviderMappings 统一映射
- [x] Provider 常量类替代魔法字符串
- [x] FastData/FastData.Tooling/FastData.SyncTool.WinForms 统一引用

## 推荐的 Docker 测试环境

如果在本地开发环境，可以使用以下 docker-compose 配置：

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=FastData@Test123
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  mysql:
    image: mysql:8.0
    environment:
      - MYSQL_ROOT_PASSWORD=FastData@Test123
      - MYSQL_DATABASE=testdb
      - MYSQL_USER=fastdata
      - MYSQL_PASSWORD=FastData@Test123
    ports:
      - "3306:3306"

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_PASSWORD=FastData@Test123
      - POSTGRES_USER=fastdata
      - POSTGRES_DB=testdb
    ports:
      - "5432:5432"

volumes:
  sqlserver_data:
```

运行：
```bash
docker-compose up -d
# 等待所有服务启动后（约 2 分钟）
# 即可进行完整的端到端测试
```

## 当前环境限制

1. **Docker 网络问题**: 无法从 Docker Hub 拉取新镜像
2. **SQL Server 内存限制**: SQL Server 2019 需要约 2GB 内存
3. **权限问题**: 容器需要 root 权限创建数据目录

## 建议

在拥有以下条件的完整开发环境中进行最终验证：
1. 充足的内存（至少 4GB）
2. 稳定的 Docker Hub 网络访问
3. 或者本地已下载好的数据库镜像

当前已完成所有无需数据库的代码质量改进。
