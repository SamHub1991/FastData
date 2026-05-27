#if !NETFRAMEWORK
using System;
using System.Collections.Generic;

namespace FastData.Queue
{
    /// <summary>
    /// 写入操作类型
    /// </summary>
    public enum WriteOperationType
    {
        /// <summary>
        /// 插入操作
        /// </summary>
        Add,

        /// <summary>
        /// 更新操作
        /// </summary>
        Update,

        /// <summary>
        /// 删除操作
        /// </summary>
        Delete
    }

    /// <summary>
    /// 读取操作类型
    /// </summary>
    public enum ReadOperationType
    {
        /// <summary>
        /// 查询单条
        /// </summary>
        QuerySingle,

        /// <summary>
        /// 查询列表
        /// </summary>
        QueryList,

        /// <summary>
        /// 查询数量
        /// </summary>
        QueryCount,

        /// <summary>
        /// 查询分页
        /// </summary>
        QueryPaging
    }

    /// <summary>
    /// 写入操作模型
    /// 用于消息队列传递和写入后端队列
    /// </summary>
    public class WriteOperation
    {
        /// <summary>
        /// 操作类型（Add/Update/Delete）
        /// </summary>
        public WriteOperationType OperationType { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 实体类型全名
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// 序列化后的数据（JSON 格式）
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 数据库 Key（可选，默认使用主库）
        /// </summary>
        public string DatabaseKey { get; set; }

        /// <summary>
        /// 操作时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作唯一标识
        /// </summary>
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 扩展元数据（可选）
        /// 用于传递额外的业务信息，如来源系统、操作人、业务单号等
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// 读取操作模型
    /// 用于消息队列传递读取请求
    /// </summary>
    public class ReadOperation
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
        /// 实体类型全名
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// 查询条件（JSON 格式的 Lambda 表达式序列化）
        /// </summary>
        public string Predicate { get; set; }

        /// <summary>
        /// 查询字段（可选，为空则查询所有字段）
        /// </summary>
        public string Fields { get; set; }

        /// <summary>
        /// 排序字段（可选）
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// 是否升序
        /// </summary>
        public bool IsAscending { get; set; } = true;

        /// <summary>
        /// 页码（分页查询用）
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小（分页查询用）
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 数据库 Key（可选，默认使用主库）
        /// </summary>
        public string DatabaseKey { get; set; }

        /// <summary>
        /// 操作时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作唯一标识
        /// </summary>
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 扩展元数据（可选）
        /// 用于传递额外的业务信息，如来源系统、操作人、业务单号等
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// 队列操作结果
    /// </summary>
    public class QueueOperationResult
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
        /// 操作 ID
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// 扩展元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }
}
#endif
