#if !NETFRAMEWORK
using System;

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
    }
}
#endif
