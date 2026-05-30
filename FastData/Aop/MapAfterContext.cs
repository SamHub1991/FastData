using System.Collections.Generic;
using System.Data.Common;
using FastData.DbTypes;

namespace FastData.Aop
{
    /// <summary>
    /// Map SQL 执行后上下文
    /// 
    /// 在 Map SQL 执行后传递给 AOP 拦截器，包含执行结果信息。
    /// </summary>
    public class MapAfterContext
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataDbType dbType { get; set; }

        /// <summary>
        /// 已执行的 SQL 语句
        /// </summary>
        public string sql { get; set; }

        /// <summary>
        /// Map 名称（XML 中定义的 Name）
        /// </summary>
        public string mapName { get; set; }

        /// <summary>
        /// SQL 参数列表
        /// </summary>
        public List<DbParameter> param { get; set; } = new List<DbParameter>();

        /// <summary>
        /// AOP 操作类型
        /// </summary>
        public AopType type { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public object result { get; set; }
    }
}
