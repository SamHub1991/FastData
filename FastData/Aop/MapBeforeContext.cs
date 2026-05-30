using System.Collections.Generic;
using System.Data.Common;
using FastData.DbTypes;

namespace FastData.Aop
{
    /// <summary>
    /// Map SQL 执行前上下文
    /// 
    /// 在 Map SQL 执行前传递给 AOP 拦截器，包含即将执行的 Map SQL 信息。
    /// </summary>
    public class MapBeforeContext
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataDbType dbType { get; set; }

        /// <summary>
        /// 即将执行的 SQL 语句
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
    }
}
