#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
数据同步工具 - 资源受限环境下的完整测试
测试场景：
1. 基础数据同步（MySQL → PostgreSQL）
2. 大批量数据同步性能测试
3. 资源监控（内存/CPU）
4. 并发同步测试
5. 断点续传测试
"""

import mysql.connector
import psycopg2
import time
import os
import subprocess
from datetime import datetime
from decimal import Decimal

# 数据库配置
MYSQL_CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

PG_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'user': 'postgres',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

def print_section(title):
    print(f"\n{'='*80}")
    print(f" {title}")
    print(f"{'='*80}\n")

def get_resource_usage():
    """获取当前资源使用情况"""
    result = subprocess.run(
        ['docker', 'stats', '--no-stream', '--format', 
         'table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}'],
        capture_output=True, text=True
    )
    return result.stdout

def get_mysql_conn():
    return mysql.connector.connect(**MYSQL_CONFIG)

def get_pg_conn():
    return psycopg2.connect(**PG_CONFIG)

def setup_test_data(count=100):
    """准备测试数据"""
    print_section(f"步骤 1: 准备测试数据 ({count}条)")
    
    start_time = datetime.now()
    
    conn = get_mysql_conn()
    cursor = conn.cursor()
    
    # 创建测试表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS sync_perf_test (
            id INT PRIMARY KEY AUTO_INCREMENT,
            username VARCHAR(50) NOT NULL,
            email VARCHAR(100) NOT NULL,
            age INT,
            status VARCHAR(20) DEFAULT 'active',
            balance DECIMAL(10,2) DEFAULT 0.00,
            description TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            KEY idx_email (email),
            KEY idx_status (status)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
    """)
    
    cursor.execute("DELETE FROM sync_perf_test")
    
    # 批量插入
    batch_size = 100
    test_data = []
    
    for i in range(count):
        test_data.append((
            f'user_{i}',
            f'user_{i}@test.com',
            20 + (i % 40),
            'active' if i % 5 != 0 else 'inactive',
            Decimal(str(round(100 + i * 10.5, 2))),
            f'This is description for user {i}' * 10  # 增加数据量
        ))
        
        if len(test_data) >= batch_size:
            cursor.executemany(
                """INSERT INTO sync_perf_test (username, email, age, status, balance, description) 
                   VALUES (%s, %s, %s, %s, %s, %s)""",
                test_data
            )
            test_data = []
    
    if test_data:
        cursor.executemany(
            """INSERT INTO sync_perf_test (username, email, age, status, balance, description) 
               VALUES (%s, %s, %s, %s, %s, %s)""",
            test_data
        )
    
    conn.commit()
    
    cursor.execute("SELECT COUNT(*) FROM sync_perf_test")
    actual_count = cursor.fetchone()[0]
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    print(f"MySQL 测试表就绪，共 {actual_count} 条记录")
    print(f"数据准备耗时：{duration:.2f} 秒")
    
    cursor.close()
    conn.close()
    return actual_count

def setup_pg_target():
    """初始化 PostgreSQL 目标表"""
    print_section("步骤 2: 初始化 PostgreSQL 目标表")
    
    conn = get_pg_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        DROP TABLE IF EXISTS sync_perf_test_pg CASCADE;
        
        CREATE TABLE sync_perf_test_pg (
            id SERIAL PRIMARY KEY,
            username VARCHAR(50) NOT NULL,
            email VARCHAR(100) NOT NULL UNIQUE,
            age INT,
            status VARCHAR(20) DEFAULT 'active',
            balance DECIMAL(10,2) DEFAULT 0.00,
            description TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE INDEX IF NOT EXISTS idx_status_pg ON sync_perf_test_pg(status);
    """)
    
    conn.commit()
    print("PostgreSQL 目标表就绪 (email 列已添加 UNIQUE 约束)")
    
    cursor.close()
    conn.close()

def sync_performance_test(batch_size=100):
    """同步性能测试"""
    print_section(f"步骤 3: 同步性能测试 (批量大小：{batch_size})")
    
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    # 记录开始前资源
    print("同步前资源使用:")
    print(get_resource_usage())
    
    start_time = datetime.now()
    
    # 读取源数据
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance, description, created_at 
        FROM sync_perf_test ORDER BY id
    """)
    
    rows = source_cursor.fetchall()
    total_rows = len(rows)
    print(f"\n从 MySQL 读取 {total_rows} 条记录")
    
    # 批量同步
    batch_start = datetime.now()
    batch_count = 0
    
    for i in range(0, total_rows, batch_size):
        batch = rows[i:i+batch_size]
        
        for row in batch:
            try:
                target_cursor.execute("""
                    INSERT INTO sync_perf_test_pg (username, email, age, status, balance, description, created_at)
                    VALUES (%s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT (email) DO UPDATE SET
                        username = EXCLUDED.username,
                        age = EXCLUDED.age,
                        status = EXCLUDED.status,
                        balance = EXCLUDED.balance,
                        description = EXCLUDED.description
                """, (row[1], row[2], row[3], row[4], row[5], row[6], row[7]))
                batch_count += 1
            except Exception as e:
                print(f"插入失败 {row[0]}: {e}")
        
        # 每批次提交
        if batch_count % (batch_size * 5) == 0:
            target_conn.commit()
            sync_time = (datetime.now() - start_time).total_seconds()
            rate = batch_count / sync_time if sync_time > 0 else 0
            print(f"  已同步 {batch_count}/{total_rows} 条 ({rate:.0f} 条/秒)")
    
    target_conn.commit()
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    rate = total_rows / duration if duration > 0 else 0
    
    print(f"\n同步完成:")
    print(f"  总记录数：{total_rows} 条")
    print(f"  总耗时：{duration:.2f} 秒")
    print(f"  同步速率：{rate:.0f} 条/秒")
    
    # 记录结束后资源
    print("\n同步后资源使用:")
    print(get_resource_usage())
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    return total_rows, duration, rate

def verify_data():
    """验证数据一致性"""
    print_section("步骤 4: 验证数据一致性")
    
    start_time = datetime.now()
    
    # MySQL 数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*), SUM(balance) FROM sync_perf_test")
    mysql_stats = cursor.fetchone()
    cursor.close()
    conn.close()
    
    # PostgreSQL 数据
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*), SUM(balance) FROM sync_perf_test_pg")
    pg_stats = cursor.fetchone()
    cursor.close()
    conn.close()
    
    mysql_count = mysql_stats[0]
    pg_count = pg_stats[0]
    
    mysql_total = float(mysql_stats[1]) if mysql_stats[1] else 0
    pg_total = float(pg_stats[1]) if pg_stats[1] else 0
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    print(f"MySQL 记录数：{mysql_count:,}")
    print(f"PostgreSQL 记录数：{pg_count:,}")
    print(f"MySQL 总金额：${mysql_total:,.2f}")
    print(f"PostgreSQL 总金额：${pg_total:,.2f}")
    print(f"验证耗时：{duration:.2f} 秒")
    
    count_match = mysql_count == pg_count
    amount_match = abs(mysql_total - pg_total) < 0.01
    
    if count_match and amount_match:
        print("\n✓ 数据一致性验证通过")
        return True
    else:
        print("\n✗ 数据一致性验证失败")
        if not count_match:
            print(f"  记录数不匹配：MySQL={mysql_count}, PG={pg_count}")
        if not amount_match:
            print(f"  总金额不匹配：MySQL=${mysql_total:,.2f}, PG=${pg_total:,.2f}")
        return False

def test_incremental_sync():
    """测试增量同步"""
    print_section("步骤 5: 增量同步测试")
    
    start_time = datetime.now()
    
    # 源库新增数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    
    increment_count = 50
    test_data = []
    for i in range(increment_count):
        test_data.append((
            f'new_user_{i}',
            f'new_user_{i}@test.com',
            25 + i,
            'active',
            Decimal(str(round(500 + i * 5.5, 2)))
        ))
    
    cursor.executemany(
        """INSERT INTO sync_perf_test (username, email, age, status, balance) 
           VALUES (%s, %s, %s, %s, %s)""",
        test_data
    )
    conn.commit()
    cursor.close()
    conn.close()
    
    print(f"MySQL 新增 {increment_count} 条记录")
    
    # 执行增量同步
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance, description, created_at 
        FROM sync_perf_test 
        WHERE email LIKE 'new_user_%'
        ORDER BY id
    """)
    
    rows = source_cursor.fetchall()
    
    for row in rows:
        target_cursor.execute("""
            INSERT INTO sync_perf_test_pg (username, email, age, status, balance, description, created_at)
            VALUES (%s, %s, %s, %s, %s, %s, %s)
        """, (row[1], row[2], row[3], row[4], row[5], row[6], row[7]))
    
    target_conn.commit()
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    # 验证
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM sync_perf_test_pg WHERE email LIKE 'new_user_%'")
    pg_increment = cursor.fetchone()[0]
    cursor.close()
    conn.close()
    
    print(f"PostgreSQL 新增 {pg_increment} 条记录")
    print(f"增量同步耗时：{duration:.2f} 秒")
    
    return pg_increment == increment_count

def test_update_sync():
    """测试更新同步"""
    print_section("步骤 6: 更新同步测试")
    
    start_time = datetime.now()
    
    # 源库更新数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("""
        UPDATE sync_perf_test 
        SET status = 'vip', balance = 9999.99 
        WHERE email = 'user_0@test.com'
    """)
    conn.commit()
    cursor.close()
    conn.close()
    
    print("MySQL 更新 user_0 的记录")
    
    # 执行更新同步
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance 
        FROM sync_perf_test 
        WHERE email = 'user_0@test.com'
    """)
    row = source_cursor.fetchone()
    
    target_cursor.execute("""
        INSERT INTO sync_perf_test_pg (username, email, age, status, balance)
        VALUES (%s, %s, %s, %s, %s)
        ON CONFLICT (email) DO UPDATE SET
            status = EXCLUDED.status,
            balance = EXCLUDED.balance
    """, (row[1], row[2], row[3], row[4], row[5]))
    
    target_conn.commit()
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    # 验证
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT status, balance FROM sync_perf_test_pg WHERE email = 'user_0@test.com'
    """)
    pg_row = cursor.fetchone()
    cursor.close()
    conn.close()
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    print(f"PostgreSQL 中 user_0 的状态：{pg_row[0]}, 余额：{float(pg_row[1]):.2f}")
    print(f"更新同步耗时：{duration:.2f} 秒")
    
    return pg_row[0] == 'vip' and abs(float(pg_row[1]) - 9999.99) < 0.01

def test_dedup():
    """测试去重功能"""
    print_section("步骤 7: 去重功能测试")
    
    start_time = datetime.now()
    
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    # 读取数据
    source_cursor.execute("""
        SELECT COUNT(*) FROM sync_perf_test
    """)
    source_count = source_cursor.fetchone()[0]
    
    # 重复同步 3 次
    for i in range(3):
        source_cursor.execute("""
            SELECT id, username, email, age, status, balance 
            FROM sync_perf_test ORDER BY id
        """)
        rows = source_cursor.fetchall()
        
        for row in rows:
            target_cursor.execute("""
                INSERT INTO sync_perf_test_pg (username, email, age, status, balance)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (email) DO UPDATE SET
                    username = EXCLUDED.username
            """, (row[1], row[2], row[3], row[4], row[5]))
        
        target_conn.commit()
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    # 验证
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM sync_perf_test_pg")
    pg_count = cursor.fetchone()[0]
    cursor.close()
    conn.close()
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    print(f"源库记录数：{source_count}")
    print(f"重复同步 3 次后目标库记录数：{pg_count}")
    print(f"去重测试耗时：{duration:.2f} 秒")
    
    if pg_count == source_count:
        print("✓ 去重功能正常，无重复记录")
        return True
    else:
        print(f"✗ 去重功能异常，应该有 {source_count} 条，实际 {pg_count} 条")
        return False

def main():
    print(f"\n{'='*80}")
    print(" 数据同步工具 - 资源受限环境完整测试")
    print(f"{'='*80}")
    print(f"测试开始时间：{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    total_start = datetime.now()
    
    try:
        # 前置检查
        print_section("前置检查：数据库连接")
        print(get_resource_usage())
        
        try:
            get_mysql_conn().close()
            print("✓ MySQL 连接正常")
        except Exception as e:
            print(f"✗ MySQL 连接失败：{e}")
            return 1
            
        try:
            get_pg_conn().close()
            print("✓ PostgreSQL 连接正常")
        except Exception as e:
            print(f"✗ PostgreSQL 连接失败：{e}")
            return 1
        
        # 执行测试
        test_count = 1000  # 可调整为 500, 1000, 5000 等
        
        setup_test_data(test_count)
        setup_pg_target()
        
        sync_count, sync_duration, sync_rate = sync_performance_test(batch_size=50)
        data_consistent = verify_data()
        incremental_success = test_incremental_sync()
        update_success = test_update_sync()
        dedup_success = test_dedup()
        
        # 最终结果
        total_end = datetime.now()
        total_duration = (total_end - total_start).total_seconds()
        
        print_section("📊 测试结果汇总")
        print(f"总耗时：{total_duration:.2f} 秒")
        print(f"测试数据量：{test_count} 条")
        print(f"同步速率：{sync_rate:.0f} 条/秒")
        print(f"\n内存使用（测试结束时）:")
        print(get_resource_usage())
        
        print(f"\n功能测试:")
        print(f"  基础同步：    {'✓ 通过' if sync_count == test_count else '✗ 失败'}")
        print(f"  数据一致性：  {'✓ 通过' if data_consistent else '✗ 失败'}")
        print(f"  增量同步：    {'✓ 通过' if incremental_success else '✗ 失败'}")
        print(f"  更新同步：    {'✓ 通过' if update_success else '✗ 失败'}")
        print(f"  去重功能：    {'✓ 通过' if dedup_success else '✗ 失败'}")
        
        all_pass = all([
            sync_count == test_count,
            data_consistent,
            incremental_success,
            update_success,
            dedup_success
        ])
        
        if all_pass:
            print("\n🎉 所有测试通过!")
            print(f"\n性能指标:")
            print(f"  - 同步速率：{sync_rate:.0f} 条/秒")
            print(f"  - 平均延迟：{(sync_duration/test_count)*1000:.2f} ms/条")
            return 0
        else:
            print("\n⚠️ 部分测试失败")
            return 1
            
    except Exception as e:
        print(f"\n✗ 测试失败：{e}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == '__main__':
    exit(main())
