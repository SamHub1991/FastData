#!/usr/bin/env python3
"""
MySQL Data Sync Test - Duplicate Scenario Testing
Tests: 1. Sync twice without dedup -> duplicates
      2. Sync with dedup -> no duplicates
"""

import mysql.connector
import json
from datetime import datetime

MYSQL_CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

def get_connection():
    return mysql.connector.connect(**MYSQL_CONFIG)

def test_without_dedup():
    """测试没有去重的情况"""
    print("=== 场景1: 没有去重逻辑 (直接 INSERT) ===")
    
    conn = get_connection()
    cursor = conn.cursor()
    
    # 第一次同步
    cursor.execute("SELECT * FROM users")
    data = cursor.fetchall()
    columns = [desc[0] for desc in cursor.description]
    
    for row in data:
        placeholders = ','.join(['%s'] * len(columns))
        sql = f"INSERT INTO users_copy ({','.join(columns)}) VALUES ({placeholders})"
        cursor.execute(sql, row)
    
    conn.commit()
    print(f"第一次同步: 插入 {len(data)} 条")
    
    # 第二次同步 (不清空，直接再插入)
    for row in data:
        placeholders = ','.join(['%s'] * len(columns))
        sql = f"INSERT INTO users_copy ({','.join(columns)}) VALUES ({placeholders})"
        try:
            cursor.execute(sql, row)
        except:
            pass  # 忽略重复键错误
    conn.commit()
    print(f"第二次同步: 尝试插入 {len(data)} 条")
    
    # 检查结果
    cursor.execute("SELECT COUNT(*) FROM users_copy")
    total = cursor.fetchone()[0]
    print(f"目标表总数: {total}")
    print(f"结果: {'有重复' if total > len(data) else '无重复'}")
    
    cursor.close()
    conn.close()
    return total

def test_with_dedup():
    """测试有去重的情况 (使用 INSERT IGNORE 或 ON DUPLICATE KEY)"""
    print("\n=== 场景2: 有去重逻辑 (INSERT IGNORE) ===")
    
    conn = get_connection()
    cursor = conn.cursor()
    
    # 清空目标表
    cursor.execute("TRUNCATE TABLE users_copy")
    conn.commit()
    
    # 第一次同步
    cursor.execute("SELECT * FROM users")
    data = cursor.fetchall()
    columns = [desc[0] for desc in cursor.description]
    
    for row in data:
        placeholders = ','.join(['%s'] * len(columns))
        sql = f"INSERT IGNORE INTO users_copy ({','.join(columns)}) VALUES ({placeholders})"
        cursor.execute(sql, row)
    
    conn.commit()
    print(f"第一次同步: 插入 {len(data)} 条")
    
    # 第二次同步 (使用 INSERT IGNORE 跳过已存在)
    for row in data:
        placeholders = ','.join(['%s'] * len(columns))
        sql = f"INSERT IGNORE INTO users_copy ({','.join(columns)}) VALUES ({placeholders})"
        cursor.execute(sql, row)
    conn.commit()
    print(f"第二次同步: 尝试插入 {len(data)} 条")
    
    # 检查结果
    cursor.execute("SELECT COUNT(*) FROM users_copy")
    total = cursor.fetchone()[0]
    print(f"目标表总数: {total}")
    print(f"结果: {'有重复' if total > len(data) else '无重复 (去重成功)'}")
    
    cursor.close()
    conn.close()
    return total

if __name__ == "__main__":
    # 清理环境
    conn = get_connection()
    conn.cursor().execute("TRUNCATE TABLE users_copy")
    conn.commit()
    conn.close()
    
    # 测试场景1
    test_without_dedup()
    
    # 清理环境
    conn = get_connection()
    conn.cursor().execute("TRUNCATE TABLE users_copy")
    conn.commit()
    conn.close()
    
    # 测试场景2
    test_with_dedup()
    
    print("\n=== 结论 ===")
    print("DataSyncService.AlwaysDeduplicate=true 时使用 UPSERT 或 INSERT IGNORE")
    print("可以避免重复数据")
