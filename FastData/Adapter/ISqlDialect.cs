namespace FastData.Adapter
{
    /// <summary>
    /// SQL 方言接口
    /// 
    /// 定义不同数据库的 SQL 语法差异，用于生成特定数据库的 SQL 语句。
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>
        /// 应用 TOP/LIMIT 子句
        /// </summary>
        /// <param name="sql">原始 SQL 语句</param>
        /// <param name="take">获取的记录数</param>
        /// <returns>添加了 TOP/LIMIT 的 SQL 语句</returns>
        string ApplyTake(string sql, int take);

        /// <summary>
        /// 构建参数名称
        /// </summary>
        /// <param name="name">参数名（不含前缀）</param>
        /// <returns>带前缀的参数名（如 @name、:name）</returns>
        string BuildParameterName(string name);
    }
}
