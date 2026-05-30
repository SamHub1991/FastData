#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FastData.Queue
{
    /// <summary>
    /// 链式写入构建器
    /// 支持 Fluent API：FastWrite.QueueBuilder().Add(user).Add(user2).Execute()
    /// 自动根据表配置决定是否使用消息队列
    /// </summary>
    public class FastWriteQueueBuilder
    {
        private readonly List<WriteOperation> _operations = new List<WriteOperation>();
        private readonly string _databaseKey;
        private WriteBehindConfig _overrideConfig;
        private Dictionary<string, object> _globalMetadata;
        private bool _enableSqlLog;

        internal FastWriteQueueBuilder(string databaseKey = null)
        {
            _databaseKey = databaseKey;
        }

        /// <summary>
        /// 启用当前写入操作的SQL日志（覆盖全局设置）
        /// </summary>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder EnableSqlLog()
        {
            _enableSqlLog = true;
            return this;
        }

        /// <summary>
        /// 设置全局扩展元数据（应用于所有操作）
        /// </summary>
        /// <param name="metadata">扩展元数据</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder WithMetadata(Dictionary<string, object> metadata)
        {
            _globalMetadata = metadata;
            return this;
        }

        /// <summary>
        /// 添加单个扩展元数据键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder AddMetadata(string key, object value)
        {
            if (_globalMetadata == null)
                _globalMetadata = new Dictionary<string, object>();
            _globalMetadata[key] = value;
            return this;
        }

        /// <summary>
        /// 添加实体（INSERT）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="metadata">操作级别的扩展元数据（可选，会与全局元数据合并）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Add<T>(T model, Dictionary<string, object> metadata = null) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 添加实体（INSERT）- 支持匿名类型
        /// </summary>
        /// <typeparam name="T">实体类型（可以是匿名类型）</typeparam>
        /// <param name="tableName">表名</param>
        /// <param name="model">实体对象</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Add<T>(string tableName, T model, Dictionary<string, object> metadata = null) where T : class
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                TableName = tableName,
                EntityType = typeof(T).FullName ?? "Anonymous",
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 批量添加实体（INSERT）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="models">实体列表</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder AddRange<T>(IEnumerable<T> models, Dictionary<string, object> metadata = null) where T : class, new()
        {
            foreach (var model in models)
            {
                Add(model, metadata);
            }
            return this;
        }

        /// <summary>
        /// 批量添加实体（INSERT）- 支持匿名类型
        /// </summary>
        /// <typeparam name="T">实体类型（可以是匿名类型）</typeparam>
        /// <param name="tableName">表名</param>
        /// <param name="models">实体列表</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder AddRange<T>(string tableName, IEnumerable<T> models, Dictionary<string, object> metadata = null) where T : class
        {
            foreach (var model in models)
            {
                Add(tableName, model, metadata);
            }
            return this;
        }

        /// <summary>
        /// 更新实体（UPDATE by PrimaryKey）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="field">需要更新的字段（可选）</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Update<T>(T model, Expression<Func<T, object>> field = null, Dictionary<string, object> metadata = null) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Update,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 更新实体（UPDATE）- 支持匿名类型
        /// </summary>
        /// <typeparam name="T">实体类型（可以是匿名类型）</typeparam>
        /// <param name="tableName">表名</param>
        /// <param name="model">实体对象</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Update<T>(string tableName, T model, Dictionary<string, object> metadata = null) where T : class
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Update,
                TableName = tableName,
                EntityType = typeof(T).FullName ?? "Anonymous",
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 删除实体（DELETE by PrimaryKey）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Delete<T>(T model, Dictionary<string, object> metadata = null) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Delete,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 删除实体（DELETE）- 支持匿名类型
        /// </summary>
        /// <typeparam name="T">实体类型（可以是匿名类型）</typeparam>
        /// <param name="tableName">表名</param>
        /// <param name="model">实体对象</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Delete<T>(string tableName, T model, Dictionary<string, object> metadata = null) where T : class
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Delete,
                TableName = tableName,
                EntityType = typeof(T).FullName ?? "Anonymous",
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 覆盖队列配置（可选，用于临时修改队列行为）
        /// </summary>
        /// <param name="config">队列配置</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder WithQueue(WriteBehindConfig config)
        {
            _overrideConfig = config;
            return this;
        }

        /// <summary>
        /// 执行所有操作（同步）
        /// 自动根据表配置决定：直接写数据库 或 写消息队列
        /// 如果数据库异常且启用了降级，自动切换到可信队列
        /// </summary>
        /// <returns>执行结果</returns>
        public WriteBehindResult Execute()
        {
            if (_operations.Count == 0)
            {
                return new WriteBehindResult { Success = true, Message = "无操作" };
            }

            var result = WriteBehindExecutor.Execute(_operations, _databaseKey, _overrideConfig);

            if (_enableSqlLog)
            {
                FastData.Core.Base.DbLog.LogSql(true, $"WriteBehindExecutor: {_operations.Count} ops, Success={result.Success}", "", 0);
            }

            return result;
        }

        /// <summary>
        /// 执行所有操作（异步）
        /// </summary>
        /// <returns>执行结果</returns>
        public Task<WriteBehindResult> ExecuteAsync()
        {
            return Task.Run(() => Execute());
        }

        /// <summary>
        /// 获取待执行的操作数量
        /// </summary>
        public int Count => _operations.Count;

        /// <summary>
        /// 清空操作列表
        /// </summary>
        public void Clear()
        {
            _operations.Clear();
        }

        /// <summary>
        /// 合并全局元数据和操作级别元数据
        /// </summary>
        private Dictionary<string, object> MergeMetadata(Dictionary<string, object> operationMetadata)
        {
            if (_globalMetadata == null && operationMetadata == null)
                return null;

            var merged = new Dictionary<string, object>();

            if (_globalMetadata != null)
            {
                foreach (var kvp in _globalMetadata)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            if (operationMetadata != null)
            {
                foreach (var kvp in operationMetadata)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return merged.Count > 0 ? merged : null;
        }
    }

    /// <summary>
    /// 写入后端执行结果
    /// </summary>
    public class WriteBehindResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 直接写入数据库的数量
        /// </summary>
        public int DirectWriteCount { get; set; }

        /// <summary>
        /// 写入队列的数量（降级）
        /// </summary>
        public int QueuedCount { get; set; }

        /// <summary>
        /// 失败的数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 是否发生了降级
        /// </summary>
        public bool FallbackOccurred { get; set; }

        /// <summary>
        /// 详细结果列表
        /// </summary>
        public List<WriteOperationResult> Details { get; set; } = new List<WriteOperationResult>();
    }

    /// <summary>
    /// 单个操作的结果
    /// </summary>
    public class WriteOperationResult
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public WriteOperationType OperationType { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 是否使用了队列
        /// </summary>
        public bool UsedQueue { get; set; }

        /// <summary>
        /// 错误消息（如果有）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 扩展元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }
}
#endif
