#!/bin/bash
#
# FastData 跨平台构建脚本
#
# 用法:
#   ./build.sh                                # 构建所有框架（cross 平台，默认）
#   ./build.sh --platform cross               # 跨平台构建（仅 net6.0/net8.0/net10.0）
#   ./build.sh --platform windows             # Windows 构建（含 net45/net462）
#   ./build.sh --platform windows net45       # 仅构建 net45
#   ./build.sh net10.0                        # 仅构建 net10.0（cross 平台）
#   ./build.sh clean                          # 清理构建输出
#   ./build.sh run                            # 运行 Demo（仅 net10.0）
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

# 默认平台
PLATFORM="cross"

# 解析 --platform 参数
parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --platform)
                if [[ -z "$2" || "$2" == -* ]]; then
                    log_error "--platform requires a value (cross or windows)"
                    exit 1
                fi
                PLATFORM="$2"
                shift 2
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                ARGS+=("$1")
                shift
                ;;
        esac
    done
}

show_help() {
    echo "FastData Build Script"
    echo ""
    echo "Usage: $0 [options] [command]"
    echo ""
    echo "Options:"
    echo "  --platform <cross|windows>  Target platform (default: cross)"
    echo "    cross    Build net6.0;net8.0;net10.0 only (skip net45)"
    echo "    windows  Build all targets including net45/net462"
    echo ""
    echo "Commands:"
    echo "  (none)     Build all frameworks for target platform"
    echo "  net45      Build net45 only (requires --platform windows)"
    echo "  net6.0     Build net6.0 only"
    echo "  net8.0     Build net8.0 only"
    echo "  net10.0    Build net10.0 only"
    echo "  clean      Clean build outputs"
    echo "  run        Run FastData.Demo"
    echo "  -h, --help Show this help"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Cross-platform build (net6.0+)"
    echo "  $0 --platform windows                 # Full build with net45"
    echo "  $0 --platform cross net10.0           # Build net10.0 only"
    echo "  $0 --platform windows clean           # Clean all outputs"
}

# 检测是否支持 net45 编译
check_net45_support() {
    FRAMEWORK_REF="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
    
    if [ ! -d "$FRAMEWORK_REF" ]; then
        log_warn "net45 reference assemblies not found at $FRAMEWORK_REF"
        return 1
    fi
    
    return 0
}

# 构建函数
do_build() {
    local framework="$1"
    local build_platform="$PLATFORM"
    
    log_info "Platform: $build_platform"
    
    if [ -z "$framework" ]; then
        # 构建所有框架
        if [ "$build_platform" == "cross" ]; then
            log_info "Building cross-platform targets (net6.0;net8.0;net10.0)..."
            /root/.dotnet/dotnet build FastData.sln -c Release -p:BuildPlatform=cross
        else
            log_info "Building all targets (including net45)..."
            
            # net45 (仅当支持时)
            if check_net45_support; then
                log_info "Building net45..."
                export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
                /root/.dotnet/dotnet build FastData.sln -c Release -p:BuildPlatform=windows -p:RegisterForComInterop=false /p:TargetFramework=net452
                unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
                unset FrameworkPathOverride
            else
                log_warn "Skipping net45 (reference assemblies not installed)"
            fi
            
            # net6.0, net8.0, net10.0
            for fw in net6.0 net8.0 net10.0; do
                log_info "Building $fw..."
                /root/.dotnet/dotnet build FastData.sln -c Release -p:BuildPlatform=windows /p:TargetFramework=$fw
            done
        fi
        
        log_info "All frameworks built successfully!"
        
    elif [ "$framework" == "net45" ] || [ "$framework" == "net452" ] || [ "$framework" == "net462" ]; then
        # net45 需要 windows 平台
        if [ "$build_platform" != "windows" ]; then
            log_error "net45/net462 requires --platform windows"
            log_info "Usage: $0 --platform windows $framework"
            exit 1
        fi
        
        if check_net45_support; then
            log_info "Building $framework..."
            export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
            export FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5"
            /root/.dotnet/dotnet build FastData.sln -c Release -p:BuildPlatform=windows -p:RegisterForComInterop=false /p:TargetFramework=$framework
            unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
            unset FrameworkPathOverride
            log_info "$framework built successfully!"
        else
            log_error "$framework not supported (reference assemblies not installed)"
            exit 1
        fi
        
    elif [ "$framework" == "clean" ]; then
        log_info "Cleaning build outputs..."
        /root/.dotnet/dotnet clean FastData.sln
        log_info "Clean completed!"
        
    elif [ "$framework" == "run" ]; then
        log_info "Running FastData.Demo (net10.0)..."
        cd FastData.Demo
        /root/.dotnet/dotnet run --configuration Release
    else
        # 构建特定框架
        log_info "Building $framework (platform: $build_platform)..."
        /root/.dotnet/dotnet build FastData.sln -c Release -p:BuildPlatform=$build_platform /p:TargetFramework=$framework
        log_info "$framework built successfully!"
    fi
}

# 主程序
ARGS=()
parse_args "$@"

case "${ARGS[0]:-}" in
    net45|net452|net462|net6.0|net8.0|net10.0|clean|run|"")
        do_build "${ARGS[0]:-}"
        ;;
    -h|--help)
        show_help
        ;;
    *)
        log_error "Unknown command: ${ARGS[0]}"
        echo "Use '$0 --help' for usage information"
        exit 1
        ;;
esac
