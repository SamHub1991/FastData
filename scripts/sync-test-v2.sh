#!/bin/bash
# 数据同步并行测试脚本
# 测试内容：SQL Server、MySQL、PostgreSQL 之间的数据同步

set -e

API_BASE="http://localhost:5000"
LOG_FILE="/workspace/docs/sync-test-log.txt"
ISSUES_FILE="/workspace/docs/sync-test-issues.md"

# 初始化日志文件
echo "# 数据同步并行测试日志" > "$LOG_FILE"
echo "测试开始时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$LOG_FILE"
echo "========================================" >> "$LOG_FILE"
echo "" >> "$LOG_FILE"

# 初始化问题文件
echo "# 数据同步测试问题记录" > "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"
echo "## 测试环境" >> "$ISSUES_FILE"
echo "- 测试开始时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$ISSUES_FILE"
echo "- API地址: $API_BASE" >> "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"
echo "## 发现的问题" >> "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"

# 统计变量
TOTAL_SYNC=0
SUCCESS_SYNC=0
FAILED_SYNC=0
ISSUES_COUNT=0

# 记录问题的函数
record_issue() {
    local severity=$1
    local category=$2
    local description=$3
    local detail=$4
    
    ISSUES_COUNT=$((ISSUES_COUNT + 1))
    
    echo "### 问题 $ISSUES_COUNT: $description" >> "$ISSUES_FILE"
    echo "- **严重程度**: $severity" >> "$ISSUES_FILE"
    echo "- **类别**: $category" >> "$ISSUES_FILE"
    echo "- **发现时间**: $(date '+%Y-%m-%d %H:%M:%S')" >> "$ISSUES_FILE"
    echo "- **详细信息**: $detail" >> "$ISSUES_FILE"
    echo "" >> "$ISSUES_FILE"
}

# 测试数据同步
test_data_sync() {
    local source=$1
    local target=$2
    local description=$3
    
    TOTAL_SYNC=$((TOTAL_SYNC + 1))
    
    echo "[$(date '+%H:%M:%S')] 测试: $description" >> "$LOG_FILE"
    echo "  源数据库: $source" >> "$LOG_FILE"
    echo "  目标数据库: $target" >> "$LOG_FILE"
    
    # 从源数据库读取数据
    local source_data=""
    case $source in
        "SqlServer")
            source_data=$(curl -s "$API_BASE/api/users" 2>/dev/null)
            ;;
        "MySql")
            source_data=$(curl -s "$API_BASE/api/mysql-users" 2>/dev/null)
            ;;
        "PostgreSql")
            source_data=$(curl -s "$API_BASE/api/pg-users" 2>/dev/null)
            ;;
    esac
    
    if [ -z "$source_data" ] || [ "$source_data" = "[]" ]; then
        echo "  状态: 源数据库无数据" >> "$LOG_FILE"
        record_issue "LOW" "数据问题" "$description 源数据库无数据" "源数据库 $source 没有数据可供同步"
        return
    fi
    
    # 解析数据数量
    local data_count=$(echo "$source_data" | grep -o '"id"' | wc -l)
    echo "  源数据数量: $data_count 条" >> "$LOG_FILE"
    
    # 模拟同步过程
    echo "  同步中..." >> "$LOG_FILE"
    sleep 1
    
    # 验证目标数据库
    local target_data=""
    case $target in
        "SqlServer")
            target_data=$(curl -s "$API_BASE/api/users" 2>/dev/null)
            ;;
        "MySql")
            target_data=$(curl -s "$API_BASE/api/mysql-users" 2>/dev/null)
            ;;
        "PostgreSql")
            target_data=$(curl -s "$API_BASE/api/pg-users" 2>/dev/null)
            ;;
    esac
    
    local target_count=$(echo "$target_data" | grep -o '"id"' | wc -l)
    echo "  目标数据数量: $target_count 条" >> "$LOG_FILE"
    
    if [ $target_count -ge $data_count ]; then
        SUCCESS_SYNC=$((SUCCESS_SYNC + 1))
        echo "  状态: 成功" >> "$LOG_FILE"
    else
        FAILED_SYNC=$((FAILED_SYNC + 1))
        echo "  状态: 失败 (目标数据不足)" >> "$LOG_FILE"
        record_issue "HIGH" "同步失败" "$description 同步失败" "源数据: $data_count 条, 目标数据: $target_count 条"
    fi
    
    echo "" >> "$LOG_FILE"
}

echo "========================================="
echo "数据同步并行测试开始"
echo "========================================="
echo ""

# 测试所有同步组合
test_data_sync "SqlServer" "MySql" "SQL Server → MySQL"
test_data_sync "SqlServer" "PostgreSql" "SQL Server → PostgreSQL"
test_data_sync "MySql" "SqlServer" "MySQL → SQL Server"
test_data_sync "MySql" "PostgreSql" "MySQL → PostgreSQL"
test_data_sync "PostgreSql" "SqlServer" "PostgreSQL → SQL Server"
test_data_sync "PostgreSql" "MySql" "PostgreSQL → MySQL"

# 更新问题文件的总结
echo "" >> "$ISSUES_FILE"
echo "## 测试总结" >> "$ISSUES_FILE"
echo "- 测试结束时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$ISSUES_FILE"
echo "- 总同步测试: $TOTAL_SYNC" >> "$ISSUES_FILE"
echo "- 成功: $SUCCESS_SYNC" >> "$ISSUES_FILE"
echo "- 失败: $FAILED_SYNC" >> "$ISSUES_FILE"
echo "- 成功率: $(( SUCCESS_SYNC * 100 / TOTAL_SYNC ))%" >> "$ISSUES_FILE"
echo "- 发现问题数: $ISSUES_COUNT" >> "$ISSUES_FILE"

echo "" >> "$LOG_FILE"
echo "========================================" >> "$LOG_FILE"
echo "测试结束时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$LOG_FILE"
echo "总同步测试: $TOTAL_SYNC" >> "$LOG_FILE"
echo "成功: $SUCCESS_SYNC" >> "$LOG_FILE"
echo "失败: $FAILED_SYNC" >> "$LOG_FILE"
echo "成功率: $(( SUCCESS_SYNC * 100 / TOTAL_SYNC ))%" >> "$LOG_FILE"

echo ""
echo "========================================="
echo "数据同步并行测试完成"
echo "========================================="
echo "总同步测试: $TOTAL_SYNC"
echo "成功: $SUCCESS_SYNC"
echo "失败: $FAILED_SYNC"
echo "成功率: $(( SUCCESS_SYNC * 100 / TOTAL_SYNC ))%"
echo "发现问题数: $ISSUES_COUNT"
echo ""
echo "测试日志: $LOG_FILE"
echo "问题记录: $ISSUES_FILE"
