using System.Collections.Generic;
using System.Data.Common;

namespace FastData.Model
{
    #region 查询
    /// <summary>
    /// 查询
    /// </summary>
    public sealed class DataQuery
    {
        /// <summary>
        /// 条件集
        /// </summary>
        internal List<VisitModel> Predicate { set; get; } = new List<VisitModel>();

        /// <summary>
        /// 排序
        /// </summary>
        internal List<string> OrderBy { set; get; } = new List<string>();

        /// <summary>
        /// group by
        /// </summary>
        internal List<string> GroupBy { set; get; } = new List<string>();

        /// <summary>
        /// 字段集
        /// </summary>
        internal List<string> Field { set; get; } = new List<string>();

        /// <summary>
        /// 前几条
        /// </summary>
        internal int Take { get; set; }

        /// <summary>
        /// 数据库键
        /// </summary>
        internal string Key { get; set; }

        /// <summary>
        /// 字段集别名
        /// </summary>
        internal List<string> AsName { set; get; } = new List<string>();

        /// <summary>
        /// 表集
        /// </summary>
        internal List<string> Table { set; get; } = new List<string>();

        /// <summary>
        /// 连接配置
        /// </summary>
        internal ConfigModel Config { get; set; }

        /// <summary>
        /// 链式追加的 WHERE 条件（AND/OR）
        /// </summary>
        internal List<ChainedCondition> ChainedConditions { set; get; } = new List<ChainedCondition>();
    }
    #endregion

    #region 链式条件
    /// <summary>
    /// 链式条件（用于 Where/Or 链式调用）
    /// </summary>
    internal class ChainedCondition
    {
        /// <summary>
        /// 逻辑运算符（AND/OR）
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// WHERE 条件字符串
        /// </summary>
        public string Where { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        public List<DbParameter> Param { get; set; } = new List<DbParameter>();
    }
    #endregion
}
