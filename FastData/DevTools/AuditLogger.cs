using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace FastData.DevTools
{
    /// <summary>
    /// 审计日志记录器
    /// </summary>
    public interface IAuditLogger
    {
        void Log(AuditEntry entry);
        void LogAdd<T>(T entity, string dbKey = null) where T : class, new();
        void LogUpdate<T>(T oldEntity, T newEntity, string dbKey = null) where T : class, new();
        void LogDelete<T>(T entity, string dbKey = null) where T : class, new();
    }

    /// <summary>
    /// 控制台审计日志记录器
    /// </summary>
    public class ConsoleAuditLogger : IAuditLogger
    {
        public void Log(AuditEntry entry)
        {
            Console.WriteLine($"[{entry.Timestamp}] {entry.Action} - {entry.EntityType}");
            Console.WriteLine($"  操作人: {entry.User}");
            Console.WriteLine($"  主键: {entry.PrimaryKey}");
            if (!string.IsNullOrEmpty(entry.Changes))
            {
                Console.WriteLine($"  变更: {entry.Changes}");
            }
        }

        public void LogAdd<T>(T entity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "INSERT",
                EntityType = typeof(T).Name,
                Changes = GetEntityChanges(entity),
                DbKey = dbKey
            };
            Log(entry);
        }

        public void LogUpdate<T>(T oldEntity, T newEntity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "UPDATE",
                EntityType = typeof(T).Name,
                Changes = GetEntityDiff(oldEntity, newEntity),
                DbKey = dbKey
            };
            Log(entry);
        }

        public void LogDelete<T>(T entity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "DELETE",
                EntityType = typeof(T).Name,
                Changes = GetEntityChanges(entity),
                DbKey = dbKey
            };
            Log(entry);
        }

        private string GetEntityChanges<T>(T entity) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var changes = props.Select(p => $"{p.Name}={p.GetValue(entity)}");
            return string.Join(", ", changes);
        }

        private string GetEntityDiff<T>(T oldEntity, T newEntity) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var changes = new List<string>();

            foreach (var prop in props)
            {
                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add($"{prop.Name}: {oldValue} -> {newValue}");
                }
            }

            return changes.Any() ? string.Join(", ", changes) : "无变更";
        }
    }

    /// <summary>
    /// 数据库审计日志记录器
    /// </summary>
    public class DatabaseAuditLogger : IAuditLogger
    {
        private readonly string _dbKey;

        public DatabaseAuditLogger(string dbKey)
        {
            _dbKey = dbKey;
        }

        public void Log(AuditEntry entry)
        {
            try
            {
                using var db = new DataContext(_dbKey);
                db.cmd.CommandText = @"
                    INSERT INTO AuditLog (Timestamp, Action, EntityType, PrimaryKey, User, Changes, DbKey)
                    VALUES (@Timestamp, @Action, @EntityType, @PrimaryKey, @User, @Changes, @DbKey)";
                db.cmd.Parameters.Clear();
                db.cmd.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
                db.cmd.Parameters.AddWithValue("@Action", entry.Action);
                db.cmd.Parameters.AddWithValue("@EntityType", entry.EntityType);
                db.cmd.Parameters.AddWithValue("@PrimaryKey", entry.PrimaryKey ?? "");
                db.cmd.Parameters.AddWithValue("@User", entry.User ?? "System");
                db.cmd.Parameters.AddWithValue("@Changes", entry.Changes ?? "");
                db.cmd.Parameters.AddWithValue("@DbKey", entry.DbKey ?? "");
                db.cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"审计日志记录失败: {ex.Message}");
            }
        }

        public void LogAdd<T>(T entity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "INSERT",
                EntityType = typeof(T).Name,
                Changes = GetEntityChanges(entity),
                DbKey = dbKey
            };
            Log(entry);
        }

        public void LogUpdate<T>(T oldEntity, T newEntity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "UPDATE",
                EntityType = typeof(T).Name,
                Changes = GetEntityDiff(oldEntity, newEntity),
                DbKey = dbKey
            };
            Log(entry);
        }

        public void LogDelete<T>(T entity, string dbKey = null) where T : class, new()
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "DELETE",
                EntityType = typeof(T).Name,
                Changes = GetEntityChanges(entity),
                DbKey = dbKey
            };
            Log(entry);
        }

        private string GetEntityChanges<T>(T entity) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var changes = props.Select(p => $"{p.Name}={p.GetValue(entity)}");
            return string.Join(", ", changes);
        }

        private string GetEntityDiff<T>(T oldEntity, T newEntity) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var changes = new List<string>();

            foreach (var prop in props)
            {
                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add($"{prop.Name}: {oldValue} -> {newValue}");
                }
            }

            return changes.Any() ? string.Join(", ", changes) : "无变更";
        }
    }

    /// <summary>
    /// 审计日志条目
    /// </summary>
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string PrimaryKey { get; set; }
        public string User { get; set; }
        public string Changes { get; set; }
        public string DbKey { get; set; }
    }

    /// <summary>
    /// 审计装饰器
    /// </summary>
    public static class AuditDecorator
    {
        private static IAuditLogger _logger = new ConsoleAuditLogger();

        public static void SetLogger(IAuditLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static FastData.Model.Result AddWithAudit<T>(T entity, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Add<T>(entity, dbKey);
            if (result.IsSuccess)
            {
                _logger.LogAdd(entity, dbKey);
            }
            return result;
        }

        public static FastData.Model.Result UpdateWithAudit<T>(T oldEntity, T newEntity, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Update<T>(newEntity, dbKey);
            if (result.IsSuccess)
            {
                _logger.LogUpdate(oldEntity, newEntity, dbKey);
            }
            return result;
        }

        public static FastData.Model.Result DeleteWithAudit<T>(T entity, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.Delete<T>(entity, dbKey);
            if (result.IsSuccess)
            {
                _logger.LogDelete(entity, dbKey);
            }
            return result;
        }

        public static FastData.Model.Result AddRangeWithAudit<T>(IEnumerable<T> entities, string dbKey = null) where T : class, new()
        {
            var result = FastData.Write.Write.AddRange(entities, dbKey);
            if (result.IsSuccess)
            {
                foreach (var entity in entities)
                {
                    _logger.LogAdd(entity, dbKey);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 审计日志查询器
    /// </summary>
    public static class AuditLogQuery
    {
        public static List<AuditEntry> QueryLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string action = null,
            string entityType = null,
            string user = null,
            string dbKey = null)
        {
            var logs = new List<AuditEntry>();

            try
            {
                using var db = new DataContext(dbKey);
                var sql = "SELECT * FROM AuditLog WHERE 1=1";
                var conditions = new List<string>();

                if (startDate.HasValue)
                {
                    conditions.Add("Timestamp >= @StartDate");
                    db.cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                }

                if (endDate.HasValue)
                {
                    conditions.Add("Timestamp <= @EndDate");
                    db.cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    conditions.Add("Action = @Action");
                    db.cmd.Parameters.AddWithValue("@Action", action);
                }

                if (!string.IsNullOrEmpty(entityType))
                {
                    conditions.Add("EntityType = @EntityType");
                    db.cmd.Parameters.AddWithValue("@EntityType", entityType);
                }

                if (!string.IsNullOrEmpty(user))
                {
                    conditions.Add("User = @User");
                    db.cmd.Parameters.AddWithValue("@User", user);
                }

                if (conditions.Any())
                {
                    sql += " AND " + string.Join(" AND ", conditions);
                }

                sql += " ORDER BY Timestamp DESC";

                db.cmd.CommandText = sql;
                using var reader = db.cmd.ExecuteReader();

                while (reader.Read())
                {
                    logs.Add(new AuditEntry
                    {
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                        Action = reader.GetString(reader.GetOrdinal("Action")),
                        EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
                        PrimaryKey = reader.IsDBNull(reader.GetOrdinal("PrimaryKey")) ? null : reader.GetString(reader.GetOrdinal("PrimaryKey")),
                        User = reader.IsDBNull(reader.GetOrdinal("User")) ? null : reader.GetString(reader.GetOrdinal("User")),
                        Changes = reader.IsDBNull(reader.GetOrdinal("Changes")) ? null : reader.GetString(reader.GetOrdinal("Changes")),
                        DbKey = reader.IsDBNull(reader.GetOrdinal("DbKey")) ? null : reader.GetString(reader.GetOrdinal("DbKey"))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询审计日志失败: {ex.Message}");
            }

            return logs;
        }

        public static void InitializeAuditLogTable(string dbKey)
        {
            try
            {
                using var db = new DataContext(dbKey);
                db.cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AuditLog (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        Timestamp DATETIME NOT NULL,
                        Action VARCHAR(50) NOT NULL,
                        EntityType VARCHAR(100) NOT NULL,
                        PrimaryKey VARCHAR(200),
                        User VARCHAR(100),
                        Changes TEXT,
                        DbKey VARCHAR(50),
                        INDEX idx_timestamp (Timestamp),
                        INDEX idx_action (Action),
                        INDEX idx_entity (EntityType),
                        INDEX idx_user (User)
                    )";
                db.cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化审计日志表失败: {ex.Message}");
            }
        }
    }
}