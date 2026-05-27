#!/usr/bin/env python3
"""
MySQL Data Sync Test - Simulates FastData.DataSyncService behavior
Syncs data from testdb.users -> testdb.users_copy using MySQL
"""

import mysql.connector
import json
from datetime import datetime

# MySQL connection config
MYSQL_CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

INTERMEDIATE_CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'fastdata_sync'
}

def get_connection(db_config):
    return mysql.connector.connect(**db_config)

def sync_users():
    """Sync users from source to target"""
    print("=== MySQL Data Sync Test ===")
    print()
    
    # Step 1: Read source data
    print("1. Reading source data from testdb.users...")
    source_conn = get_connection(MYSQL_CONFIG)
    source_cursor = source_conn.cursor(dictionary=True)
    source_cursor.execute("SELECT * FROM users")
    source_data = source_cursor.fetchall()
    source_cursor.close()
    source_conn.close()
    print(f"   Read {len(source_data)} rows from source")
    
    if not source_data:
        print("   No data to sync!")
        return
    
    # Step 2: Write to target
    print("2. Writing to target table testdb.users_copy...")
    target_conn = get_connection(MYSQL_CONFIG)
    target_cursor = target_conn.cursor()
    
    # Clear target first for clean test
    target_cursor.execute("TRUNCATE TABLE users_copy")
    
    # Insert each row
    columns = list(source_data[0].keys())
    placeholders = ','.join(['%s'] * len(columns))
    column_names = ','.join(columns)
    insert_sql = f"INSERT INTO users_copy ({column_names}) VALUES ({placeholders})"
    
    for row in source_data:
        values = []
        for col in columns:
            val = row[col]
            if isinstance(val, datetime):
                val = val.isoformat()
            values.append(val)
        target_cursor.execute(insert_sql, values)
    
    target_conn.commit()
    target_cursor.close()
    target_conn.close()
    print(f"   Inserted {len(source_data)} rows to target")
    
    # Step 3: Log to intermediate database
    print("3. Logging to intermediate database fastdata_sync...")
    intermediate_conn = get_connection(INTERMEDIATE_CONFIG)
    intermediate_cursor = intermediate_conn.cursor()
    
    # Insert sync log
    log_id = f"sync_{datetime.now().strftime('%Y%m%d%H%M%S')}"
    log_message = f"Sync completed: {len(source_data)} rows synced"
    intermediate_cursor.execute(
        "INSERT INTO fd_sync_log (log_id, task_id, log_level, message, created_time) VALUES (%s, %s, %s, %s, NOW())",
        (log_id, 'test_sync', 'INFO', log_message)
    )
    
    # Insert sync record (for failed record tracking)
    for row in source_data:
        record_id = f"record_{row['id']}"
        payload = json.dumps(row, default=str)
        intermediate_cursor.execute(
            "INSERT INTO fd_sync_record (record_id, batch_id, record_key, payload, status, retry_count) VALUES (%s, %s, %s, %s, %s, %s)",
            (record_id, log_id, str(row['id']), payload, 'Success', 0)
        )
    
    intermediate_conn.commit()
    intermediate_cursor.close()
    intermediate_conn.close()
    print(f"   Logged {len(source_data)} sync records")
    
    # Step 4: Verify
    print("4. Verifying sync results...")
    verify_conn = get_connection(MYSQL_CONFIG)
    verify_cursor = verify_conn.cursor()
    verify_cursor.execute("SELECT COUNT(*) FROM users_copy")
    target_count = verify_cursor.fetchone()[0]
    verify_cursor.close()
    verify_conn.close()
    
    print(f"   Target table now has {target_count} rows")
    
    # Final results
    print()
    print("=== Sync Results ===")
    print(f"Source rows: {len(source_data)}")
    print(f"Target rows: {target_count}")
    print(f"Status: {'SUCCESS' if target_count == len(source_data) else 'FAILED'}")
    
    return target_count == len(source_data)

if __name__ == "__main__":
    try:
        success = sync_users()
        exit(0 if success else 1)
    except Exception as e:
        print(f"ERROR: {e}")
        exit(1)
