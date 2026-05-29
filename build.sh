#!/bin/bash
#
# FastData 跨平台构建脚本
#
# 用法:
#   ./build.sh                    # 构建所有框架（自动检测环境）
#   ./build.sh net45              # 仅构建 net45
#   ./build.sh net6.0             # 仅构建 net6.0
#   ./build.sh net8.0             # 仅构建 net8.0
#   ./build.sh net10.0            # 仅构建 net10.0
#   ./build.sh clean              # 清理构建输出
#   ./build.sh run                # 运行 Demo（仅 net10.0）
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检测是否支持 net45 编译
check_net45_support() {
    # 检查 FrameworkPathOverride 路径
    FRAMEWORK_REF="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
    
    if [ ! -d "$FRAMEWORK_REF" ]; then
        log_warn "net45 reference assemblies not found at $FRAMEWORK_REF"
        log_info "Install with: dotnet nuget install microsoft.netframework.referenceassemblies.net45 -s https://api.nuget.org/v3/index.json"
        return 1
    fi
    
    return 0
}

# 构建函数
do_build() {
    local framework="$1"
    
    if [ -z "$framework" ]; then
        # 构建所有框架
        log_info "Building all frameworks..."
        
        # net45 (仅当支持时)
        if check_net45_support; then
            log_info "Building net45..."
            export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
            export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
            /root/.dotnet/dotnet build FastData.sln -c Release -p:RegisterForComInterop=false /p:TargetFramework=net45
            unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
            unset FrameworkPathOverride
        else
            log_warn "Skipping net45 (reference assemblies not installed)"
        fi
        
        # net6.0, net8.0, net10.0
        for fw in net6.0 net8.0 net10.0; do
            log_info "Building $fw..."
            /root/.dotnet/dotnet build FastData.sln -c Release /p:TargetFramework=$fw
        done
        
        log_info "All frameworks built successfully!"
        
    elif [ "$framework" == "net45" ]; then
        if check_net45_support; then
            log_info "Building net45..."
            export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
            export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
            /root/.dotnet/dotnet build FastData.sln -c Release -p:RegisterForComInterop=false /p:TargetFramework=net45
            unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
            unset FrameworkPathOverride
            log_info "net45 built successfully!"
        else
            log_error "net45 not supported (reference assemblies not installed)"
            exit 1
        fi
        
    elif [ "$framework" == "clean" ]; then
        log_info "Cleaning build outputs..."
        /root/.dotnet/dotnet clean FastData.sln
        log_info "Clean completed!"
        
    elif [ "$framework" == "run" ]; then
        log_info "Running FastData.Demo (net10.0)..."
        cd FastData.Demo
        # 运行时不要设置 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
        /root/.dotnet/dotnet run --configuration Release
    else
        # 构建特定框架
        log_info "Building $framework..."
        if [[ "$framework" == "net45" ]]; then
            if check_net45_support; then
                export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
                /root/.dotnet/dotnet build FastData.sln -c Release -p:RegisterForComInterop=false /p:TargetFramework=$framework
                unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
                unset FrameworkPathOverride
            else
                log_error "net45 not supported"
                exit 1
            fi
        else
            /root/.dotnet/dotnet build FastData.sln -c Release /p:TargetFramework=$framework
        fi
        log_info "$framework built successfully!"
    fi
}

# 主程序
case "${1:-}" in
    net45|net6.0|net8.0|net10.0|clean|run|"")
        do_build "$1"
        ;;
    -h|--help)
        echo "FastData Build Script"
        echo ""
        echo "Usage: $0 [command]"
        echo ""
        echo "Commands:"
        echo "  (none)     Build all frameworks"
        echo "  net45      Build net45 only"
        echo "  net6.0     Build net6.0 only"
        echo "  net8.0     Build net8.0 only"
        echo "  net10.0    Build net10.0 only"
        echo "  clean      Clean build outputs"
        echo "  run        Run FastData.Demo"
        echo "  -h, --help Show this help"
        echo ""
        echo "Important:"
        echo "  - net45 requires DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 (compile time only)"
        echo "  - net6.0+ should NOT set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"
        echo "  - Runtime (all versions) should NOT set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"
        ;;
    *)
        log_error "Unknown command: $1"
        echo "Use '$0 --help' for usage information"
        exit 1
        ;;
esac
