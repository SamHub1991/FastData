namespace FastData.Adapter
{
    /// <summary>
    /// SQLite 方言实现
    /// 
    /// 处理 SQLite 特有的 SQL 语法。
    /// </summary>
    internal class SqliteDialect : ISqlDialect
    {
        /// <summary>
        /// 应用 LIMIT 子句
        /// </summary>
        /// <param name="sql">原始 SQL 语句</param>
        /// <param name="take">获取的记录数</param>
        /// <returns>添加了 LIMIT 的 SQL 语句</returns>
        public string ApplyTake(string sql, int take)
        {
            return take <= 0 ? sql : sql + " limit " + take;
        }

        /// <summary>
        /// 构建参数名称（使用 @ 前缀）
        /// </summary>
        /// <param name="name">参数名（不含前缀）</param>
        /// <returns>带 @ 前缀的参数名</returns>
        public string BuildParameterName(string name)
        {
            return "@" + name.TrimStart('@', ':', '?');
        }
    }
}
