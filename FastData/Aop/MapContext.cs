using System.Collections.Generic;
using System.Data.Common;

namespace FastData.Aop
{
    /// <summary>
    /// Map SQL 上下文基类
    /// 
    /// 包含 Map SQL 执行的基本信息。
    /// </summary>
    public class MapContext
    {
        /// <summary>
        /// 数据库类型（字符串形式）
        /// </summary>
        public string dbType { get; set; }

        /// <summary>
        /// SQL 语句
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
    }
}
