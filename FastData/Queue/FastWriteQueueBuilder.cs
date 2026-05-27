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
    /// 支持 Fluent API：FastWrite.Queue().Add(user).Add(user2).Execute()
    /// 自动根据表配置决定是否使用消息队列
    /// </summary>
    public class FastWriteQueueBuilder
    {
        private readonly List<WriteOperation> _operations = new List<WriteOperation>();
        private readonly string _databaseKey;
        private WriteBehindConfig _overrideConfig;

        internal FastWriteQueueBuilder(string databaseKey = null)
        {
            _databaseKey = databaseKey;
        }

        /// <summary>
        /// 添加实体（INSERT）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Add<T>(T model) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                TableName = typeof(T).Name,
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 批量添加实体（INSERT）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="models">实体列表</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder AddRange<T>(IEnumerable<T> models) where T : class, new()
        {
            foreach (var model in models)
            {
                Add(model);
            }
            return this;
        }

        /// <summary>
        /// 更新实体（UPDATE by PrimaryKey）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="field">需要更新的字段（可选）</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Update<T>(T model, Expression<Func<T, object>> field = null) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Update,
                TableName = typeof(T).Name,
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey
            };
            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 删除实体（DELETE by PrimaryKey）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <returns>构建器（支持链式调用）</returns>
        public FastWriteQueueBuilder Delete<T>(T model) where T : class, new()
        {
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Delete,
                TableName = typeof(T).Name,
                EntityType = typeof(T).FullName,
                Data = JsonConvert.SerializeObject(model),
                DatabaseKey = _databaseKey
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

            return WriteBehindExecutor.Execute(_operations, _databaseKey, _overrideConfig);
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
    }
}
#endif
