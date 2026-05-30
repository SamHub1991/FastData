namespace FastData.Adapter
{
    /// <summary>
    /// SQL Server 方言实现
    /// 
    /// 处理 SQL Server 特有的 SQL 语法。
    /// </summary>
    internal class SqlServerDialect : ISqlDialect
    {
        /// <summary>
        /// 应用 TOP 子句
        /// </summary>
        /// <param name="sql">原始 SQL 语句</param>
        /// <param name="take">获取的记录数</param>
        /// <returns>添加了 TOP 的 SQL 语句</returns>
        public string ApplyTake(string sql, int take)
        {
            if (take <= 0)
                return sql;

            var value = sql ?? string.Empty;
            if (value.ToLower().StartsWith("select "))
                return "select top " + take + " " + value.Substring(7);

            return value;
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
