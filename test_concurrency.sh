#!/bin/bash
# 高并发 API 测试脚本

BASE_URL="http://localhost:5001/api/ConcurrencyTest"
CONCURRENT_USERS=50
TOTAL_REQUESTS=100

echo "=========================================="
echo "FastData 高并发 API 测试"
echo "=========================================="
echo ""

# 检查服务是否运行
echo "检查服务状态..."
curl -s "$BASE_URL/stats" > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "错误: 服务未运行，请先启动 FastData.Demo"
    echo "运行命令: cd FastData.Demo && dotnet run"
    exit 1
fi
echo "服务运行正常"
echo ""

# 重置统计
echo "重置统计数据..."
curl -s -X POST "$BASE_URL/reset" > /dev/null
echo ""

# 测试1: 单条查询
echo "测试1: 单条查询 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/query/single?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试2: 批量查询
echo "测试2: 批量查询 (并发=$CONCURRENT_USERS, 每批100条)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/query/batch?pageSize=100&dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试3: 写入操作
echo "测试3: 写入操作 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s -X POST "$BASE_URL/write/insert?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试4: 更新操作
echo "测试4: 更新操作 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s -X PUT "$BASE_URL/write/update?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试5: 删除操作
echo "测试5: 删除操作 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s -X DELETE "$BASE_URL/write/delete?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试6: 事务操作
echo "测试6: 事务操作 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s -X POST "$BASE_URL/transaction?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试7: 分页查询
echo "测试7: 分页查询 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/query/paged?page=1&pageSize=20&dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试8: 聚合查询
echo "测试8: 聚合查询 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/query/aggregate?dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试9: 多数据库并发查询
echo "测试9: 多数据库并发查询 (并发=$CONCURRENT_USERS)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/query/multi-db" > /dev/null &
done
wait
echo ""

# 测试10: 批量写入
echo "测试10: 批量写入 (并发=10, 每批100条)"
for i in $(seq 1 10); do
    curl -s -X POST "$BASE_URL/write/bulk?count=100&dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试11: 连接池压力测试
echo "测试11: 连接池压力测试 (并发=$CONCURRENT_USERS, 每次10次迭代)"
for i in $(seq 1 $CONCURRENT_USERS); do
    curl -s "$BASE_URL/pool/stress?iterations=10&dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 测试12: 混合读写压力测试
echo "测试12: 混合读写压力测试 (并发=20, 每次100次操作)"
for i in $(seq 1 20); do
    curl -s -X POST "$BASE_URL/mixed/stress?count=100&dbKey=SqlServer" > /dev/null &
done
wait
echo ""

# 获取统计结果
echo "=========================================="
echo "测试结果统计"
echo "=========================================="
curl -s "$BASE_URL/stats" | python3 -m json.tool 2>/dev/null || curl -s "$BASE_URL/stats"
echo ""
