# FastData

FastData 是一个面向 .NET Framework 的轻量 ORM 组件，支持 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis 辅助能力和多数据库连接配置。

NuGet 地址：<https://www.nuget.org/packages/Fast.Data/>

## 核心能力

- 支持 Oracle、MySQL、SQL Server、SQLite、PostgreSQL、DB2。
- 支持默认数据库连接和按 Key 指定数据库连接。
- 支持 `FastRead`、`FastWrite` 静态入口。
- 支持 `FastRead.Use(key)`、`FastWrite.Use(key)` 绑定数据库 Key。
- 支持 `using (FastDb.Use(key))` 在当前执行上下文中切换数据库。
- 支持 `FastRepository` 和 `FastRepositoryFactory`。
- 支持 XML Map SQL、动态 SQL 标签和 AOP 扩展。

## 快速安装

```powershell
Install-Package Fast.Data
```

```bash
dotnet add package Fast.Data
```

## 快速配置

推荐使用统一 `Connections` 配置：

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>

<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" ConnStr="server=.;database=demo;uid=sa;pwd=123456" IsDefault="true" DesignModel="DbFirst" CacheType="web" />
    <Add Provider="MySql" Key="ReportDb" ConnStr="server=127.0.0.1;database=report;uid=root;pwd=123456" DesignModel="DbFirst" CacheType="web" />
  </Connections>
</DataConfig>
```

## 快速使用

默认库查询：

```csharp
var users = FastRead.Query<User>(a => a.IsEnabled == true);
```

指定库查询：

```csharp
var reports = FastRead.Use("ReportDb").Query<Report>(a => a.Year == 2026);
```

作用域切换：

```csharp
using (FastDb.Use("ArchiveDb"))
{
    var logs = FastRead.Query<Log>(a => a.CreatedTime >= beginTime);
    FastWrite.Add(new ArchiveLog());
}
```

Repository 工厂：

```csharp
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();

var defaultRepository = factory.Default();
var reportRepository = factory.Use("ReportDb");
```

## 文档

- [中文使用说明](.monkeycode/docs/usage.md)
- [当前进度](.monkeycode/docs/progress.md)
- [2026 年 5 月需求文档](.monkeycode/specs/项目需求2026年5月/requirements.md)
- [2026 年 5 月技术方案](.monkeycode/specs/项目需求2026年5月/design.md)
- [2026 年 5 月任务清单](.monkeycode/specs/项目需求2026年5月/tasklist.md)

## 构建验证

当前解决方案包含旧式 `.NET Framework 4.5` 项目。在 Linux 环境中可通过 .NET SDK 和 reference assemblies 验证构建：

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" /root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

当前构建状态：`0 Warning(s)`, `0 Error(s)`。
