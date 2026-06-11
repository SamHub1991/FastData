using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastData.Property;

namespace FastData.ChangeTracking
{
    /// <summary>
    /// 变更跟踪器 - 跟踪实体的属性变更
    /// </summary>
    public class ChangeTracker
    {
        private readonly Dictionary<object, EntitySnapshot> _snapshots = new Dictionary<object, EntitySnapshot>();

        /// <summary>
        /// 开始跟踪实体变更
        /// </summary>
        public void Track<T>(T entity) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var snapshot = CreateSnapshot(entity);
            _snapshots[entity] = snapshot;
        }

        /// <summary>
        /// 停止跟踪实体变更
        /// </summary>
        public void Untrack<T>(T entity) where T : class
        {
            if (entity != null)
            {
                _snapshots.Remove(entity);
            }
        }

        /// <summary>
        /// 获取实体的变更列表
        /// </summary>
        public List<PropertyChange> GetChanges<T>(T entity) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (!_snapshots.TryGetValue(entity, out var snapshot))
                return new List<PropertyChange>();

            var currentSnapshot = CreateSnapshot(entity);
            var changes = new List<PropertyChange>();

            foreach (var property in snapshot.Properties)
            {
                currentSnapshot.Properties.TryGetValue(property.Key, out var currentValue);
                var originalValue = property.Value;

                if (!Equals(originalValue, currentValue))
                {
                    changes.Add(new PropertyChange
                    {
                        PropertyName = property.Key,
                        OriginalValue = originalValue,
                        CurrentValue = currentValue,
                        ChangeType = GetChangeType(originalValue, currentValue)
                    });
                }
            }

            return changes;
        }

        /// <summary>
        /// 检查实体是否有变更
        /// </summary>
        public bool HasChanges<T>(T entity) where T : class
        {
            return GetChanges(entity).Count > 0;
        }

        /// <summary>
        /// 获取所有被跟踪的实体
        /// </summary>
        public IEnumerable<TrackedEntity> GetTrackedEntities()
        {
            return _snapshots.Select(kvp => new TrackedEntity
            {
                Entity = kvp.Value.Entity,
                EntityType = kvp.Value.Entity.GetType(),
                Changes = GetChanges(kvp.Value.Entity)
            });
        }

        /// <summary>
        /// 清除所有跟踪
        /// </summary>
        public void Clear()
        {
            _snapshots.Clear();
        }

        private EntitySnapshot CreateSnapshot<T>(T entity) where T : class
        {
            var properties = PropertyCache.GetPropertiesCached<T>()
                .Where(p => p.CanRead && p.CanWrite)
                .ToDictionary(p => p.Name, p => p.GetValue(entity));

            return new EntitySnapshot
            {
                Entity = entity,
                Properties = properties,
                SnapshotTime = DateTime.UtcNow
            };
        }

        private ChangeType GetChangeType(object originalValue, object currentValue)
        {
            if (originalValue == null && currentValue != null)
                return ChangeType.Added;
            if (originalValue != null && currentValue == null)
                return ChangeType.Removed;
            return ChangeType.Modified;
        }

        /// <summary>
        /// 更新快照（将当前状态标记为原始状态）
        /// </summary>
        public void UpdateSnapshot<T>(T entity) where T : class
        {
            if (entity != null)
            {
                var snapshot = CreateSnapshot(entity);
                _snapshots[entity] = snapshot;
            }
        }

        /// <summary>
        /// 获取实体变更的 SQL 更新语句
        /// </summary>
        public string GetUpdateSql<T>(T entity, string tableName = null) where T : class
        {
            var changes = GetChanges(entity);
            if (changes.Count == 0)
                return null;

            tableName = tableName ?? TableNameHelper.GetTableName(entity.GetType());

            var setClauses = changes
                .Where(c => c.PropertyName != "Id")
                .Select(c => string.Format("{0} = @{0}", c.PropertyName))
                .ToList();

            if (setClauses.Count == 0)
                return null;

            var sql = string.Format("UPDATE {0} SET {1} WHERE Id = @Id", tableName, string.Join(", ", setClauses));
            return sql;
        }
    }

    /// <summary>
    /// 实体快照
    /// </summary>
    internal class EntitySnapshot
    {
        public object Entity { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DateTime SnapshotTime { get; set; }
    }

    /// <summary>
    /// 属性变更信息
    /// </summary>
    public class PropertyChange
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 原始值
        /// </summary>
        public object OriginalValue { get; set; }

        /// <summary>
        /// 当前值
        /// </summary>
        public object CurrentValue { get; set; }

        /// <summary>
        /// 变更类型
        /// </summary>
        public ChangeType ChangeType { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1} → {2}", PropertyName, OriginalValue, CurrentValue);
        }
    }

    /// <summary>
    /// 变更类型
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// 已添加
        /// </summary>
        Added,

        /// <summary>
        /// 已修改
        /// </summary>
        Modified,

        /// <summary>
        /// 已移除
        /// </summary>
        Removed
    }

    /// <summary>
    /// 被跟踪的实体
    /// </summary>
    public class TrackedEntity
    {
        /// <summary>
        /// 实体实例
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// 变更列表
        /// </summary>
        public List<PropertyChange> Changes { get; set; }

        /// <summary>
        /// 是否有变更
        /// </summary>
        public bool HasChanges => Changes != null && Changes.Count > 0;
    }
}