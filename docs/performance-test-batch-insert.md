# 批量插入性能测试脚本

## 概述

本脚本用于测试 FastData 批量插入优化功能的性能提升效果。

## 测试场景

### 场景 1：MySQL 批量插入测试

```bash
#!/bin/bash
# MySQL 批量插入性能测试

echo "=== MySQL 批量插入性能测试 ==="
echo "测试数据量：10000 条"
echo "批量大小：500 行/批"
echo ""

# 测试配置
MYSQL_HOST="localhost"
MYSQL_PORT="3306"
MYSQL_DB="testdb"
MYSQL_USER="fastdata"
MYSQL_PASS="FastData@Test123"

# 创建测试表
mysql -h$MYSQL_HOST -P$MYSQL_PORT -u$MYSQL_USER -p$MYSQL_PASS $MYSQL_DB <<EOF
DROP TABLE IF EXISTS performance_test;
CREATE TABLE performance_test (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100),
    email VARCHAR(255),
    created_at DATETIME,
    updated_at DATETIME
);
EOF

echo "测试表已创建"
echo ""

# 使用 FastData 进行批量插入测试
# 注意：此脚本需要在 Windows 环境中配合 FastData 使用
echo "请在 Windows 环境中运行以下 C# 测试代码:"
```

```csharp
using FastData;
using FastData.Database;
using System;
using System.Data;
using System.Diagnostics;

namespace FastData.PerformanceTest
{
    public class BatchInsertPerformanceTest
    {
        private readonly string connectionString = "server=localhost;port=3306;database=testdb;uid=fastdata;pwd=FastData@Test123;";
        
        public void RunTest()
        {
            Console.WriteLine("=== 批量插入性能测试 ===");
            Console.WriteLine("数据量：10,000 条");
            Console.WriteLine("批量大小：500 行/批");
            Console.WriteLine();

            // 测试 1：逐行插入
            Console.WriteLine("【测试 1】逐行插入");
            var sw1 = Stopwatch.StartNew();
            InsertRowByRow(10000);
            sw1.Stop();
            Console.WriteLine($"耗时：{sw1.ElapsedMilliseconds}ms");
            Console.WriteLine($"每秒：{10000.0 / sw1.ElapsedMilliseconds * 1000:F0} 行/秒");
            Console.WriteLine();

            // 清空数据
            ClearTable();

            // 测试 2：批量插入
            Console.WriteLine("【测试 2】批量插入 (500 行/批)");
            var sw2 = Stopwatch.StartNew();
            InsertInBatches(10000, 500);
            sw2.Stop();
            sw2.Stop();
            Console.WriteLine($"耗时：{sw2.ElapsedMilliseconds}ms");
            Console.WriteLine($"每秒：{10000.0 / sw2.ElapsedMilliseconds * 1000:F0} 行/秒");
            Console.WriteLine();

            // 性能对比
            Console.WriteLine("=== 性能对比 ===");
            Console.WriteLine($"性能提升：{sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F2}x");
            Console.WriteLine($"时间节省：{(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds) / 1000.0:F2}秒");
        }

        private void InsertRowByRow(int count)
        {
            FastDb.Use("Default", () =>
            {
                for (int i = 0; i < count; i++)
                {
                    FastWrite.Add("performance_test", new
                    {
                        name = $"User_{i}",
                        email = $"user{i}@test.com",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }
            });
        }

        private void InsertInBatches(int totalCount, int batchSize)
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("email", typeof(string));
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("updated_at", typeof(DateTime));

            for (int i = 0; i < totalCount; i++)
            {
                var row = table.NewRow();
                row["name"] = $"User_{i}";
                row["email"] = $"user{i}@test.com";
                row["created_at"] = DateTime.Now;
                row["updated_at"] = DateTime.Now;
                table.Rows.Add(row);

                // 达到批量大小时执行插入
                if (table.Rows.Count >= batchSize)
                {
                    FastWrite.AddBatch("performance_test", table);
                    table.Rows.Clear();
                }
            }

            // 插入剩余数据
            if (table.Rows.Count > 0)
            {
                FastWrite.AddBatch("performance_test", table);
            }
        }

        private void ClearTable()
        {
            FastDb.Use("Default", () =>
            {
                FastWrite.Execute("TRUNCATE TABLE performance_test");
            });
        }
    }
}
```

### 场景 2：PostgreSQL 批量插入测试

```bash
#!/bin/bash
# PostgreSQL 批量插入性能测试

echo "=== PostgreSQL 批量插入性能测试 ==="

PGHOST="localhost"
PGPORT="5432"
PGDATABASE="testdb"
PGUSER="fastdata"
PGPASSWORD="FastData@Test123"

# 创建测试表
psql -h $PGHOST -p $PGPORT -U $PGUSER -d $PGDATABASE <<EOF
DROP TABLE IF EXISTS performance_test;
CREATE TABLE performance_test (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    email VARCHAR(255),
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
EOF

echo "测试表已创建"
```

### 场景 3：SQLite 批量插入测试

```bash
#!/bin/bash
# SQLite 批量插入性能测试

echo "=== SQLite 批量插入性能测试 ==="

DB_FILE="/tmp/fastdata_performance_test.db"

# 删除旧数据库
rm -f $DB_FILE

# 创建测试表
sqlite3 $DB_FILE <<EOF
CREATE TABLE performance_test (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT,
    email TEXT,
    created_at DATETIME,
    updated_at DATETIME
);
EOF

echo "测试表已创建，数据库文件：$DB_FILE"
```

## 测试步骤

### 1. 准备数据库环境

```bash
# MySQL
docker run -d --name mysql-perf \
  -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=testdb \
  -e MYSQL_USER=fastdata \
  -e MYSQL_PASSWORD=FastData@Test123 \
  mysql:8.0

# PostgreSQL
docker run -d --name pg-perf \
  -p 5432:5432 \
  -e POSTGRES_DB=testdb \
  -e POSTGRES_USER=fastdata \
  -e POSTGRES_PASSWORD=FastData@Test123 \
  postgres:15
```

### 2. 配置 FastData 连接

在 `app.config` 中添加：

```xml
<connectionStrings>
  <add name="Default" 
       connectionString="server=localhost;port=3306;database=testdb;uid=fastdata;pwd=FastData@Test123;" 
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

### 3. 运行测试程序

```csharp
class Program
{
    static void Main(string[] args)
    {
        var test = new BatchInsertPerformanceTest();
        test.RunTest();
        Console.ReadLine();
    }
}
```

## 预期结果

### 10,000 条记录性能对比（估算值）

| 插入方式 | 耗时 (MySQL) | 耗时 (PostgreSQL) | 耗时 (SQLite) |
|---------|--------------|-------------------|---------------|
| 逐行插入 | ~30-50 秒 | ~40-60 秒 | ~10-20 秒 |
| 批量插入 (500 行/批) | ~2-5 秒 | ~3-6 秒 | ~1-3 秒 |
| **性能提升** | **10-15x** | **10-12x** | **5-8x** |

### 100,000 条记录性能对比（估算值）

| 插入方式 | 耗时 (MySQL) | 耗时 (PostgreSQL) | 耗时 (SQLite) |
|---------|--------------|-------------------|---------------|
| 逐行插入 | ~5-8 分钟 | ~6-10 分钟 | ~1-3 分钟 |
| 批量插入 (500 行/批) | ~20-40 秒 | ~30-50 秒 | ~10-20 秒 |
| **性能提升** | **10-15x** | **12-15x** | **6-9x** |

## 优化技巧

### 1. 批量大小调优

- **小批量 (100-500 行)**：适合小数据量，内存占用低
- **中批量 (500-1000 行)**：推荐默认值，平衡性能和内存
- **大批量 (1000-5000 行)**：适合大数据量，需要更多内存

### 2. 索引优化

在批量插入前删除索引，插入后重建：

```sql
-- 删除索引
ALTER TABLE performance_test DROP INDEX idx_name;

-- 批量插入
-- ...

-- 重建索引
ALTER TABLE performance_test ADD INDEX idx_name (name);
```

### 3. 事务优化

使用事务包裹批量插入：

```csharp
using (var transaction = connection.BeginTransaction())
{
    // 批量插入
    transaction.Commit();
}
```

### 4. 并行插入

对于多表插入，可使用并行：

```csharp
Parallel.ForEach(tables, table =>
{
    FastWrite.AddBatch(table.Name, table.Data);
});
```

## 测试报告模板

```markdown
# 批量插入性能测试报告

## 测试环境

- **CPU**: [填写]
- **内存**: [填写]
- **数据库**: MySQL 8.0 / PostgreSQL 15 / SQLite 3.40
- **FastData 版本**: [填写]
- **测试日期**: 2026-05-26

## 测试结果

### 10,000 条记录

| 插入方式 | 耗时 | 行/秒 |
|---------|------|-------|
| 逐行插入 | XX ms | XX |
| 批量插入 | XX ms | XX |
| **提升** | **XX%** | **XX%** |

### 100,000 条记录

| 插入方式 | 耗时 | 行/秒 |
|---------|------|-------|
| 逐行插入 | XX ms | XX |
| 批量插入 | XX ms | XX |
| **提升** | **XX%** | **XX%** |

## 结论

[填写测试结论和优化建议]
```

## 相关文件

- `FastData.Tooling/Sync/DataRowSerializer.cs` - JSON 序列化工具
- `FastData.Tooling/Sync/DataSyncService.cs` - 数据同步服务（批量插入）
- `docs/docker-database-setup.md` - Docker 数据库环境配置

---

**创建时间**: 2026-05-26  
**适用范围**: FastData 批量插入性能测试
