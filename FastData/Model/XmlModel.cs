using System.Collections.Generic;

namespace FastData.Model
{
    /// <summary>
    /// XML 配置解析结果模型
    /// 
    /// 存储从 XML 文件中解析出来的 SQL 配置信息。
    /// </summary>
    public class XmlModel
    {
        /// <summary>
        /// SQL 键名列表（Map Name）
        /// </summary>
        public List<string> key { get; set; } = new List<string>();

        /// <summary>
        /// SQL 语句列表（与 key 一一对应）
        /// </summary>
        public List<string> sql { get; set; } = new List<string>();

        /// <summary>
        /// 数据库映射（键名 → 数据库名称）
        /// </summary>
        public Dictionary<string, object> db { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 类型映射（参数名 → 数据类型）
        /// </summary>
        public Dictionary<string, object> type { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 视图映射（键名 → 是否为视图）
        /// </summary>
        public Dictionary<string, object> view { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 参数映射（键名 → 参数列表）
        /// </summary>
        public Dictionary<string, object> param { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 验证规则映射（参数名 → 验证规则）
        /// </summary>
        public Dictionary<string, object> check { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 名称映射（键名 → 显示名称）
        /// </summary>
        public Dictionary<string, object> name { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 参数名称映射（内部参数名 → 外部参数名）
        /// </summary>
        public Dictionary<string, object> parameName { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 解析是否成功
        /// </summary>
        public bool isSuccess { get; set; }
    }
}
