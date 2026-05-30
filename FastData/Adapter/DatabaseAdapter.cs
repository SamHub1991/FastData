using FastData.DbTypes;

namespace FastData.Adapter
{
    /// <summary>
    /// 数据库适配器实现
    /// 
    /// 封装数据库类型、提供程序名称和 SQL 方言。
    /// </summary>
    internal class DatabaseAdapter : IDatabaseAdapter
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <param name="providerName">提供程序名称</param>
        /// <param name="dialect">SQL 方言实现</param>
        public DatabaseAdapter(DataDbType dbType, string providerName, ISqlDialect dialect)
        {
            DbType = dbType;
            ProviderName = providerName;
            Dialect = dialect;
        }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataDbType DbType { get; private set; }

        /// <summary>
        /// 数据库提供程序名称
        /// </summary>
        public string ProviderName { get; private set; }

        /// <summary>
        /// SQL 方言实现
        /// </summary>
        public ISqlDialect Dialect { get; private set; }
    }
}
