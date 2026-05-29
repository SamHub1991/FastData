# FastData 跨平台构建指南

## 概述

FastData 支持多目标框架编译（net45, net6.0, net8.0, net10.0），各框架有不同的构建要求。

## ⚠️ 重要警告

**`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` 只能在 net45 编译时使用，绝不能用于运行时！**

| 场景 | 设置 | 说明 |
|------|------|------|
| net45 编译 | `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` | 必需 |
| net6.0+ 编译 | 不设置 | 不需要 |
| **运行时（所有版本）** | **不设置** | **设置会导致 SqlClient 异常** |

## 推荐方式：使用构建脚本

```bash
# 构建所有支持的框架
./build.sh

# 构建特定框架
./build.sh net45      # 仅 net45
./build.sh net10.0    # 仅 net10.0

# 清理
./build.sh clean

# 运行 Demo（net10.0）
./build.sh run
```

## 手动构建命令

### net45（Linux）

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" \
/root/.dotnet/dotnet build FastData.sln -c Release /p:RegisterForComInterop=false /p:TargetFramework=net45
```

### net6.0+/net8.0+/net10.0+（Linux）

```bash
/root/.dotnet/dotnet build FastData.sln -c Release /p:TargetFramework=net10.0
```

### Visual Studio（Windows）

1. 直接打开 `FastData.sln`
2. 选择目标框架编译
3. VS 自动处理所有引用和配置

## 运行时命令（所有框架）

```bash
# 不要设置 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT！
/root/.dotnet/dotnet run --no-build -c Release --project FastData.Demo
```

## 原理说明

### 为什么 net45 需要特殊处理？

- net45 是 .NET Framework，需要 .NET Framework 参考程序集
- Linux 环境下没有 .NET Framework SDK
- 通过 `FrameworkPathOverride` 指向 NuGet 包中的参考程序集
- 设置 `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 绕过全球化 API 限制

### 为什么 net6.0+ 不能设置 Invariant？

- 使用 Microsoft.Data.SqlClient 需要完整的全球化支持
- `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 会禁用全球化功能
- SqlClient 检测到此设置后会抛出 `NotSupportedException`

## 配置文件位置

- `Directory.Build.props` - MSBuild 全局属性（自动被 dotnet 加载）
- `build.sh` - Linux/Mac 构建脚本
- `FastData.Demo/db.config` - 数据库配置（不包含构建配置）

## 常见问题

### 编译报错：找不到 FrameworkPathOverride

```bash
# 安装 .NET Framework 参考程序集
dotnet nuget install microsoft.netframework.referenceassemblies.net45 -s https://api.nuget.org/v3/index.json
```

### 运行时 SqlClient 异常

```
System.NotSupportedException: Globalization invariant mode is not supported
```

**原因：** 运行时设置了 `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`

**解决：** 确保运行时未设置此环境变量：
```bash
unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
dotnet run --no-build
```
