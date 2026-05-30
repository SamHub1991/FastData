namespace FastData.DbTypes
{
    /// <summary>
    /// 数据库类型枚举
    /// 
    /// 用于指定数据库类型，支持 6 种主流数据库。
    /// 
    /// 使用示例：
    /// <code>
    /// // 获取数据库适配器
    /// var adapter = DatabaseAdapterFactory.Create(DataDbType.SqlServer);
    /// 
    /// // 配置数据库类型
    /// var config = new ConfigModel
    /// {
    ///     Key = "DefaultDb",
    ///     DbType = DataDbType.MySql,
    ///     ConnStr = "Server=localhost;Database=TestDb;"
    /// };
    /// </code>
    /// </summary>
    public enum DataDbType
    {
        /// <summary>
        /// Oracle 数据库
        /// </summary>
        Oracle = 1,

        /// <summary>
        /// MySQL 数据库
        /// </summary>
        MySql = 2,

        /// <summary>
        /// SQL Server 数据库
        /// </summary>
        SqlServer = 3,

        /// <summary>
        /// DB2 数据库
        /// </summary>
        DB2 = 4,

        /// <summary>
        /// SQLite 数据库
        /// </summary>
        SQLite = 5,

        /// <summary>
        /// PostgreSQL 数据库
        /// </summary>
        PostgreSql = 6
    }
}
