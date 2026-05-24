# FastData

FastData 是一个面向 .NET Framework 的轻量 ORM 组件，支持 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis 辅助能力和多数据库连接配置。

NuGet 地址：<https://www.nuget.org/packages/Fast.Data/>

## 功能特性

- 支持 Oracle、MySQL、SQL Server、SQLite、PostgreSQL、DB2。
- 支持默认数据库连接和按 Key 指定数据库连接。
- 支持 `FastRead`、`FastWrite` 静态入口。
- 支持 `FastRead.Use(key)`、`FastWrite.Use(key)` 绑定数据库 Key。
- 支持 `using (FastDb.Use(key))` 在当前执行上下文中切换数据库。
- 支持 `FastRepository` 和 `FastRepositoryFactory`。
- 支持 XML Map SQL 和动态 SQL 标签。
- 支持 AOP 事件扩展。
- 支持 Web 缓存和 Redis 缓存配置。

## 安装

通过 NuGet 安装：

```powershell
Install-Package Fast.Data
```

或通过 .NET CLI 安装：

```bash
dotnet add package Fast.Data
```

## 数据库配置

在 `web.config` 或 `db.config` 中注册 `DataConfig` 配置节：

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>
```

推荐使用统一的 `Connections` 配置：

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

旧版分组配置仍可继续使用：

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

## 初始化

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

## AOP 扩展

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

## 查询示例

使用默认数据库查询：

```csharp
var query = FastRead.Query<User>(a => a.IsEnabled == true);
```

指定数据库 Key 查询：

```csharp
var query = FastRead.Query<User>(a => a.IsEnabled == true, key: "ReportDb");
```

使用绑定数据库 Key 的查询入口：

```csharp
var query = FastRead.Use("ReportDb").Query<User>(a => a.IsEnabled == true);
```

执行 SQL 查询：

```csharp
var list = FastRead.ExecuteSql<User>(
    "select * from Users where IsEnabled = @IsEnabled",
    new[] { new SqlParameter("@IsEnabled", true) });
```

## 写入示例

使用默认数据库新增：

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

使用绑定数据库 Key 的写入入口：

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

## 作用域数据库切换

`FastDb.Use(key)` 会在当前执行上下文中设置数据库 Key，作用域结束后自动恢复原 Key：

```csharp
using (FastDb.Use("ArchiveDb"))
{
    var users = FastRead.Query<User>(a => a.IsEnabled == true);
    var result = FastWrite.Add(new User { UserName = "archive-user" });
}
```

该方式适合批量处理同一个数据库连接下的多次读写操作。

## Repository 用法

直接注册 Repository：

```csharp
services.AddTransient<IFastRepository, FastRepository>();
services.AddTransient<IRedisRepository, RedisRepository>();
```

注册 Repository 工厂：

```csharp
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();
```

使用默认 Repository：

```csharp
public class UserService
{
    private readonly IFastRepository repository;

    public UserService(IFastRepository repository)
    {
        this.repository = repository;
    }
}
```

使用 Repository 工厂切换数据库：

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

## Map SQL 配置

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

## 构建验证

当前解决方案包含旧式 `.NET Framework 4.5` 项目。在 Linux 环境中可通过 .NET SDK 和 reference assemblies 验证构建：

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" /root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

已知构建输出中存在较多 XML 文档注释警告，主要来自既有公开接口注释，当前不影响编译通过。

## 兼容说明

- 原有 `FastRead.Query(..., key)`、`FastWrite.Add(..., key)` 等传 Key 写法继续可用。
- 原有按数据库类型分组的配置节点继续可用。
- 新增 `Connections` 配置用于简化多数据库配置。
- 新增默认库机制后，未传 Key 时优先使用 `Default` 或 `IsDefault="true"` 指定的连接。
- 多个连接未指定默认库时，使用配置中的第一个可用连接。
