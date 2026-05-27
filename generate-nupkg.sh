#!/bin/bash

# FastData NuGet 包生成脚本
# 用于生成所有项目的 NuGet 包

set -e

echo "=== FastData NuGet 包生成 ==="
echo ""

# 创建输出目录
OUTPUT_DIR="./nupkgs"
mkdir -p $OUTPUT_DIR

# 清理旧包
rm -f $OUTPUT_DIR/*.nupkg

# 项目列表
PROJECTS=(
    "FastUntility/FastUntility.csproj"
    "FastData.Tooling/FastData.Tooling.csproj"
    "FastData/FastData.csproj"
    "FastRedis/FastRedis.csproj"
)

# 先构建所有项目（Release 配置）
echo "=== 构建项目（Release 配置）==="
for proj in "${PROJECTS[@]}"; do
    echo "正在构建: $proj"
    /root/.dotnet/dotnet build $proj \
        --configuration Release \
        -v quiet 2>&1 || echo "警告: $proj 构建失败"
done

echo ""
echo "=== 生成 NuGet 包 ==="

# 生成 NuGet 包
for proj in "${PROJECTS[@]}"; do
    echo "正在生成: $proj"
    /root/.dotnet/dotnet pack $proj \
        --configuration Release \
        --output $OUTPUT_DIR \
        --no-build \
        -v quiet 2>&1 || echo "警告: $proj 生成失败"
    echo ""
done

# 列出生成的包
echo "=== 生成的 NuGet 包 ==="
ls -la $OUTPUT_DIR/*.nupkg 2>/dev/null || echo "未找到生成的包"

echo ""
echo "=== 完成 ==="
