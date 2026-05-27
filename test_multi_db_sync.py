#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
多数据库同步完整测试
测试 MySQL、SQL Server、PostgreSQL 之间的数据同步
"""

import mysql.connector
import psycopg2
import pyodbc
import time
from datetime import datetime

# 数据库配置
MYSQL_CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

MSSQL_CONFIG = {
    'driver': '{ODBC Driver 17 for SQL Server}',
    'server': 'localhost',
    'port': 1433,
    'user': 'sa',
    'password': 'FastData@Test123',
    'database': 'testdb',
    'TrustServerCertificate': 'yes'
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

def get_mssql_conn():
    conn_str = f"DRIVER={MSSQL_CONFIG['driver']};SERVER={MSSQL_CONFIG['server']},{MSSQL_CONFIG['port']};UID={MSSQL_CONFIG['user']};PWD={MSSQL_CONFIG['password']};DATABASE={MSSQL_CONFIG['database']};TrustServerCertificate={MSSQL_CONFIG['TrustServerCertificate']}"
    return pyodbc.connect(conn_str)

def setup_mysql():
    """初始化 MySQL 源库"""
    print_section("步骤 1: 初始化 MySQL 源库")
    
    conn = get_mysql_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS users_source (
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
    
    cursor.execute("DELETE FROM users_source")
    
    test_data = [
        ('alice', 'alice@test.com', 25, 'active', 1000.50),
        ('bob', 'bob@test.com', 30, 'active', 2500.00),
        ('charlie', 'charlie@test.com', 28, 'inactive', 500.75),
        ('david', 'david@test.com', 35, 'active', 3000.00),
        ('eve', 'eve@test.com', 22, 'active', 1500.25),
        ('frank', 'frank@test.com', 40, 'active', 5000.00),
        ('grace', 'grace@test.com', 27, 'inactive', 2000.00),
    ]
    
    cursor.executemany(
        """INSERT INTO users_source (username, email, age, status, balance) 
           VALUES (%s, %s, %s, %s, %s)""",
        test_data
    )
    
    conn.commit()
    
    cursor.execute("SELECT COUNT(*) FROM users_source")
    count = cursor.fetchone()[0]
    print(f"MySQL 源库就绪，共 {count} 条记录")
    
    cursor.execute("SELECT * FROM users_source")
    for row in cursor.fetchall():
        print(f"  {row}")
    
    cursor.close()
    conn.close()
    return count

def setup_mssql():
    """初始化 SQL Server 目标库"""
    print_section("步骤 2: 初始化 SQL Server 目标库")
    
    conn = get_mssql_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        IF OBJECT_ID('users_target_mssql', 'U') IS NOT NULL
            DROP TABLE users_target_mssql
            
        CREATE TABLE users_target_mssql (
            id INT IDENTITY(1,1) PRIMARY KEY,
            username NVARCHAR(50) NOT NULL,
            email NVARCHAR(100) NOT NULL UNIQUE,
            age INT,
            status NVARCHAR(20) DEFAULT 'active',
            balance DECIMAL(10,2) DEFAULT 0.00,
            created_at DATETIME DEFAULT GETDATE()
        )
    """)
    
    conn.commit()
    print("SQL Server 目标库就绪")
    
    cursor.close()
    conn.close()

def setup_pg():
    """初始化 PostgreSQL 目标库"""
    print_section("步骤 3: 初始化 PostgreSQL 目标库")
    
    conn = get_pg_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        DROP TABLE IF EXISTS users_target_pg CASCADE;
        
        CREATE TABLE users_target_pg (
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

def sync_mysql_to_mssql():
    """同步 MySQL → SQL Server"""
    print_section("步骤 4: 同步 MySQL → SQL Server")
    
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    
    target_conn = get_mssql_conn()
    target_cursor = target_conn.cursor()
    
    # 读取源数据
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance, created_at 
        FROM users_source ORDER BY id
    """)
    rows = source_cursor.fetchall()
    print(f"从 MySQL 读取 {len(rows)} 条记录")
    
    # 写入 SQL Server
    insert_count = 0
    for row in rows:
        try:
            target_cursor.execute("""
                INSERT INTO users_target_mssql (username, email, age, status, balance, created_at)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (row[1], row[2], row[3], row[4], row[5], row[6]))
            insert_count += 1
        except Exception as e:
            print(f"插入失败 {row[0]}: {e}")
    
    target_conn.commit()
    print(f"向 SQL Server 写入 {insert_count} 条记录")
    
    # 验证
    target_cursor.execute("SELECT COUNT(*) FROM users_target_mssql")
    target_count = target_cursor.fetchone()[0]
    print(f"SQL Server 目标库记录数：{target_count}")
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    return insert_count, target_count

def sync_mysql_to_pg():
    """同步 MySQL → PostgreSQL"""
    print_section("步骤 5: 同步 MySQL → PostgreSQL")
    
    source_conn = get_mysql_conn()
    source_cursor = source_conn.cursor()
    
    target_conn = get_pg_conn()
    target_cursor = target_conn.cursor()
    
    # 读取源数据
    source_cursor.execute("""
        SELECT id, username, email, age, status, balance, created_at 
        FROM users_source ORDER BY id
    """)
    rows = source_cursor.fetchall()
    print(f"从 MySQL 读取 {len(rows)} 条记录")
    
    # 写入 PostgreSQL
    insert_count = 0
    for row in rows:
        try:
            target_cursor.execute("""
                INSERT INTO users_target_pg (username, email, age, status, balance, created_at)
                VALUES (%s, %s, %s, %s, %s, %s)
            """, (row[1], row[2], row[3], row[4], row[5], row[6]))
            insert_count += 1
        except Exception as e:
            print(f"插入失败 {row[0]}: {e}")
    
    target_conn.commit()
    print(f"向 PostgreSQL 写入 {insert_count} 条记录")
    
    # 验证
    target_cursor.execute("SELECT COUNT(*) FROM users_target_pg")
    target_count = target_cursor.fetchone()[0]
    print(f"PostgreSQL 目标库记录数：{target_count}")
    
    source_cursor.close()
    source_conn.close()
    target_cursor.close()
    target_conn.close()
    
    return insert_count, target_count

def verify_data():
    """验证数据一致性"""
    print_section("步骤 6: 验证数据一致性")
    
    results = {}
    
    # MySQL
    conn = get_mysql_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT id, username, email, balance FROM users_source ORDER BY id")
    results['MySQL'] = cursor.fetchall()
    cursor.close()
    conn.close()
    
    # SQL Server
    conn = get_mssql_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT id, username, email, balance FROM users_target_mssql ORDER BY id")
    results['SQL Server'] = cursor.fetchall()
    cursor.close()
    conn.close()
    
    # PostgreSQL
    conn = get_pg_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT id, username, email, balance FROM users_target_pg ORDER BY id")
    results['PostgreSQL'] = cursor.fetchall()
    cursor.close()
    conn.close()
    
    # 比较记录数
    mysql_count = len(results['MySQL'])
    mssql_count = len(results['SQL Server'])
    pg_count = len(results['PostgreSQL'])
    
    print(f"MySQL 记录数：      {mysql_count}")
    print(f"SQL Server 记录数： {mssql_count}")
    print(f"PostgreSQL 记录数： {pg_count}")
    
    # 数据比较
    print("\n数据对比:")
    print(f"{'ID':<4} {'Username':<10} {'Email':<25} {'Balance':<10} {'MySQL':<6} {'MSSQL':<6} {'PG':<6}")
    print("-" * 75)
    
    all_match = True
    for i, mysql_row in enumerate(results['MySQL']):
        mssql_row = results['SQL Server'][i] if i < len(results['SQL Server']) else None
        pg_row = results['PostgreSQL'][i] if i < len(results['PostgreSQL']) else None
        
        mysql_balance = float(mysql_row[3])
        mssql_balance = float(mssql_row[3]) if mssql_row else 0
        pg_balance = float(pg_row[3]) if pg_row else 0
        
        match = (mysql_row[1:3] == mssql_row[1:3] == pg_row[1:3]) if mssql_row and pg_row else False
        status = "✓" if match else "✗"
        
        if not match:
            all_match = False
        
        print(f"{mysql_row[0]:<4} {mysql_row[1]:<10} {mysql_row[2]:<25} {mysql_balance:<10.2f} {status:<6} {status:<6} {status:<6}")
    
    return all_match and mysql_count == mssql_count == pg_count

def test_update_sync():
    """测试增量同步（更新场景）"""
    print_section("步骤 7: 测试增量同步（更新源数据后重新同步）")
    
    # 更新 MySQL 源数据
    conn = get_mysql_conn()
    cursor = conn.cursor()
    
    cursor.execute("""
        UPDATE users_source SET status = 'vip', balance = 9999.99 WHERE email = 'alice@test.com'
    """)
    cursor.execute("""
        INSERT INTO users_source (username, email, age, status, balance) 
        VALUES ('new_user', 'new@test.com', 33, 'active', 500.00)
    """)
    
    conn.commit()
    cursor.execute("SELECT COUNT(*) FROM users_source")
    new_count = cursor.fetchone()[0]
    print(f"MySQL 源库更新后记录数：{new_count}")
    cursor.close()
    conn.close()
    
    # 清空并重新同步到 SQL Server
    conn = get_mssql_conn()
    cursor = conn.cursor()
    cursor.execute("TRUNCATE TABLE users_target_mssql")
    conn.commit()
    cursor.close()
    conn.close()
    
    print("重新同步到 SQL Server...")
    sync_mysql_to_mssql()
    
    # 验证
    conn = get_mssql_conn()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM users_target_mssql")
    mssql_count = cursor.fetchone()[0]
    cursor.close()
    conn.close()
    
    print(f"SQL Server 同步后记录数：{mssql_count}")
    return new_count == mssql_count

def main():
    start_time = datetime.now()
    print(f"\n测试开始时间：{start_time.strftime('%Y-%m-%d %H:%M:%S')}")
    
    try:
        # 检查数据库连接
        print_section("前置检查：数据库连接")
        try:
            get_mysql_conn().close()
            print("✓ MySQL 连接正常")
        except Exception as e:
            print(f"✗ MySQL 连接失败：{e}")
            return 1
            
        try:
            get_mssql_conn().close()
            print("✓ SQL Server 连接正常")
        except Exception as e:
            print(f"✗ SQL Server 连接失败：{e}")
            return 1
            
        try:
            get_pg_conn().close()
            print("✓ PostgreSQL 连接正常")
        except Exception as e:
            print(f"✗ PostgreSQL 连接失败：{e}")
            return 1
        
        # 执行测试
        source_count = setup_mysql()
        setup_mssql()
        setup_pg()
        
        mssql_insert, mssql_total = sync_mysql_to_mssql()
        pg_insert, pg_total = sync_mysql_to_pg()
        
        data_consistent = verify_data()
        
        update_success = test_update_sync()
        
        # 最终结果
        end_time = datetime.now()
        duration = (end_time - start_time).total_seconds()
        
        print_section("测试结果")
        print(f"总耗时：{duration:.2f} 秒")
        print(f"源数据库记录数：{source_count} → {source_count + 1} (更新测试)")
        print(f"SQL Server 同步：{'✓ 成功' if mssql_total >= source_count else '✗ 失败'} ({mssql_total} 条)")
        print(f"PostgreSQL 同步：{'✓ 成功' if pg_total >= source_count else '✗ 失败'} ({pg_total} 条)")
        print(f"数据一致性：{'✓ 一致' if data_consistent else '✗ 不一致'}")
        print(f"增量同步测试：{'✓ 通过' if update_success else '✗ 失败'}")
        
        if data_consistent and update_success:
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
