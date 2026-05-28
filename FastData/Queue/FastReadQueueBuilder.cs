#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FastData.Queue
{
    /// <summary>
    /// 链式读取构建器
    /// 支持 Fluent API：FastRead.QueueBuilder<User>().Where(u => u.IsActive).Execute()
    /// 将查询请求推送到消息队列，实现异步查询或查询审计
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class FastReadQueueBuilder<T> where T : class, new()
    {
        private readonly List<ReadOperation> _operations = new List<ReadOperation>();
        private readonly string _databaseKey;
        private WriteBehindConfig _overrideConfig;
        private Dictionary<string, object> _globalMetadata;

        internal FastReadQueueBuilder(string databaseKey = null)
        {
            _databaseKey = databaseKey;
        }

        /// <summary>
        /// 设置全局扩展元数据（应用于所有操作）
        /// </summary>
        /// <param name="metadata">扩展元数据</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastReadQueueBuilder<T> WithMetadata(Dictionary<string, object> metadata)
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
        public FastReadQueueBuilder<T> AddMetadata(string key, object value)
        {
            if (_globalMetadata == null)
                _globalMetadata = new Dictionary<string, object>();
            _globalMetadata[key] = value;
            return this;
        }

        /// <summary>
        /// 添加查询单条请求
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastReadQueueBuilder<T> QuerySingle(Expression<Func<T, bool>> predicate, Dictionary<string, object> metadata = null)
        {
            var operation = new ReadOperation
            {
                OperationType = ReadOperationType.QuerySingle,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Predicate = JsonConvert.SerializeObject(predicate),
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 添加查询列表请求
        /// </summary>
        /// <param name="predicate">查询条件（可选）</param>
        /// <param name="orderBy">排序字段（可选）</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastReadQueueBuilder<T> QueryList(Expression<Func<T, bool>> predicate = null, Expression<Func<T, object>> orderBy = null, bool isAscending = true, Dictionary<string, object> metadata = null)
        {
            var operation = new ReadOperation
            {
                OperationType = ReadOperationType.QueryList,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Predicate = predicate != null ? JsonConvert.SerializeObject(predicate) : null,
                OrderBy = orderBy != null ? JsonConvert.SerializeObject(orderBy) : null,
                IsAscending = isAscending,
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 添加查询数量请求
        /// </summary>
        /// <param name="predicate">查询条件（可选）</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastReadQueueBuilder<T> QueryCount(Expression<Func<T, bool>> predicate = null, Dictionary<string, object> metadata = null)
        {
            var operation = new ReadOperation
            {
                OperationType = ReadOperationType.QueryCount,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Predicate = predicate != null ? JsonConvert.SerializeObject(predicate) : null,
                DatabaseKey = _databaseKey,
                Metadata = MergeMetadata(metadata)
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 添加分页查询请求
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="predicate">查询条件（可选）</param>
        /// <param name="orderBy">排序字段（可选）</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="metadata">操作级别的扩展元数据（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastReadQueueBuilder<T> QueryPaging(int pageIndex, int pageSize, Expression<Func<T, bool>> predicate = null, Expression<Func<T, object>> orderBy = null, bool isAscending = true, Dictionary<string, object> metadata = null)
        {
            var operation = new ReadOperation
            {
                OperationType = ReadOperationType.QueryPaging,
                TableName = Base.TableNameHelper.GetTableName<T>(),
                EntityType = typeof(T).FullName,
                Predicate = predicate != null ? JsonConvert.SerializeObject(predicate) : null,
                OrderBy = orderBy != null ? JsonConvert.SerializeObject(orderBy) : null,
                IsAscending = isAscending,
                PageIndex = pageIndex,
                PageSize = pageSize,
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
        public FastReadQueueBuilder<T> WithQueue(WriteBehindConfig config)
        {
            _overrideConfig = config;
            return this;
        }

        /// <summary>
        /// 执行所有操作（同步）
        /// 将查询请求推送到消息队列
        /// </summary>
        /// <returns>执行结果</returns>
        public ReadQueueResult Execute()
        {
            if (_operations.Count == 0)
            {
                return new ReadQueueResult { Success = true, Message = "无操作" };
            }

            return ReadQueueExecutor.Execute(_operations, _databaseKey, _overrideConfig);
        }

        /// <summary>
        /// 执行所有操作（异步）
        /// </summary>
        /// <returns>执行结果</returns>
        public Task<ReadQueueResult> ExecuteAsync()
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
    /// 读取队列执行结果
    /// </summary>
    public class ReadQueueResult
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
        /// 推送到队列的数量
        /// </summary>
        public int QueuedCount { get; set; }

        /// <summary>
        /// 失败的数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 详细结果列表
        /// </summary>
        public List<ReadOperationResult> Details { get; set; } = new List<ReadOperationResult>();
    }

    /// <summary>
    /// 单个读取操作的结果
    /// </summary>
    public class ReadOperationResult
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public ReadOperationType OperationType { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

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
