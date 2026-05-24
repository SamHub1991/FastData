# FastData 当前进度

更新时间：2026-05-24

## 已完成

- 已创建 2026 年 5 月需求、技术方案和实施任务清单。
- 已实现统一 `Connections` 多数据库配置结构。
- 已保留旧版按数据库类型分组配置的兼容读取能力。
- 已实现默认数据库配置，未传 Key 时可使用默认库。
- 已实现 `FastRead.Use(key)` 和 `FastWrite.Use(key)` 绑定数据库 Key 调用入口。
- 已实现 `FastDb.Use(key)` 作用域数据库切换。
- 已实现 `IFastRepositoryFactory` 和 `FastRepositoryFactory`，支持默认库与指定库 Repository。
- 已补充配置 Key 缺失时的可用 Key 错误提示。
- 已修复当前构建中暴露的 SQL Server MapFile 模型重复定义问题。
- 已修复 `VisitModel.IsSuccess` 缺失问题。
- 已修复 `DataContext` 中 `Parameter.ParamMerge(...)` 命名空间引用问题。
- 已将中文使用说明整理到 `.monkeycode/docs/usage.md`。
- 已将 README 收敛为项目入口、快速示例和文档导航。

## 验证状态

最近一次构建命令：

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" /root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

验证结果：

- 构建通过，`0 Error(s)`。
- 构建输出仍包含较多 XML 文档注释警告，主要来自既有公开接口注释。
- `git diff --check` 已通过。

## 待推进

- 架构优化：继续抽取数据库适配器、SQL 方言、元数据读取接口。
- Model 生成工具：创建 `FastData.Tooling` 和 `FastData.ModelGenerator.WinForms`。
- 数据同步工具：创建 `FastData.SyncTool.WinForms`，实现中间库脚本、全量同步、增量同步、重试和清理。
- 中文文档：后续补充 Model 生成工具、数据同步工具和 FAQ。

## 当前注意事项

- 当前仓库工作区存在本地未跟踪文件 `dotnet-install.sh`，已按要求排除在提交之外。
- Linux 环境构建旧式 `.NET Framework 4.5` 项目需要 `FrameworkPathOverride` 指向 reference assemblies。
- 使用 `/p:RegisterForComInterop=false` 可绕过 .NET Core MSBuild 对 COM 注册任务的限制。
