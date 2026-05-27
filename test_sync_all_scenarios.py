#!/usr/bin/env python3
"""
MySQL Data Sync Comprehensive Test - 完整同步场景测试
覆盖场景：
  1. 无主键表业务主键去重 (email as business key)
  2. 首次全量同步
  3. 时间范围增量同步
  4. 手动指定表和范围
  5. 布尔字段过滤
"""

import mysql.connector
from datetime import datetime, timedelta

CONFIG = {
    'host': 'localhost',
    'port': 3306,
    'user': 'root',
    'password': 'FastData@Test123',
    'database': 'testdb'
}

pass_cnt = 0
fail_cnt = 0

def check(desc, condition):
    global pass_cnt, fail_cnt
    if condition:
        print(f"  [PASS] {desc}")
        pass_cnt += 1
    else:
        print(f"  [FAIL] {desc}")
        fail_cnt += 1

def get_conn():
    return mysql.connector.connect(**CONFIG)

# =============================================================
# 场景1: 无主键表 + 业务主键(email)去重
# =============================================================
print("="*60)
print("场景1: 无主键表 + 业务主键(email)去重")
print("="*60)

conn = get_conn()
cur = conn.cursor()

# 清空目标表，添加 email 唯一约束（业务主键）
conn = get_conn()
cur = conn.cursor()
cur.execute("DROP TABLE IF EXISTS sync_no_pk_target")
cur.execute("""
    CREATE TABLE sync_no_pk_target (
        id INT NOT NULL,
        name VARCHAR(100),
        email VARCHAR(100),
        amount DECIMAL(10,2),
        create_time DATETIME,
        UNIQUE INDEX uq_email (email)
    )
""")
conn.commit()

# 1a. 首次全量同步（模拟 DataSyncService: AlwaysDeduplicate=true, PrimaryKeyColumns='email'）
print("\n1a. 首次全量同步:")
cur.execute("SELECT * FROM sync_no_pk")
src_data = cur.fetchall()
cols = [d[0] for d in cur.description]
email_idx = cols.index('email')

# 用 INSERT IGNORE 模拟 AlwaysDeduplicate=true
insert_ignore_sql = f"INSERT IGNORE INTO sync_no_pk_target ({','.join(cols)}) VALUES ({','.join(['%s']*len(cols))})"
for row in src_data:
    cur.execute(insert_ignore_sql, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_no_pk_target")
target_cnt = cur.fetchone()[0]
print(f"  源表: {len(src_data)}, 目标表: {target_cnt}")
check("首次全量同步行数一致", target_cnt == len(src_data))

# 1b. 再次同步（相同数据，email 唯一约束阻止重复）
print("\n1b. 重复同步（相同数据，AlwaysDeduplicate=true）:")
cur.execute("SELECT * FROM sync_no_pk")
src_data2 = cur.fetchall()
for row in src_data2:
    cur.execute(insert_ignore_sql, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_no_pk_target")
target_cnt2 = cur.fetchone()[0]
check(f"重复同步后无重复数据 (INSERT IGNORE)", target_cnt2 == len(src_data))

# 1c. 程序级去重（模拟 DataSyncService.CheckRowExists）
print("\n1c. 程序级去重（CheckRowExists + INSERT）:")
# 新增数据中有已存在的 email
test_rows = [
    (1, '张三-更新', 'zhangsan@test.com', 150.00, '2026-05-27 10:00:00'),  # 存在，跳过
    (6, '钱九', 'qianjiu@test.com', 600.00, '2026-05-27 11:00:00'),       # 新增
    (2, '李四-更新', 'lisi@test.com', 250.00, '2026-05-27 10:00:00'),     # 存在，跳过
]
inserted = 0
skipped = 0
for row in test_rows:
    email_val = row[email_idx]
    cur.execute("SELECT COUNT(*) FROM sync_no_pk_target WHERE email = %s", (email_val,))
    exists = cur.fetchone()[0] > 0
    if exists:
        skipped += 1
    else:
        cur.execute(insert_ignore_sql, row)
        inserted += 1
conn.commit()
check(f"程序级去重: 跳过 {skipped}, 插入 {inserted} (预期 skip=2, insert=1)", skipped == 2 and inserted == 1)
cur.execute("SELECT COUNT(*) FROM sync_no_pk_target")
final_cnt = cur.fetchone()[0]
check(f"目标表总数 {final_cnt} (5+1=6)", final_cnt == 6)

cur.close()
conn.close()

# =============================================================
# 场景2: 首次全量 + 后续时间范围增量
# =============================================================
print("\n" + "="*60)
print("场景2: 首次全量 + 时间范围增量同步")
print("="*60)

conn = get_conn()
cur = conn.cursor()

cur.execute("DROP TABLE IF EXISTS sync_time_target")
cur.execute("CREATE TABLE sync_time_target LIKE sync_time_range")
cur.execute("TRUNCATE TABLE sync_time_target")

# 2a. 首次全量同步
print("\n2a. 首次全量同步 (IsFullSyncForFirstTime=true):")
cur.execute("SELECT * FROM sync_time_range")
all_data = cur.fetchall()
cols2 = [d[0] for d in cur.description]
ins_sql2 = f"INSERT IGNORE INTO sync_time_target ({','.join(cols2)}) VALUES ({','.join(['%s']*len(cols2))})"
for row in all_data:
    cur.execute(ins_sql2, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_time_target")
check("首次全量同步 6 条", cur.fetchone()[0] == 6)

# 2b. 时间范围增量同步（模拟后续同步，RangeDays=2）
print("\n2b. 增量同步 (RangeDays=2, 同步最近2天):")
two_days_ago = (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d 00:00:00')
cur.execute(f"SELECT * FROM sync_time_range WHERE create_time >= '{two_days_ago}'")
inc_data = cur.fetchall()
print(f"  时间过滤: create_time >= {two_days_ago}, 命中 {len(inc_data)} 条")

# AlwaysDeduplicate: 使用 INSERT IGNORE 或 CheckRowExists
for row in inc_data:
    cur.execute("SELECT COUNT(*) FROM sync_time_target WHERE id = %s", (row[0],))
    if cur.fetchone()[0] == 0:
        cur.execute(ins_sql2, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_time_target")
check(f"时间增量无重复", cur.fetchone()[0] == 6)

# 2c. 不同天数范围测试
print("\n2c. 不同范围天数:")
for days in [7, 3, 1]:
    since = (datetime.now() - timedelta(days=days)).strftime('%Y-%m-%d 00:00:00')
    cur.execute(f"SELECT COUNT(*) FROM sync_time_range WHERE create_time >= '{since}'")
    cnt = cur.fetchone()[0]
    print(f"  RangeDays={days}: {cnt} 条 (create_time >= {since[:10]})")

cur.close()
conn.close()

# =============================================================
# 场景3: 手动指定表和时间范围
# =============================================================
print("\n" + "="*60)
print("场景3: 手动指定表和时间范围")
print("="*60)

conn = get_conn()
cur = conn.cursor()

# 3a. 指定表和自定义时间范围
print("\n3a. 指定时间范围 2026-05-24~2026-05-26:")
cur.execute("""
    SELECT COUNT(*) FROM sync_time_range 
    WHERE create_time >= '2026-05-24 00:00:00' 
    AND create_time <= '2026-05-26 23:59:59'
""")
check("自定义范围命中 2 条（王五+赵六）", cur.fetchone()[0] == 2)

# 3b. 指定源表字段是时间的用时间段过滤（手动指定表+字段）
print("\n3b. 手动指定 update_time 过滤:")
cur.execute("""
    SELECT COUNT(*) FROM sync_time_range 
    WHERE update_time >= '2026-05-26 00:00:00'
""")
cnt = cur.fetchone()[0]
print(f"  update_time >= 2026-05-26: {cnt} 条")

cur.close()
conn.close()

# =============================================================
# 场景4: 布尔字段过滤
# =============================================================
print("\n" + "="*60)
print("场景4: 布尔字段过滤同步")
print("="*60)

conn = get_conn()
cur = conn.cursor()

cur.execute("DROP TABLE IF EXISTS sync_bool_target")
cur.execute("CREATE TABLE sync_bool_target LIKE sync_bool_filter")

# 4a. 只同步 is_active=1 的记录
print("\n4a. 只同步活跃记录 (is_active=1):")
cur.execute("SELECT * FROM sync_bool_filter WHERE is_active = 1")
active_data = cur.fetchall()
cols3 = [d[0] for d in cur.description]
ins_sql3 = f"INSERT IGNORE INTO sync_bool_target ({','.join(cols3)}) VALUES ({','.join(['%s']*len(cols3))})"
for row in active_data:
    cur.execute(ins_sql3, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_bool_target")
cnt_active = cur.fetchone()[0]
check(f"活跃记录 4 条（排除王五+孙七）", cnt_active == 4)

# 4b. 只同步 is_deleted=0 的记录
print("\n4b. 只同步未删除记录 (is_deleted=0):")
cur.execute("TRUNCATE TABLE sync_bool_target")
cur.execute("SELECT * FROM sync_bool_filter WHERE is_deleted = 0")
live_data = cur.fetchall()
for row in live_data:
    cur.execute(ins_sql3, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_bool_target")
check(f"未删除 4 条（排除赵六+孙七）", cur.fetchone()[0] == 4)

# 4c. 组合过滤: is_active=1 AND is_deleted=0
print("\n4c. 组合过滤 (is_active=1 AND is_deleted=0):")
cur.execute("TRUNCATE TABLE sync_bool_target")
cur.execute("SELECT * FROM sync_bool_filter WHERE is_active = 1 AND is_deleted = 0")
combined = cur.fetchall()
for row in combined:
    cur.execute(ins_sql3, row)
conn.commit()
cur.execute("SELECT COUNT(*) FROM sync_bool_target")
check(f"活跃+未删除 3 条", cur.fetchone()[0] == 3)

cur.close()
conn.close()

# =============================================================
# 场景5: 手动指定表 + 任意过滤条件
# =============================================================
print("\n" + "="*60)
print("场景5: 手动指定表 + 自定义过滤条件")
print("="*60)

conn = get_conn()
cur = conn.cursor()

# 5a. 同步指定状态
print("\n5a. 按状态过滤 (status='active'):")
cur.execute("SELECT COUNT(*) FROM sync_time_range WHERE status = 'active'")
check("active status 4 条", cur.fetchone()[0] == 4)

# 5b. 同步指定金额范围
print("\n5b. 按金额范围过滤 (amount >= 300):")
cur.execute("SELECT COUNT(*) FROM sync_no_pk WHERE amount >= 300")
check("amount >= 300 共 3 条", cur.fetchone()[0] == 3)

# 5c. 时间+状态组合过滤
print("\n5c. 时间+状态组合过滤:")
cur.execute("""
    SELECT COUNT(*) FROM sync_time_range 
    WHERE create_time >= '2026-05-26 00:00:00' 
    AND status = 'active'
""")
check(">=5.26 + active 2 条", cur.fetchone()[0] == 2)

cur.close()
conn.close()

# =============================================================
# 总结
# =============================================================
print("\n" + "="*60)
print("=== 测试总结 ===")
print(f"通过: {pass_cnt}, 失败: {fail_cnt}, 总计: {pass_cnt + fail_cnt}")
if fail_cnt == 0:
    print("全部测试通过!")
else:
    print(f"{fail_cnt} 个测试失败!")
print("="*60)