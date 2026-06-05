using System;
using System.Collections.Generic;
using System.Linq;

namespace FastData.Migrations
{
    /// <summary>
    /// 数据库迁移管理器
    /// </summary>
    public class MigrationManager
    {
        private readonly string _connectionString;
        private readonly List<IMigration> _migrations = new List<IMigration>();

        public MigrationManager(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// 添加迁移
        /// </summary>
        public void AddMigration(IMigration migration)
        {
            if (migration == null)
                throw new ArgumentNullException(nameof(migration));

            _migrations.Add(migration);
        }

        /// <summary>
        /// 执行所有未应用的迁移
        /// </summary>
        public void Migrate()
        {
            var appliedMigrations = GetAppliedMigrations();
            var pendingMigrations = _migrations
                .Where(m => !appliedMigrations.Contains(m.Version))
                .OrderBy(m => m.Version)
                .ToList();

            foreach (var migration in pendingMigrations)
            {
                ExecuteMigration(migration);
                RecordMigration(migration);
            }
        }

        /// <summary>
        /// 回滚到指定版本
        /// </summary>
        public void Rollback(string targetVersion)
        {
            var appliedMigrations = GetAppliedMigrations();
            var migrationsToRollback = _migrations
                .Where(m => appliedMigrations.Contains(m.Version) && 
                           string.Compare(m.Version, targetVersion, StringComparison.Ordinal) > 0)
                .OrderByDescending(m => m.Version)
                .ToList();

            foreach (var migration in migrationsToRollback)
            {
                migration.Down();
                RemoveMigrationRecord(migration.Version);
            }
        }

        /// <summary>
        /// 获取迁移历史
        /// </summary>
        public List<MigrationInfo> GetMigrationHistory()
        {
            return _migrations
                .OrderBy(m => m.Version)
                .Select(m => new MigrationInfo
                {
                    Version = m.Version,
                    Description = m.Description,
                    AppliedAt = GetMigrationAppliedDate(m.Version)
                })
                .ToList();
        }

        private void ExecuteMigration(IMigration migration)
        {
            try
            {
                migration.Up();
            }
            catch (Exception ex)
            {
                throw new MigrationException(string.Format("执行迁移 {0} 失败: {1}", migration.Version, ex.Message), ex);
            }
        }

        private List<string> GetAppliedMigrations()
        {
            // 实现从数据库读取已应用的迁移版本
            return new List<string>();
        }

        private void RecordMigration(IMigration migration)
        {
            // 实现将迁移记录写入数据库
        }

        private void RemoveMigrationRecord(string version)
        {
            // 实现从数据库删除迁移记录
        }

        private DateTime? GetMigrationAppliedDate(string version)
        {
            // 实现从数据库读取迁移应用时间
            return null;
        }
    }

    /// <summary>
    /// 迁移接口
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// 迁移版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 迁移描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 执行迁移（Up）
        /// </summary>
        void Up();

        /// <summary>
        /// 回滚迁移（Down）
        /// </summary>
        void Down();
    }

    /// <summary>
    /// 迁移基类
    /// </summary>
    public abstract class Migration : IMigration
    {
        public abstract string Version { get; }
        public abstract string Description { get; }

        public abstract void Up();
        public abstract void Down();
    }

    /// <summary>
    /// 迁移信息
    /// </summary>
    public class MigrationInfo
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime? AppliedAt { get; set; }
    }

    /// <summary>
    /// 迁移异常
    /// </summary>
    public class MigrationException : Exception
    {
        public string MigrationVersion { get; }

        public MigrationException(string message) : base(message)
        {
        }

        public MigrationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public MigrationException(string migrationVersion, string message) : base(message)
        {
            MigrationVersion = migrationVersion;
        }

        public MigrationException(string migrationVersion, string message, Exception innerException) 
            : base(message, innerException)
        {
            MigrationVersion = migrationVersion;
        }
    }

    /// <summary>
    /// SQL 迁移构建器 - 帮助创建 SQL 迁移
    /// </summary>
    public class SqlMigrationBuilder
    {
        private readonly List<string> _sqlStatements = new List<string>();

        /// <summary>
        /// 执行 SQL
        /// </summary>
        public void Execute(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL 语句不能为空", nameof(sql));

            _sqlStatements.Add(sql);
        }

        /// <summary>
        /// 创建表
        /// </summary>
        public void CreateTable(string tableName, Action<TableBuilder> configure)
        {
            var builder = new TableBuilder(tableName);
            configure(builder);
            _sqlStatements.Add(builder.ToSql());
        }

        /// <summary>
        /// 删除表
        /// </summary>
        public void DropTable(string tableName)
        {
            _sqlStatements.Add(string.Format("DROP TABLE IF EXISTS {0};", tableName));
        }

        /// <summary>
        /// 添加列
        /// </summary>
        public void AddColumn(string tableName, string columnName, string columnType, bool nullable = true)
        {
            var nullableSql = nullable ? "" : " NOT NULL";
            _sqlStatements.Add(string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3};", tableName, columnName, columnType, nullableSql));
        }

        /// <summary>
        /// 删除列
        /// </summary>
        public void DropColumn(string tableName, string columnName)
        {
            _sqlStatements.Add(string.Format("ALTER TABLE {0} DROP COLUMN {1};", tableName, columnName));
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        public void CreateIndex(string indexName, string tableName, string columnName)
        {
            _sqlStatements.Add(string.Format("CREATE INDEX {0} ON {1}({2});", indexName, tableName, columnName));
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        public void DropIndex(string indexName)
        {
            _sqlStatements.Add(string.Format("DROP INDEX IF EXISTS {0};", indexName));
        }

        public List<string> GetSqlStatements()
        {
            return new List<string>(_sqlStatements);
        }
    }

    /// <summary>
    /// 表构建器
    /// </summary>
    public class TableBuilder
    {
        private readonly string _tableName;
        private readonly List<ColumnDefinition> _columns = new List<ColumnDefinition>();
        private string _primaryKey;

        public TableBuilder(string tableName)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        public TableBuilder Column(string name, string type, bool nullable = true, string defaultValue = null)
        {
            _columns.Add(new ColumnDefinition
            {
                Name = name,
                Type = type,
                Nullable = nullable,
                DefaultValue = defaultValue
            });
            return this;
        }

        public TableBuilder PrimaryKey(string columnName)
        {
            _primaryKey = columnName;
            return this;
        }

        public string ToSql()
        {
            var columnsSql = string.Join(",\n    ", _columns.Select(c =>
            {
                var sql = string.Format("{0} {1}", c.Name, c.Type);
                if (!c.Nullable)
                    sql += " NOT NULL";
                if (!string.IsNullOrEmpty(c.DefaultValue))
                    sql += string.Format(" DEFAULT {0}", c.DefaultValue);
                return sql;
            }));

            var sql = string.Format("CREATE TABLE {0} (\n    {1}", _tableName, columnsSql);
            if (!string.IsNullOrEmpty(_primaryKey))
                sql += string.Format(",\n    PRIMARY KEY ({0})", _primaryKey);
            sql += "\n);";

            return sql;
        }

        private class ColumnDefinition
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool Nullable { get; set; }
            public string DefaultValue { get; set; }
        }
    }
}