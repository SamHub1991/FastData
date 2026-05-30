using System;

namespace FastData.Tooling.Database
{
    /// <summary>
    /// 元数据读取器工厂
    /// 
    /// 根据数据库连接选项创建对应的元数据读取器。
    /// 
    /// 使用示例：
    /// <code>
    /// var options = new DatabaseConnectionOptions
    /// {
    ///     Provider = "System.Data.SqlClient",
    ///     ConnectionString = "server=.;database=TestDb;trusted_connection=true;"
    /// };
    /// 
    /// var reader = MetadataReaderFactory.Create(options);
    /// var tables = reader.GetTables();
    /// </code>
    /// </summary>
    public static class MetadataReaderFactory
    {
        /// <summary>
        /// 创建元数据读取器
        /// </summary>
        /// <param name="options">数据库连接选项</param>
        /// <returns>元数据读取器实例</returns>
        /// <exception cref="ArgumentNullException">选项为空时抛出</exception>
        /// <exception cref="ArgumentException">提供程序为空时抛出</exception>
        public static IDatabaseMetadataReader Create(DatabaseConnectionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (string.IsNullOrEmpty(options.Provider))
                throw new ArgumentException("Provider不能为空", "options");

            return new ProviderMetadataReader(options);
        }
    }
}
