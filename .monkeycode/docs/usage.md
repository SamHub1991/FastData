# FastData 中文使用说明

本文档面向 FastData 使用者，覆盖安装、配置、初始化、查询写入、多数据库切换、Repository、AOP 和 XML Map SQL。

## 1. 安装

通过 NuGet 安装：

```powershell
Install-Package Fast.Data
```

通过 .NET CLI 安装：

```bash
dotnet add package Fast.Data
```

## 2. 数据库配置

在 `web.config` 或 `db.config` 中注册配置节：

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>
```

推荐使用统一 `Connections` 配置：

```xml
<DataConfig Default="DefaultDb">
  <Connections>
    <Add
      Provider="SqlServer"
      Key="DefaultDb"
      ConnStr="server=.;database=demo;uid=sa;pwd=123456"
      IsDefault="true"
      IsOutSql="true"
      IsOutError="true"
      DesignModel="DbFirst"
      SqlErrorType="db"
      CacheType="web" />

    <Add
      Provider="MySql"
      Key="ReportDb"
      ConnStr="server=127.0.0.1;database=report;uid=root;pwd=123456"
      IsOutSql="true"
      IsOutError="true"
      DesignModel="DbFirst"
      SqlErrorType="file"
      CacheType="web" />
  </Connections>
</DataConfig>
```

`Provider` 可填写数据库类型名，也可填写 provider invariant name。常用数据库类型名包括：

- `SqlServer`
- `MySql`
- `Oracle`
- `SQLite`
- `PostgreSql`
- `DB2`

旧版分组配置继续可用：

```xml
<DataConfig>
  <Oracle>
    <Add ConnStr="connstr" Key="OraDb" IsOutSql="true" IsOutError="true" DesignModel="DbFirst" SqlErrorType="db" CacheType="web" />
  </Oracle>
  <MySql>
    <Add ConnStr="connstr" Key="MyDb" IsOutSql="true" IsOutError="true" DesignModel="DbFirst" SqlErrorType="db" CacheType="web" />
  </MySql>
  <SqlServer>
    <Add ConnStr="connstr" Key="SqlDb" IsOutSql="true" IsOutError="true" DesignModel="DbFirst" SqlErrorType="db" CacheType="web" />
  </SqlServer>
</DataConfig>
```

## 3. 初始化

在应用启动时缓存模型属性：

```csharp
FastMap.InstanceProperties("Your.Model.Namespace", "db.config", new TestAop());
```

初始化 Map 缓存：

```csharp
FastMap.InstanceMap("DefaultDb", "SqlMap.config", "db.config", new TestAop());
```

使用嵌入式资源初始化 Map 缓存：

```csharp
FastData.FastMap.InstanceMapResource("DefaultDb", "db.config", "SqlMap.config", new TestAop());
```

初始化 Redis 配置：

```csharp
FastRedis.RedisInfo.Init("db.config");
```

## 4. 查询

默认库查询：

```csharp
var query = FastRead.Query<User>(a => a.IsEnabled == true);
```

指定数据库 Key 查询：

```csharp
var query = FastRead.Query<User>(a => a.IsEnabled == true, key: "ReportDb");
```

绑定数据库 Key 查询：

```csharp
var query = FastRead.Use("ReportDb").Query<User>(a => a.IsEnabled == true);
```

执行 SQL 查询：

```csharp
var list = FastRead.ExecuteSql<User>(
    "select * from Users where IsEnabled = @IsEnabled",
    new[] { new SqlParameter("@IsEnabled", true) });
```

## 5. 写入

默认库新增：

```csharp
var result = FastWrite.Add(new User
{
    UserName = "admin",
    IsEnabled = true
});
```

指定数据库 Key 新增：

```csharp
var result = FastWrite.Add(new User
{
    UserName = "report-user",
    IsEnabled = true
}, key: "ReportDb");
```

绑定数据库 Key 写入：

```csharp
var result = FastWrite.Use("ReportDb").Update(
    new User { IsEnabled = false },
    a => a.UserName == "report-user");
```

执行 SQL 写入：

```csharp
var result = FastWrite.ExecuteSql(
    "update Users set IsEnabled = @IsEnabled where UserName = @UserName",
    new[]
    {
        new SqlParameter("@IsEnabled", false),
        new SqlParameter("@UserName", "admin")
    });
```

## 6. 作用域数据库切换

`FastDb.Use(key)` 会在当前执行上下文中设置数据库 Key，作用域结束后自动恢复原 Key：

```csharp
using (FastDb.Use("ArchiveDb"))
{
    var users = FastRead.Query<User>(a => a.IsEnabled == true);
    var result = FastWrite.Add(new User { UserName = "archive-user" });
}
```

该方式适合批量处理同一个数据库连接下的多次读写操作。

## 7. Repository

直接注册 Repository：

```csharp
services.AddTransient<IFastRepository, FastRepository>();
services.AddTransient<IRedisRepository, RedisRepository>();
```

注册 Repository 工厂：

```csharp
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();
```

使用工厂获取默认库和指定库 Repository：

```csharp
public class ReportService
{
    private readonly IFastRepositoryFactory factory;

    public ReportService(IFastRepositoryFactory factory)
    {
        this.factory = factory;
    }

    public void Execute()
    {
        var defaultRepository = factory.Default();
        var reportRepository = factory.Use("ReportDb");
    }
}
```

## 8. AOP 扩展

实现 `IFastAop` 后可在查询、写入、Map 执行前后处理日志、审计、异常等逻辑：

```csharp
using FastData.Aop;
using System;

public class TestAop : IFastAop
{
    public void Before(BeforeContext context)
    {
    }

    public void After(AfterContext context)
    {
    }

    public void MapBefore(MapBeforeContext context)
    {
    }

    public void MapAfter(MapAfterContext context)
    {
    }

    public void Exception(Exception ex, string name)
    {
    }
}
```

## 9. XML Map SQL

`SqlMap.config` 示例：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="MapConfig" type="FastData.Config.MapConfig,FastData" />
  </configSections>

  <MapConfig>
    <SqlMap>
      <Add File="map/map.xml" />
    </SqlMap>
  </MapConfig>
</configuration>
```

`map/map.xml` 示例：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap>
  <select id="User.GetList" log="true">
    select a.*
    from base_user a
    <dynamic prepend=" where 1=1">
      <isPropertyAvailable prepend=" and " property="userId">a.userId = ?userId</isPropertyAvailable>
      <isEqual compareValue="5" prepend=" and " property="userName">a.userName = ?userName</isEqual>
      <isNotEqual compareValue="5" prepend=" and " property="fullName">a.fullName = ?fullName</isNotEqual>
      <isGreaterThan compareValue="5" prepend=" and " property="orgId">a.orgId = ?orgId</isGreaterThan>
      <isLessThan compareValue="5" prepend=" and " property="userNo">a.userNo = ?userNo</isLessThan>
      <isNullOrEmpty prepend=" and " property="roleId">a.roleId = ?roleId</isNullOrEmpty>
      <isNotNullOrEmpty prepend=" and " property="isAdmin">a.isAdmin = ?isAdmin</isNotNullOrEmpty>
      <choose property="userNo">
        <condition prepend=" and " property="userNo&gt;5">a.userNo = :userNo and a.userNo = 5</condition>
        <other prepend=" and ">a.userNo = :userNo and a.userNo = 6</other>
      </choose>
    </dynamic>
  </select>
</sqlMap>
```

调用 Map SQL：

```csharp
var param = new List<OracleParameter>
{
    new OracleParameter
    {
        ParameterName = "userid",
        Value = "dd5c99f2-0892-4179-83db-c2ccf243104c"
    }
};

var users = FastMap.Query<UserResult>("User.GetList", param.ToArray(), null, "DefaultDb");
```

## 10. 兼容说明

- 原有 `FastRead.Query(..., key)`、`FastWrite.Add(..., key)` 等传 Key 写法继续可用。
- 原有按数据库类型分组的配置节点继续可用。
- 新增 `Connections` 配置用于简化多数据库配置。
- 新增默认库机制后，未传 Key 时优先使用 `Default` 或 `IsDefault="true"` 指定的连接。
- 多个连接未指定默认库时，使用配置中的第一个可用连接。

## 11. Model 生成工具

项目：`FastData.ModelGenerator.WinForms`

当前工具支持：

- 选择数据库 Provider。
- 输入连接字符串并测试连接。
- 加载数据表。
- 按表名搜索过滤。
- 多选数据表。
- 设置默认命名空间。
- 设置单表命名空间覆盖。
- 选择数据表后预览字段、数据库类型、是否可空和主键信息。
- 预览生成的 Model 代码。
- 输出 `.cs` Model 文件。

入口文件：`FastData.ModelGenerator.WinForms/Program.cs`

核心复用能力来自 `FastData.Tooling`：

- `IDatabaseMetadataReader`
- `MetadataReaderFactory`
- `ModelCodeGenerator`

## 12. 数据同步工具

项目：`FastData.SyncTool.WinForms`

当前工具支持：

- 配置源库 Provider 和连接字符串。
- 配置目标库 Provider 和连接字符串。
- 配置中间库 Provider 和连接字符串。
- 配置同步任务 ID。
- 配置源表、目标表、批量大小和失败重试次数。
- 配置增量字段和增量起点，用于生成基础增量同步查询。
- 自动创建中间库表。
- 恢复中间库中的失败记录。
- 选择是否清理中间库成功记录。
- 按目标库 Provider 导出 SQL Server、MySQL 或 Oracle 中间库 SQL。
- 执行基础全量同步。
- 执行基础增量同步。
- 查看运行日志和错误信息。

入口文件：`FastData.SyncTool.WinForms/Program.cs`

核心复用能力来自 `FastData.Tooling`：

- `IntermediateSchemaBuilder`
- `DataSyncService`
- `DataSyncOptions`
- `DataSyncResult`

## 13. FAQ

### 未传数据库 Key 时使用哪个连接？

优先使用 `DataConfig Default="..."` 指定的连接，其次使用 `IsDefault="true"` 的连接，最后使用配置中的第一个可用连接。

### 旧版 Oracle、MySql、SqlServer 分组配置还能用吗？

可以继续使用。统一 `Connections` 配置是推荐写法，旧版分组配置保留兼容读取。

### `Provider` 应该填写什么？

可以填写数据库类型名，例如 `SqlServer`、`MySql`、`Oracle`，也可以填写 provider invariant name，例如 `System.Data.SqlClient`、`MySql.Data.MySqlClient`、`Oracle.ManagedDataAccess.Client`。

### 如何让一段业务代码都使用同一个数据库？

使用 `FastDb.Use(key)` 包裹代码块：

```csharp
using (FastDb.Use("ReportDb"))
{
    var list = FastRead.Query<User>(a => a.IsEnabled == true);
    var result = FastWrite.Add(new User { UserName = "report-user" });
}
```

### 如何只给一次查询或写入指定数据库？

使用绑定写法：

```csharp
var list = FastRead.Use("ReportDb").Query<User>(a => a.IsEnabled == true);
var result = FastWrite.Use("ReportDb").Add(new User { UserName = "report-user" });
```

### Model 生成工具生成的文件放在哪里？

工具默认输出到程序目录下的 `Models` 文件夹，也可以在界面中修改输出目录。

### 数据同步工具的增量同步如何配置？

填写增量字段和增量起点后，工具会生成 `where 增量字段 > 增量起点` 的基础增量查询。增量字段建议使用自增主键、更新时间或单调递增版本号。

### 失败记录如何恢复？

配置中间库连接并勾选“恢复失败记录”后，工具会读取 `fd_sync_record` 中 `status = 'Failed'` 的记录，重新写入目标表，成功后标记为 `Success`。

### 中间库清理会清理哪些数据？

勾选“清理中间库成功记录”后，工具会清理 `fd_sync_record` 和 `fd_sync_batch` 中状态为 `Success` 的记录。

---

## 故障排查

### 构建失败：COM 注册错误

**症状**：构建时报错 `error MSB4044: The "RegisterForComInterop" task was not given a value for the required parameter`

**解决方案**：添加 `/p:RegisterForComInterop=false` 构建参数
```bash
dotnet build FastData.sln /p:RegisterForComInterop=false
```

### 构建失败：.NET Framework 路径错误

**症状**：构建时报错 `The reference assemblies for .NETFramework,Version=v4.5 were not found`

**解决方案**：设置 `FrameworkPathOverride` 环境变量
```bash
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" dotnet build
```

### 构建失败：全球化设置错误

**症状**：Linux 环境构建时出现 `System.Globalization.CultureNotFoundException`

**解决方案**：设置全球化不变模式
```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build
```

### 配置错误：找不到数据库 Key

**症状**：运行时提示 `未找到指定的数据库 Key`

**解决方案**：
1. 检查 `db.config` 中 `Connections` 配置节点是否包含指定 Key
2. 确认 `Default` 属性配置正确
3. 错误提示会列出所有可用 Key

### 同步失败：中间库表不存在

**症状**：同步工具提示 `表 fd_sync_batch 不存在`

**解决方案**：
1. 点击"导出创建中间库脚本"按钮
2. 在中间库执行生成的 SQL 脚本
3. 或勾选"自动创建中间库表"后重新同步

### 连接失败：Provider 不匹配

**症状**：测试连接时提示 `Unable to find the requested .NET Framework DataProvider`

**解决方案**：
1. 安装对应数据库 provider 包
   - SQL Server: `System.Data.SqlClient` (内置)
   - MySQL: `MySql.Data` NuGet 包
   - Oracle: `Oracle.ManagedDataAccess` NuGet 包
2. 确认 `db.config` 中 `Provider` 属性值与安装的 provider 一致
