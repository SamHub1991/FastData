#!/bin/bash
# 系统集成测试脚本
# 测试时长：1小时（3600秒）
# 测试内容：数据库连接持续性测试 + 数据同步并行测试

set -e

API_BASE="http://localhost:5000"
TEST_DURATION=3600  # 1小时 = 3600秒
INTERVAL=10  # 每10秒测试一次
LOG_FILE="/workspace/docs/integration-test-log.txt"
ISSUES_FILE="/workspace/docs/integration-test-issues.md"

# 初始化日志文件
echo "# 系统集成测试日志" > "$LOG_FILE"
echo "测试开始时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$LOG_FILE"
echo "测试时长: 1小时" >> "$LOG_FILE"
echo "========================================" >> "$LOG_FILE"
echo "" >> "$LOG_FILE"

# 初始化问题文件
echo "# 系统集成测试问题记录" > "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"
echo "## 测试环境" >> "$ISSUES_FILE"
echo "- 测试开始时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$ISSUES_FILE"
echo "- 测试时长: 1小时" >> "$ISSUES_FILE"
echo "- API地址: $API_BASE" >> "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"
echo "## 发现的问题" >> "$ISSUES_FILE"
echo "" >> "$ISSUES_FILE"

# 统计变量
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
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

# 测试函数
test_endpoint() {
    local name=$1
    local endpoint=$2
    local method=${3:-GET}
    local data=$4
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    local start_time=$(date +%s%N)
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$API_BASE$endpoint" 2>/dev/null)
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" -H "Content-Type: application/json" -d "$data" "$API_BASE$endpoint" 2>/dev/null)
    fi
    
    local end_time=$(date +%s%N)
    local duration=$(( (end_time - start_time) / 1000000 ))
    
    local http_code=$(echo "$response" | tail -1)
    local body=$(echo "$response" | head -n -1)
    
    if [ "$http_code" = "200" ]; then
        PASSED_TESTS=$((PASSED_TESTS + 1))
        echo "[$(date '+%H:%M:%S')] ✓ $name - ${duration}ms - HTTP $http_code" >> "$LOG_FILE"
    else
        FAILED_TESTS=$((FAILED_TESTS + 1))
        echo "[$(date '+%H:%M:%S')] ✗ $name - ${duration}ms - HTTP $http_code" >> "$LOG_FILE"
        record_issue "HIGH" "连接失败" "$name 测试失败" "HTTP状态码: $http_code, 响应: $body"
    fi
    
    # 检查响应时间
    if [ $duration -gt 5000 ]; then
        record_issue "MEDIUM" "性能问题" "$name 响应时间过长" "响应时间: ${duration}ms (阈值: 5000ms)"
    fi
}

echo "========================================="
echo "系统集成测试开始"
echo "========================================="
echo ""

# 循环测试
START_TIME=$(date +%s)
ITERATION=0

while true; do
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - START_TIME))
    
    if [ $ELAPSED -ge $TEST_DURATION ]; then
        break
    fi
    
    ITERATION=$((ITERATION + 1))
    REMAINING=$(( (TEST_DURATION - ELAPSED) / 60 ))
    
    echo "----------------------------------------"
    echo "第 $ITERATION 次测试 (剩余 ${REMAINING} 分钟)"
    echo "----------------------------------------"
    
    echo "" >> "$LOG_FILE"
    echo "## 第 $ITERATION 次测试 ($(date '+%H:%M:%S'))" >> "$LOG_FILE"
    
    # 测试环境配置
    test_endpoint "环境配置" "/api/config/environment"
    
    # 测试连接列表
    test_endpoint "连接列表" "/api/config/connections"
    
    # 测试 SQL Server
    test_endpoint "SQL Server 连接" "/api/config/connections/SqlServer"
    
    # 测试 MySQL
    test_endpoint "MySQL 连接" "/api/config/connections/MySql"
    
    # 测试 PostgreSQL
    test_endpoint "PostgreSQL 连接" "/api/config/connections/PostgreSql"
    
    # 测试 Redis
    test_endpoint "Redis 配置" "/api/config/redis"
    
    # 测试配置摘要
    test_endpoint "配置摘要" "/api/config/summary"
    
    # 测试 SQL Server CRUD
    test_endpoint "SQL Server GET All" "/api/users"
    test_endpoint "SQL Server GET By ID" "/api/users/1"
    
    # 测试 MySQL CRUD
    test_endpoint "MySQL GET All" "/api/mysql-users"
    test_endpoint "MySQL GET By ID" "/api/mysql-users/1"
    
    # 测试 PostgreSQL CRUD
    test_endpoint "PostgreSQL GET All" "/api/pg-users"
    test_endpoint "PostgreSQL GET By ID" "/api/pg-users/1"
    
    echo ""
    
    # 等待下一次测试
    sleep $INTERVAL
done

# 测试结束
echo "" >> "$LOG_FILE"
echo "========================================" >> "$LOG_FILE"
echo "测试结束时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$LOG_FILE"
echo "总测试次数: $TOTAL_TESTS" >> "$LOG_FILE"
echo "通过次数: $PASSED_TESTS" >> "$LOG_FILE"
echo "失败次数: $FAILED_TESTS" >> "$LOG_FILE"
echo "成功率: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%" >> "$LOG_FILE"
echo "发现问题数: $ISSUES_COUNT" >> "$LOG_FILE"

# 更新问题文件的总结
echo "" >> "$ISSUES_FILE"
echo "## 测试总结" >> "$ISSUES_FILE"
echo "- 测试结束时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$ISSUES_FILE"
echo "- 总测试次数: $TOTAL_TESTS" >> "$ISSUES_FILE"
echo "- 通过次数: $PASSED_TESTS" >> "$ISSUES_FILE"
echo "- 失败次数: $FAILED_TESTS" >> "$ISSUES_FILE"
echo "- 成功率: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%" >> "$ISSUES_FILE"
echo "- 发现问题数: $ISSUES_COUNT" >> "$ISSUES_FILE"

echo ""
echo "========================================="
echo "系统集成测试完成"
echo "========================================="
echo "总测试次数: $TOTAL_TESTS"
echo "通过次数: $PASSED_TESTS"
echo "失败次数: $FAILED_TESTS"
echo "成功率: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%"
echo "发现问题数: $ISSUES_COUNT"
echo ""
echo "测试日志: $LOG_FILE"
echo "问题记录: $ISSUES_FILE"
