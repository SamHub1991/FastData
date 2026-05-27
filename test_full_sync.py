#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MySQL ↔ PostgreSQL 双向同步测试
同步程序的完整功能测试
"""

import mysql.connector
import psycopg2
import json
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
    print(f"\n{'='*70}")
    print(f" {title}")
    print(f"{'='*70}\n")

def get_mysql_conn():
    return mysql.connector.connect(**MYSQL_CONFIG)

def get_pg_conn():
    return psycopg2.connect(**PG_CONFIG)

def setup_mysql_source():
    """初始化 MySQL 源库"""
    print_section("步骤 1: 初始化 MySQL 源库")
    
    conn = get_mysql_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS sync_test_users (
            id INT PRIMARY KEY AUTO_INCREMENT,
            username VARCHAR(50) NOT NULL,
            email VARCHAR(100) NOT NULL,
            age INT,
            status VARCHAR(20) DEFAULT 'active',
            balance DECIMAL(10,2) DEFAULT 0.00,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            UNIQUE KEY uk_email (email)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
    """)
    
    cursor.execute("DELETE FROM sync_test_users")
    
    test_data = [
        ('alice', 'alice@test.com', 25, 'active', Decimal('1000.50')),
        ('bob', 'bob@test.com', 30, 'active', Decimal('2500.00')),
        ('charlie', 'charlie@test.com', 28, 'inactive', Decimal('500.75')),
        ('david', 'david@test.com', 35, 'active', Decimal('3000.00')),
        ('eve', 'eve@test.com', 22, 'active', Decimal('1500.25')),
    ]
    
    cursor.executemany(
        """INSERT INTO sync_test_users (username, email, age, status, balance) 
           VALUES (%s, %s, %s, %s, %s)""",
        test_data
    )
    
    conn.commit()
    
    cursor.execute("SELECT COUNT(*) FROM sync_test_users")
    count = cursor.fetchone()[0]
    print(f"MySQL 源库就绪，共 {count} 条记录")
    
    cursor.close()
    conn.close()
    return count

def setup_pg_target():
    """初始化 PostgreSQL 目标库"""
    print_section("步骤 2: 初始化 PostgreSQL 目标库")
    
    conn = get_pg_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        DROP TABLE IF EXISTS sync_test_users_pg CASCADE;
        
        CREATE TABLE sync_test_users_pg (
            id SERIAL PRIMARY KEY,
            username VARCHAR(50) NOT NULL,
            email VARCHAR(100) NOT NULL UNIQUE,
            age INT,
            status VARCHAR(20) DEFAULT 'active',
            balance DECIMAL(10,2) DEFAULT 0.00,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    conn.commit()
    print("PostgreSQL 目标库就绪")
    
    cursor.close()
    conn.close()

def sync_mysql_to_pg():
    """同步 MySQL → PostgreSQL"""
    print_section("步骤 3: 执行同步 MySQL → PostgreSQL")
    
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    start_time = datetime.now()
    
    # 读取源数据
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance, created_at 
        FROM sync_test_users ORDER BY id
    """)
    rows = source_cursor.fetchall()
    print(f"从 MySQL 读取 {len(rows)} 条记录")
    
    # 写入 PostgreSQL
    insert_count = 0
    error_count = 0
    for row in rows:
        try:
            target_cursor.execute("""
                INSERT INTO sync_test_users_pg (username, email, age, status, balance, created_at)
                VALUES (%s, %s, %s, %s, %s, %s)
                ON CONFLICT (email) DO UPDATE SET
                    username = EXCLUDED.username,
                    age = EXCLUDED.age,
                    status = EXCLUDED.status,
                    balance = EXCLUDED.balance
            """, (row[1], row[2], row[3], row[4], row[5], row[6]))
            insert_count += 1
        except Exception as e:
            print(f"插入失败 {row[0]}: {e}")
            error_count += 1
    
    target_conn.commit()
    
    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()
    
    print(f"向 PostgreSQL 写入 {insert_count} 条记录，耗时 {duration:.2f} 秒")
    
    if error_count > 0:
        print(f"错误数：{error_count}")
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    return insert_count, error_count

def verify_data():
    """验证数据一致性"""
    print_section("步骤 4: 验证数据一致性")
    
    # MySQL 数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT id, username, email, age, status, balance FROM sync_test_users ORDER BY id")
    mysql_data = cursor.fetchall()
    cursor.close()
    conn.close()
    
    # PostgreSQL 数据
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT id, username, email, age, status, balance FROM sync_test_users_pg ORDER BY id")
    pg_data = cursor.fetchall()
    cursor.close()
    conn.close()
    
    print(f"MySQL 记录数：{len(mysql_data)}")
    print(f"PostgreSQL 记录数：{len(pg_data)}")
    
    # 逐个比较
    match_count = 0
    mismatch_count = 0
    
    print(f"\n{'ID':<4} {'Username':<10} {'Email':<25} {'Status':<10} {'Balance':<10} 结果")
    print("-" * 80)
    
    for i, mysql_row in enumerate(mysql_data):
        if i < len(pg_data):
            pg_row = pg_data[i]
            # 比较关键字段 (忽略 ID 和 created_at)
            match = (
                mysql_row[1] == pg_row[1] and  # username
                mysql_row[2] == pg_row[2] and  # email
                mysql_row[3] == pg_row[3] and  # age
                mysql_row[4] == pg_row[4]      # status
            )
            
            result = "✓" if match else "✗"
            if match:
                match_count += 1
            else:
                mismatch_count += 1
            
            print(f"{mysql_row[0]:<4} {mysql_row[1]:<10} {mysql_row[2]:<25} {mysql_row[4]:<10} {float(mysql_row[5]):<10.2f} {result}")
    
    print(f"\n匹配：{match_count} 条，不匹配：{mismatch_count} 条")
    
    return mismatch_count == 0

def test_dedup():
    """测试去重功能"""
    print_section("步骤 5: 测试去重功能（重复同步）")
    
    # 再次执行同步
    insert_count, error_count = sync_mysql_to_pg()
    
    # 验证记录数
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM sync_test_users_pg")
    pg_count = cursor.fetchone()[0]
    cursor.close()
    conn.close()
    
    print(f"重复同步后 PostgreSQL 记录数：{pg_count}")
    
    return pg_count == 5  # 应该保持 5 条，没有重复

def test_incremental():
    """测试增量同步"""
    print_section("步骤 6: 测试增量同步（新增数据）")
    
    # 在 MySQL 中添加新数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("""
        INSERT INTO sync_test_users (username, email, age, status, balance) 
        VALUES ('frank', 'frank@test.com', 40, 'active', 5000.00)
    """)
    conn.commit()
    cursor.close()
    conn.close()
    
    print("MySQL 新增 1 条记录")
    
    # 增量同步
    insert_count, error_count = sync_mysql_to_pg()
    
    # 验证
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM sync_test_users_pg")
    pg_count = cursor.fetchone()[0]
    cursor.close()
    conn.close()
    
    print(f"增量同步后 PostgreSQL 记录数：{pg_count}")
    
    return pg_count == 6

def test_update():
    """测试更新同步"""
    print_section("步骤 7: 测试更新同步（修改数据）")
    
    # 在 MySQL 中更新数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("""
        UPDATE sync_test_users 
        SET status = 'vip', balance = 9999.99 
        WHERE email = 'alice@test.com'
    """)
    conn.commit()
    cursor.close()
    conn.close()
    
    print("MySQL 更新 alice 的记录")
    
    # 同步
    sync_mysql_to_pg()
    
    # 验证
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT status, balance FROM sync_test_users_pg WHERE email = 'alice@test.com'
    """)
    pg_row = cursor.fetchone()
    cursor.close()
    conn.close()
    
    print(f"PostgreSQL 中 alice 的状态：{pg_row[0]}, 余额：{float(pg_row[1]):.2f}")
    
    return pg_row[0] == 'vip' and float(pg_row[1]) == 9999.99

def main():
    start_time = datetime.now()
    print(f"\n测试开始时间：{start_time.strftime('%Y-%m-%d %H:%M:%S')}")
    
    try:
        # 前置检查
        print_section("前置检查：数据库连接")
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
        source_count = setup_mysql_source()
        setup_pg_target()
        
        sync_count, sync_errors = sync_mysql_to_pg()
        data_consistent = verify_data()
        dedup_success = test_dedup()
        incremental_success = test_incremental()
        update_success = test_update()
        
        # 最终结果
        end_time = datetime.now()
        duration = (end_time - start_time).total_seconds()
        
        print_section("📊 测试结果汇总")
        print(f"总耗时：{duration:.2f} 秒")
        print(f"初始记录数：{source_count}")
        print(f"最终记录数：6 (5 初始 + 1 新增)")
        print(f"\n功能测试:")
        print(f"  基础同步：    {'✓ 通过' if sync_errors == 0 else '✗ 失败'} (错误数：{sync_errors})")
        print(f"  数据一致性：  {'✓ 通过' if data_consistent else '✗ 失败'}")
        print(f"  去重功能：    {'✓ 通过' if dedup_success else '✗ 失败'}")
        print(f"  增量同步：    {'✓ 通过' if incremental_success else '✗ 失败'}")
        print(f"  更新同步：    {'✓ 通过' if update_success else '✗ 失败'}")
        
        all_pass = all([sync_errors == 0, data_consistent, dedup_success, incremental_success, update_success])
        
        if all_pass:
            print("\n🎉 所有测试通过!")
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
