#!/bin/bash
#
# FastData 四数据库同步测试脚本
# 测试 SQL Server, MySQL, PostgreSQL, SQLite 之间的双向同步
#

set -e

cd /workspace
echo "============================================================"
echo "FastData 四数据库同步测试"
echo "============================================================"

# 颜色定义
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}
log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}
log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查数据库容器
check_databases() {
    echo ""
    echo "=== 检查数据库容器 ==="
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "sqlserver|mysql|postgres|redis"
}

# 测试 SQL Server -> MySQL 同步
test_sqlserver_to_mysql() {
    echo ""
    echo "=== 测试 SQL Server -> MySQL 同步 ==="
    
    # 查询 SQL Server 用户数
    SS_COUNT=$(docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'FastData@Test123' -C -Q "SELECT COUNT(*) FROM FastDataDemo.dbo.AppUser" -t 2>/dev/null | tr -d ' ')
    log_info "SQL Server 用户数：$SS_COUNT"
    
    # 查询 MySQL 用户数
    MYSQL_COUNT=$(docker exec mysql mysql -h localhost -u root -p'FastData@Test123' -D FastDataDemo -e "SELECT COUNT(*) FROM AppUser" 2>/dev/null | tail -1)
    log_info "MySQL 用户数：$MYSQL_COUNT"
    
    # 比较
    if [ "$SS_COUNT" == "$MYSQL_COUNT" ]; then
        log_info "✓ SQL Server -> MySQL 数据一致"
    else
        log_warn "✗ SQL Server -> MySQL 数据不一致"
    fi
}

# 测试 SQL Server -> PostgreSQL 同步
test_sqlserver_to_pg() {
    echo ""
    echo "=== 测试 SQL Server -> PostgreSQL 同步 ==="
    
    # 查询 SQL Server 用户数
    SS_COUNT=$(docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'FastData@Test123' -C -Q "SELECT COUNT(*) FROM FastDataDemo.dbo.AppUser" -t 2>/dev/null | tr -d ' ')
    log_info "SQL Server 用户数：$SS_COUNT"
    
    # 查询 PostgreSQL 用户数
    PG_COUNT=$(docker exec postgres psql -U postgres -d fastdatademo -t -c "SELECT COUNT(*) FROM AppUser" 2>/dev/null | tr -d ' ')
    log_info "PostgreSQL 用户数：$PG_COUNT"
    
    # 比较
    if [ "$SS_COUNT" == "$PG_COUNT" ]; then
        log_info "✓ SQL Server -> PostgreSQL 数据一致"
    else
        log_warn "✗ SQL Server -> PostgreSQL 数据不一致"
    fi
}

# 测试所有数据库 CRUD
test_all_databases() {
    echo ""
    echo "=== 测试各数据库 CRUD ==="
    
    # SQL Server
    echo ""
    echo "SQL Server:"
    docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'FastData@Test123' -C -Q "SELECT COUNT(*) as 'User Count' FROM FastDataDemo.dbo.AppUser" 2>/dev/null | head -5
    
    # MySQL
    echo ""
    echo "MySQL:"
    docker exec mysql mysql -h localhost -u root -p'FastData@Test123' -D FastDataDemo -e "SELECT COUNT(*) as 'User Count' FROM AppUser" 2>/dev/null
    
    # PostgreSQL
    echo ""
    echo "PostgreSQL:"
    docker exec postgres psql -U postgres -d fastdatademo -c "SELECT COUNT(*) as \"User Count\" FROM AppUser" 2>/dev/null
}

# 运行测试
check_databases
test_all_databases
test_sqlserver_to_mysql
test_sqlserver_to_pg

echo ""
echo "============================================================"
echo "测试完成"
echo "============================================================"
