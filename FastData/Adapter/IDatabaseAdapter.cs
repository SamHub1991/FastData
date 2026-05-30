using FastData.DbTypes;

namespace FastData.Adapter
{
    /// <summary>
    /// 数据库适配器接口
    /// 
    /// 定义数据库适配器的基本属性，用于统一不同数据库的访问方式。
    /// </summary>
    public interface IDatabaseAdapter
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        DataDbType DbType { get; }

        /// <summary>
        /// 数据库提供程序名称（如 System.Data.SqlClient）
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// SQL 方言实现（用于生成特定数据库的 SQL 语句）
        /// </summary>
        ISqlDialect Dialect { get; }
    }
}
