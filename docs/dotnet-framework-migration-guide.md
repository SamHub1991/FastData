# .NET Framework 迁移指南

## 当前环境分析

### 系统信息
- **操作系统**: Debian 12 (bookworm) Linux
- **.NET SDK**: 10.0.300 (跨平台版本)
- **.NET Framework**: 无法安装（仅 Windows 支持）

### FastData 项目现状
- **目标框架**: .NET Framework 4.5
- **构建方式**: .NET 10 SDK + Reference Assemblies
- **构建状态**: ✅ 成功 (0 Errors, 0 Warnings)
- **运行限制**: 只能在 Windows 执行

## 方案一：保持 .NET Framework 4.5（现状）

### 优点
- ✅ 无需修改代码
- ✅ 现有功能完全保留
- ✅ 适合 Windows 环境部署

### 缺点
- ❌ Linux 环境无法运行测试
- ❌ 无法验证实际功能
- ❌ .NET Framework 已停止更新

### 构建命令
```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"

/root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

### 适用场景
- 最终部署环境是 Windows
- 不需要在 Linux 运行
- 仅需编译验证

## 方案二：迁移到 .NET 10（推荐）

### 优点
- ✅ 跨平台运行（Linux/Mac/Windows）
- ✅ 性能提升 50%+ 
- ✅ 现代化语言特性
- ✅ 持续维护和支持
- ✅ 可在当前环境完整测试

### 缺点
- ⚠️ 需要修改项目文件
- ⚠️ 部分 API 不兼容（如 WinForms 在 Linux）
- ⚠️ 需要验证所有功能

### 迁移步骤

#### 1. 更新项目文件

##### 核心库项目（FastData.csproj）
```xml
<!-- 之前 -->
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>

<!-- 之后 -->
<TargetFramework>net10.0</TargetFramework>
```

##### WinForms 项目（需要特殊处理）

**选项 A**: 使用 .NET 10 WinForms（仅 Windows 运行）
```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
```

**选项 B**: 改用跨平台 UI（Avalonia/MonoGame）
```xml
<TargetFramework>net10.0</TargetFramework>
<!-- 移除 WinForms 引用 -->
```

#### 2. 移除不兼容的引用

需要移除或替换：
- `System.Web.Extensions` → 使用 `System.Text.Json`
- `Microsoft.CSharp` → 通常不需要
- WinForms 特定库（如果迁移到跨平台）

#### 3. 代码适配

##### JSON 序列化
```csharp
// 之前 (.NET Framework 4.5)
using System.Web.Script.Serialization;
var serializer = new JavaScriptSerializer();

// 之后 (.NET 10)
using System.Text.Json;
var json = JsonSerializer.Serialize(obj);
```

##### 数据库连接
```csharp
// 无需修改 - DbProviderFactories 在 .NET 10 中可用
var factory = DbProviderFactories.GetFactory(providerName);
```

#### 4. 迁移检查清单

- [ ] 更新所有 .csproj 文件
- [ ] 替换不兼容的 NuGet 包
- [ ] 修复编译错误
- [ ] 运行所有测试
- [ ] 验证功能完整性
- [ ] 更新文档

## 方案三：混合方式

### 策略
- **核心库**: 迁移到 .NET 10 (FastData, FastData.Tooling)
- **WinForms UI**: 保持 .NET Framework 4.5（仅 Windows）
- **测试项目**: 迁移到 .NET 10（可在 Linux 运行测试）

### 优点
- ✅ 核心功能可跨平台测试
- ✅ UI 保持原样
- ✅ 渐进式迁移

### 实现步骤

#### 1. 迁移 FastData 核心库
```xml
<!-- FastData.csproj -->
<TargetFramework>net10.0</TargetFramework>
```

#### 2. 迁移 FastData.Tooling
```xml
<!-- FastData.Tooling.csproj -->
<TargetFramework>net10.0</TargetFramework>
```

#### 3. 迁移 FastData.Tests
```xml
<!-- FastData.Tests.csproj -->
<TargetFramework>net10.0</TargetFramework>
<ItemGroup>
  <ProjectReference Include="..\..\FastData\FastData.csproj" />
</ItemGroup>
```

#### 4. 保持 WinForms 项目不变
```xml
<!-- FastData.SyncTool.WinForms.csproj -->
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
<!-- 保持不变 -->
```

#### 5. 配置多目标框架（可选）
```xml
<!-- 同时支持 .NET Framework 和 .NET 10 -->
<TargetFrameworks>net45;net10.0</TargetFrameworks>

<ItemGroup Condition="'$(TargetFramework)' == 'net45'">
  <Reference Include="System.Web.Extensions" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

## 推荐方案

**建议采用方案三：混合方式**

### 理由
1. **风险可控**: 核心库迁移，UI 保持原样
2. **立即收益**: 可在 Linux 运行测试验证功能
3. **渐进演进**: 后续可逐步迁移 UI 部分

### 实施计划

#### 第一阶段：核心库迁移（1-2 天）
- [ ] 迁移 FastData 到 net10.0
- [ ] 迁移 FastData.Tooling 到 net10.0
- [ ] 替换 JavaScriptSerializer 为 System.Text.Json
- [ ] 验证编译通过

#### 第二阶段：测试验证（1 天）
- [ ] 迁移 FastData.Tests 到 net10.0
- [ ] 在 Linux 运行所有测试
- [ ] 验证数据库连接
- [ ] 验证同步功能

#### 第三阶段：UI 处理（可选）
- [ ] 评估 WinForms 迁移需求
- [ ] 或保持 .NET Framework 4.5（仅 Windows）
- [ ] 或改用 Avalonia UI（跨平台）

## 快速开始

### 立即迁移核心库

```bash
# 1. 备份当前项目
cd /workspace
git checkout -b migrate-to-dotnet10

# 2. 修改 FastData.csproj
# 编辑文件，替换 TargetFrameworkVersion 为 TargetFramework

# 3. 修改 FastData.Tooling.csproj
# 同样修改

# 4. 替换 JSON 序列化
# 搜索替换 JavaScriptSerializer 为 System.Text.Json

# 5. 构建验证
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

/root/.dotnet/dotnet build FastData/FastData.csproj
/root/.dotnet/dotnet build FastData.Tooling/FastData.Tooling.csproj

# 6. 运行测试
/root/.dotnet/dotnet test FastData.Tests/FastData.Tests.csproj
```

## 兼容性参考

### .NET Framework 4.5 → .NET 10 API 变更

| .NET Framework API | .NET 10 替代方案 | 兼容性 |
|-------------------|----------------|--------|
| `JavaScriptSerializer` | `System.Text.Json.JsonSerializer` | ⚠️ 需要修改 |
| `DbProviderFactories` | 保持不变 | ✅ 完全兼容 |
| `DataTable` | 保持不变 | ✅ 完全兼容 |
| `FileStream` | 保持不变 | ✅ 完全兼容 |
| `Task.Run` | 保持不变 | ✅ 完全兼容 |
| WinForms Controls | WinForms (.NET 10) | ✅ 但仅 Windows |

### NuGet 包映射

| .NET Framework 包 | .NET 10 替代方案 |
|------------------|----------------|
| `Microsoft.NetFramework.ReferenceAssemblies` | 不需要（内建） |
| `System.Web.Extensions` | `System.Text.Json` |
| `Newtonsoft.Json` (可选) | `System.Text.Json` (内建) |

## 常见问题

### Q1: 为什么要迁移？
**A**: .NET Framework 已停止更新，.NET 10 提供更好的性能、跨平台支持和长期维护。

### Q2: 迁移成本高吗？
**A**: 核心库迁移成本低（主要改项目文件和 JSON 序列化），UI 部分可选择保持或渐进迁移。

### Q3: 迁移后还能在 Windows 运行吗？
**A**: 可以，.NET 10 完全支持 Windows，且性能更好。

### Q4: 如果只改项目文件会怎样？
**A**: 大部分代码可以工作，但需要处理 `System.Web.Extensions` 等不兼容的引用。

## 总结

| 方案 | 工作量 | 收益 | 推荐度 |
|------|--------|------|--------|
| 方案一：保持现状 | 无 | 低 | ⭐ |
| 方案二：完全迁移 | 中 | 高 | ⭐⭐⭐ |
| 方案三：混合方式 | 低 | 中 | ⭐⭐⭐⭐ |

**建议选择方案三**：先迁移核心库到 .NET 10，获得跨平台测试能力，UI 部分根据实际需求决定。

---

**创建时间**: 2026-05-26  
**适用项目**: FastData  
**.NET 版本**: Framework 4.5 → .NET 10

---

## 当前项目决策（2026-05-26）

**决策**: 保持 .NET Framework 4.5 兼容性，暂不迁移

**理由**:
- 现有 Windows 环境部署稳定
- 兼容性要求（客户环境限制）
- 现有功能完整，无需跨平台

**当前构建方式**:
```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
dotnet build FastData.sln /p:RegisterForComInterop=false
```

**构建状态**: ✅ 成功 (0 Warnings, 0 Errors)

**可用测试环境**:
- Docker MySQL 8.0: localhost:3306
- Docker PostgreSQL 15: localhost:5432
- SQLite: /tmp/fastdata_test.db

**下一步**: 在完整 Windows 环境中进行功能验证
