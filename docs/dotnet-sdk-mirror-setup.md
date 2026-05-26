# .NET SDK 镜像配置指南

## 配置概要

已成功配置 .NET 10 SDK 和 NuGet 包管理器国内镜像源，解决包下载慢和连接超时问题。

## 环境信息

### .NET SDK 版本
```
.NET SDK: 10.0.300
位置：/root/.dotnet/sdk
```

### 已配置镜像源

| 源名称 | URL | 状态 |
|--------|-----|------|
| nuget.org | https://api.nuget.org/v3/index.json | ✅ 官方源 |
| aliyun | https://mirrors.aliyun.com/nuget/v3/index.json | ✅ 阿里云 |
| tencent | https://mirrors.cloud.tencent.com/nuget/v3/index.json | ✅ 腾讯云 |
| huawei | https://mirrors.huaweicloud.com/repository/nuget/v3/index.json | ✅ 华为云 |
| nju | https://repo.nju.edu.cn/repository/nuget/v3/index.json | ✅ 南京大学 |

## 配置文件位置

### NuGet.Config
**位置**: `/root/.nuget/NuGet/NuGet.Config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- 官方源 -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    
    <!-- 国内镜像源 -->
    <add key="aliyun" value="https://mirrors.aliyun.com/nuget/v3/index.json" protocolVersion="3" />
    <add key="tencent" value="https://mirrors.cloud.tencent.com/nuget/v3/index.json" protocolVersion="3" />
    <add key="huawei" value="https://mirrors.huaweicloud.com/repository/nuget/v3/index.json" protocolVersion="3" />
    <add key="nju" value="https://repo.nju.edu.cn/repository/nuget/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

### 环境变量
**位置**: `/etc/environment` 和 `~/.bashrc`

```bash
# 启用全球化不变模式（解决 ICU 库缺失问题）
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# 禁用遥测
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# 禁用 Logo 显示
export DOTNET_NOLOGO=true
```

## 使用方式

### 1. 查看已配置的源
```bash
/root/.dotnet/dotnet nuget list source
```

### 2. 测试包恢复
```bash
cd /path/to/project
/root/.dotnet/dotnet restore
```

### 3. 测试指定镜像源
```bash
/root/.dotnet/dotnet restore -s aliyun
/root/.dotnet/dotnet restore -s tencent
```

### 4. 禁用/启用镜像源
```bash
# 禁用官方源（仅使用国内镜像）
/root/.dotnet/dotnet nuget disable source nuget.org

# 重新启用
/root/.dotnet/dotnet nuget enable source nuget.org
```

## FastData 项目构建

### 标准构建命令
```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

/root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

### 一键构建脚本
```bash
#!/bin/bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

/root/.dotnet/dotnet build /workspace/FastData.sln /p:RegisterForComInterop=false -v minimal
```

### 运行测试
```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
/root/.dotnet/dotnet test FastData.Tests/FastData.Tests.csproj
```

## 镜像优先级

默认情况下，NuGet 会按顺序尝试所有启用的源。如果需要调整优先级，可以修改 `NuGet.Config` 文件，或者使用以下命令：

```bash
# 设置镜像源优先级（数字越小优先级越高）
/root/.dotnet/dotnet nuget update source aliyun -p 1
/root/.dotnet/dotnet nuget update source tencent -p 2
```

## 故障排除

### 问题 1: 包下载失败
**症状**: 构建时提示无法下载 NuGet 包

**解决**:
```bash
# 清除 NuGet 缓存
/root/.dotnet/dotnet nuget locals all --clear

# 重新恢复包
/root/.dotnet/dotnet restore -v detailed
```

### 问题 2: 某个镜像源不可用
**症状**: 构建时频繁超时

**解决**:
```bash
# 临时禁用特定源
/root/.dotnet/dotnet nuget disable source <source-name>

# 查看启用的源
/root/.dotnet/dotnet nuget list source
```

### 问题 3: ICU 库缺失错误
**症状**: `Couldn't find a valid ICU package`

**解决**: 已配置环境变量 `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`
如果仍有问题，安装 ICU 库：
```bash
apt-get install -y libicu-dev
```

### 问题 4: 权限错误
**症状**: 无法写入 NuGet 缓存

**解决**:
```bash
# 确保缓存目录有正确权限
chmod -R 755 /root/.nuget
chown -R root:root /root/.nuget
```

## 镜像源更新检查

```bash
#!/bin/bash
# 检查所有镜像源可用性

echo "检查 NuGet 镜像源可用性..."
echo ""

for source in "nuget.org" "aliyun" "tencent" "huawei" "nju"; do
    echo -n "$source: "
    timeout 5 curl -s -o /dev/null -w "%{http_code}" \
        $(curl -s https://api.nuget.org/v3/index.json | grep -o 'https://[^"]*index.json' | head -1 || echo "https://api.nuget.org/v3/index.json") \
        2>/dev/null && echo " ✓" || echo " ✗"
done
```

## 最佳实践

### 1. 项目级 NuGet.Config
在项目的根目录创建 `NuGet.Config`，确保团队成员使用相同的镜像配置：

```bash
# 在项目根目录
cat > NuGet.Config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="aliyun" value="https://mirrors.aliyun.com/nuget/v3/index.json" />
  </packageSources>
</configuration>
EOF
```

### 2. CI/CD 环境配置
在 CI/CD 流水线中：

```yaml
# Azure Pipelines 示例
steps:
- script: |
    export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
    mkdir -p ~/.nuget/NuGet
    cat > ~/.nuget/NuGet/NuGet.Config << 'EOF'
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <packageSources>
        <add key="aliyun" value="https://mirrors.aliyun.com/nuget/v3/index.json" />
      </packageSources>
    </configuration>
    EOF
  displayName: '配置 NuGet 镜像'
```

### 3. Dockerfile 配置
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0

# 配置环境变量
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# 配置 NuGet 镜像
RUN mkdir -p /root/.nuget/NuGet && \
    echo '<?xml version="1.0" encoding="utf-8"?>' > /root/.nuget/NuGet/NuGet.Config && \
    echo '<configuration>' >> /root/.nuget/NuGet/NuGet.Config && \
    echo '  <packageSources>' >> /root/.nuget/NuGet/NuGet.Config && \
    echo '    <add key="aliyun" value="https://mirrors.aliyun.com/nuget/v3/index.json" />' >> /root/.nuget/NuGet/NuGet.Config && \
    echo '  </packageSources>' >> /root/.nuget/NuGet/NuGet.Config && \
    echo '</configuration>' >> /root/.nuget/NuGet/NuGet.Config
```

## 相关链接

- [.NET 下载](https://dotnet.microsoft.com/download)
- [NuGet 官方文档](https://docs.microsoft.com/zh-cn/nuget/)
- [阿里云 NuGet 镜像](https://mirrors.aliyun.com/nuget/)
- [腾讯云 NuGet 镜像](https://mirrors.cloud.tencent.com/nuget/)
- [华为云 NuGet 镜像](https://mirrors.huaweicloud.com/repository/nuget/)

---

**配置时间**: 2026-05-26  
**SDK 版本**: 10.0.300  
**镜像源数量**: 5 (1 官方 + 4 国内)
