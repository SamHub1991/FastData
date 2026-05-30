using System;
using FastData.DbTypes;

namespace FastData.Aop
{
    /// <summary>
    /// SQL 异常上下文
    /// 
    /// 在 SQL 执行异常时传递给 AOP 拦截器，包含异常信息。
    /// </summary>
    public class ExceptionContext
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataDbType dbType { get; set; }

        /// <summary>
        /// AOP 操作类型
        /// </summary>
        public AopType type { get; set; }

        /// <summary>
        /// 操作名称（如方法名、Map 名称等）
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception ex { get; set; }
    }
}
