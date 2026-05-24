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

勾选"清理中间库成功记录"后，工具会清理 `fd_sync_record` 和 `fd_sync_batch` 中状态为 `Success` 的记录。

---

## 数据同步工具 - 高级功能（2026-05-24 新增）

### 智能范围同步（首次全量/后续增量）

同步工具会自动判断是首次同步还是后续同步：

- **首次同步**：同步表中所有历史数据
- **后续同步**：只同步最近 3 天的数据（从上次同步时间点往前推 3 天）
- **自动记录**：每次同步完成后自动记录当前时间，并保存到配置文件

**配置步骤**：
1. 选择"智能范围"单选按钮
2. 设置"范围天数"（默认 3 天）
3. 界面显示"上次同步：XXXX-XX-XX XX:XX:XX"或"上次同步：从未"

### 多表批量同步

支持一次配置多个表进行批量同步：

**添加表**：
1. 点击"从数据库加载"按钮，自动连接源库并加载所有表
2. 或点击"添加表"按钮，从表选择对话框中手动选择
3. 表选择对话框支持搜索过滤

**表配置列表**：
| 列名 | 说明 |
|------|------|
| 表名 | 源表名称 |
| 主键字段 | 用于 UPSERT 判断，多个字段逗号分隔 |
| 增量字段 | 用于时间范围查询的时间列 |
| 启用 | 勾选后参与同步 |
| 状态 | 待同步/同步中/成功/部分失败 |

**表顺序调整**：
- 选中表后点击"上移"/"下移"调整同步顺序
- 同步按从上到下顺序依次执行

### 时间范围选择

**三种同步模式**：

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| 智能范围 | 自动判断首次全量/后续增量 | 日常增量同步 |
| 手动范围 | 手动指定起止时间 | 补历史数据、重同步特定时间段 |
| 全量同步 | 不限时间范围 | 初始化、数据重建 |

**快速选择**：
点击"快速选择"按钮，可选：
- 最近 1 天
- 最近 3 天
- 最近 7 天
- 最近 30 天
- 本月
- 上月

### 定时同步

数据同步工具现在支持定时自动同步，可实现准实时数据同步。

**配置步骤**：
1. 勾选"启用定时同步"
2. 设置"定时同步间隔（秒）"，建议 30 秒以上
3. 点击"开始同步"按钮启动定时同步
4. 再次点击可停止定时同步

**状态指示**：
- **绿色**：定时同步运行中
- **橙色**：同步进行中
- **灰色**：定时同步已暂停
- **黑色**：就绪状态

### 复合主键增量同步

对于没有自增主键的表，支持配置多个字段作为复合主键进行增量同步。

**配置方式 1：界面配置**
1. 在"主键字段（逗号分隔）"中输入主键字段，如 `UserId,OrderDate`
2. 增量起点使用 `|` 分隔多个值，如 `100|2026-01-01`

**配置方式 2：主键配置管理**
1. 点击"主键配置"按钮
2. 输入表名、主键字段列表
3. 勾选"自增主键"（如果适用）
4. 可指定增量字段（如 `UpdateTime`）
5. 点击"添加"保存配置
6. 点击"导出 SQL"生成配置表脚本

**主键配置表结构**：
```sql
CREATE TABLE fd_table_pk_config (
    table_name NVARCHAR(128) PRIMARY KEY,
    pk_columns NVARCHAR(512),
    is_auto_increment BIT DEFAULT 0,
    incremental_column NVARCHAR(128)
);
```

### 增量同步策略选择

| 场景 | 推荐配置 | 说明 |
|------|----------|------|
| 自增主键表 | 主键字段=`Id`，勾选"自增主键" | 使用 `Id > @lastValue` 增量 |
| 复合主键表 | 主键字段=`UserId,OrderDate` | 使用多字段 OR 条件增量 |
| 时间戳表 | 增量字段=`UpdateTime` | 使用 `UpdateTime > @lastValue` |
| 无主键表 | 留空主键和增量字段 | 每次都全量同步 |

### UPSERT 模式

同步工具现在自动检测记录是否存在：
- **记录不存在**：执行 INSERT
- **记录已存在**：执行 UPDATE（根据主键判断）

这样即使源表数据发生变更，目标表也能保持同步更新。

### 配置持久化

同步任务配置自动保存到 `sync_tasks.json` 文件（程序目录下）：

```json
{
  "TaskId": "UserSync",
  "SourceTable": "Users",
  "TargetTable": "Users",
  "PrimaryKeyColumns": "Id",
  "IncrementalColumn": "UpdateTime",
  "LastSyncTime": "2026-05-24 15:30:00",
  "RangeDays": 3
}
```

下次启动工具时，会自动读取上次同步时间。

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
