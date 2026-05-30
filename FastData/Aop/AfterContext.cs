using System.Collections.Generic;
using System.Data.Common;
using FastData.DbTypes;

namespace FastData.Aop
{
    /// <summary>
    /// SQL 执行后上下文
    /// 
    /// 在 SQL 执行后传递给 AOP 拦截器，包含执行结果信息。
    /// </summary>
    public class AfterContext
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataDbType dbType { get; set; }

        /// <summary>
        /// 涉及的表名列表
        /// </summary>
        public List<string> tableName { get; set; } = new List<string>();

        /// <summary>
        /// 已执行的 SQL 语句
        /// </summary>
        public string sql { get; set; }

        /// <summary>
        /// SQL 参数列表
        /// </summary>
        public List<DbParameter> param { get; set; } = new List<DbParameter>();

        /// <summary>
        /// 执行结果
        /// </summary>
        public object result { get; set; }

        /// <summary>
        /// 是否为读操作
        /// </summary>
        public bool isRead { get; set; } = false;

        /// <summary>
        /// 是否为写操作
        /// </summary>
        public bool isWrite { get; set; } = true;

        /// <summary>
        /// AOP 操作类型
        /// </summary>
        public AopType type { get; set; }
    }
}
