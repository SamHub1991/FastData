namespace FastData.Tooling.Database
{
    /// <summary>
    /// 数据库连接选项
    /// 
    /// 存储数据库连接的配置信息。
    /// </summary>
    public class DatabaseConnectionOptions
    {
        /// <summary>
        /// 数据库提供程序名称（如 System.Data.SqlClient）
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DatabaseName { get; set; }
    }
}
