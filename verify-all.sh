#!/bin/bash

# FastData 综合验证测试脚本
# 用于验证 ORM API、数据同步、缓存等功能

set -e

echo "=== FastData 综合验证测试 ==="
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 测试结果统计
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# 测试函数
run_test() {
    local test_name="$1"
    local test_cmd="$2"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo -n "测试: $test_name ... "
    
    if eval "$test_cmd" > /dev/null 2>&1; then
        echo -e "${GREEN}通过${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}失败${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

# 1. 构建验证
echo "=== 1. 构建验证 ==="
run_test "FastUntility 构建" "/root/.dotnet/dotnet build FastUntility/FastUntility.csproj -v quiet"
run_test "FastData.Tooling 构建" "/root/.dotnet/dotnet build FastData.Tooling/FastData.Tooling.csproj -v quiet"
run_test "FastData 构建" "/root/.dotnet/dotnet build FastData/FastData.csproj -v quiet"
run_test "FastRedis 构建" "/root/.dotnet/dotnet build FastRedis/FastRedis.csproj -v quiet"
run_test "FastData.Tests 构建" "/root/.dotnet/dotnet build FastData.Tests/FastData.Tests.csproj -v quiet"
run_test "FastData.Demo 构建" "/root/.dotnet/dotnet build FastData.Demo/FastData.Demo.csproj -v quiet"

echo ""

# 2. 单元测试
echo "=== 2. 单元测试 ==="
run_test "单元测试通过" "/root/.dotnet/dotnet test FastData.Tests/FastData.Tests.csproj --no-build -v quiet 2>&1 | grep -q 'Passed:.*73'"

echo ""

# 3. NuGet 包验证
echo "=== 3. NuGet 包验证 ==="
run_test "FastUntility.nupkg 存在" "test -f nupkgs/FastUntility.1.0.0.nupkg"
run_test "FastData.Tooling.nupkg 存在" "test -f nupkgs/FastData.Tooling.1.0.0.nupkg"
run_test "FastData.nupkg 存在" "test -f nupkgs/FastData.1.0.0.nupkg"
run_test "FastRedis.nupkg 存在" "test -f nupkgs/FastRedis.1.0.0.nupkg"

echo ""

# 4. 多目标框架验证
echo "=== 4. 多目标框架验证 ==="
run_test "net45 目标存在" "grep -q 'net45' FastData/FastData.csproj"
run_test "net6.0 目标存在" "grep -q 'net6.0' FastData/FastData.csproj"
run_test "net8.0 目标存在" "grep -q 'net8.0' FastData/FastData.csproj"
run_test "net10.0 目标存在" "grep -q 'net10.0' FastData/FastData.csproj"

echo ""

# 5. 接口拆分验证
echo "=== 5. 接口拆分验证 ==="
run_test "IReadRepository 存在" "test -f FastData/Repository/IReadRepository.cs"
run_test "IWriteRepository 存在" "test -f FastData/Repository/IWriteRepository.cs"
run_test "IMapRepository 存在" "test -f FastData/Repository/IMapRepository.cs"
run_test "IFastRepository 继承关系" "grep -q 'IReadRepository' FastData/Repository/IFastRepository.cs"

echo ""

# 6. Redis 单例模式验证
echo "=== 6. Redis 单例模式验证 ==="
run_test "Lazy<FullRedis> 存在" "grep -q 'Lazy<FullRedis>' FastRedis/RedisInfo.NewLife.cs"
run_test "Redis 单例属性" "grep -q 'public static FullRedis Redis' FastRedis/RedisInfo.NewLife.cs"

echo ""

# 7. 连接字符串加密验证
echo "=== 7. 连接字符串加密验证 ==="
run_test "IsEncrypt 属性" "grep -q 'IsEncrypt' FastData/Context/DataContext.cs"
run_test "Decrypto 调用" "grep -q 'Decrypto' FastData/Context/DataContext.cs"

echo ""

# 8. Demo 项目验证
echo "=== 8. Demo 项目验证 ==="
run_test "Program.cs 存在" "test -f FastData.Demo/Program.cs"
run_test "UserRepository 存在" "test -f FastData.Demo/Repositories/UserRepository.cs"
run_test "CacheService 存在" "test -f FastData.Demo/Services/CacheService.cs"
run_test "DataSyncService 存在" "test -f FastData.Demo/Services/DataSyncService.cs"
run_test "UsersController 存在" "test -f FastData.Demo/Controllers/UsersController.cs"

echo ""

# 9. 文档验证
echo "=== 9. 文档验证 ==="
run_test "README.md 存在" "test -f README.md"
run_test "CHANGELOG.md 存在" "test -f CHANGELOG.md"
run_test "progress.md 存在" "test -f .monkeycode/docs/progress.md"
run_test "MEMORY.md 存在" "test -f .monkeycode/MEMORY.md"

echo ""

# 10. 条件编译验证
echo "=== 10. 条件编译验证 ==="
run_test "NETFRAMEWORK 条件" "grep -q '#if.*NETFRAMEWORK' FastData/FastDb.cs"
run_test "AsyncLocal 实现" "grep -q 'AsyncLocal' FastData/FastDb.cs"

echo ""

# 测试结果汇总
echo "=== 测试结果汇总 ==="
echo "总测试数: $TOTAL_TESTS"
echo -e "通过: ${GREEN}$PASSED_TESTS${NC}"
echo -e "失败: ${RED}$FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "\n${GREEN}所有测试通过！${NC}"
    exit 0
else
    echo -e "\n${RED}存在失败的测试！${NC}"
    exit 1
fi
