using System;
using System.Collections.Generic;
using System.Data.Common;

namespace FastData.Database
{
    /// <summary>
    /// 数据库提供程序映射
    /// 集中管理数据库类型与提供程序名称的映射关系
    /// </summary>
    public static class DatabaseProviderMappings
    {
        /// <summary>
        /// 提供程序名称到显示名称的映射
        /// </summary>
        public static readonly Dictionary<string, string> ProviderDisplayNames = new Dictionary<string, string>
        {
            { "System.Data.SqlClient", "SQL Server" },
            { "Microsoft.Data.SqlClient", "SQL Server" },
            { "MySql.Data.MySqlClient", "MySQL" },
            { "Oracle.ManagedDataAccess.Client", "Oracle" },
            { "System.Data.SQLite", "SQLite" },
            { "IBM.Data.DB2.iSeries", "DB2" },
            { "Npgsql", "PostgreSQL" }
        };

        /// <summary>
        /// 显示名称到提供程序名称的映射
        /// </summary>
        public static readonly Dictionary<string, string> DisplayNameToProvider = new Dictionary<string, string>
        {
            { "SQL Server", "System.Data.SqlClient" },
            { "MySQL", "MySql.Data.MySqlClient" },
            { "Oracle", "Oracle.ManagedDataAccess.Client" },
            { "SQLite", "System.Data.SQLite" },
            { "DB2", "IBM.Data.DB2.iSeries" },
            { "PostgreSQL", "Npgsql" }
        };

        /// <summary>
        /// 获取所有支持的提供程序名称列表
        /// </summary>
        public static string[] AllProviderNames => new[]
        {
            "System.Data.SqlClient",
            "Microsoft.Data.SqlClient",
            "MySql.Data.MySqlClient",
            "Oracle.ManagedDataAccess.Client",
            "System.Data.SQLite",
            "Npgsql"
        };

        /// <summary>
        /// 获取所有支持的显示名称列表
        /// </summary>
        public static string[] AllDisplayNames => new[]
        {
            "SQL Server",
            "MySQL",
            "Oracle",
            "SQLite",
            "PostgreSQL"
        };

        /// <summary>
        /// 根据提供程序名称获取显示名称
        /// </summary>
        public static string GetDisplayName(string providerName)
        {
            return ProviderDisplayNames.TryGetValue(providerName, out var displayName) 
                ? displayName 
                : providerName;
        }

        /// <summary>
        /// 根据显示名称获取提供程序名称
        /// </summary>
        public static string GetProviderName(string displayName)
        {
            return DisplayNameToProvider.TryGetValue(displayName, out var providerName) 
                ? providerName 
                : displayName;
        }

        /// <summary>
        /// 检查是否支持该提供程序
        /// </summary>
        public static bool IsSupported(string providerName)
        {
            return ProviderDisplayNames.ContainsKey(providerName);
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        public static DbConnection CreateConnection(string providerName, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("提供程序名称不能为空", nameof(providerName));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("连接字符串不能为空", nameof(connectionString));

            var factory = DbProviderFactories.GetFactory(providerName);
            var connection = factory.CreateConnection();
            
            if (connection == null)
                throw new InvalidOperationException($"无法为提供程序 '{providerName}' 创建数据库连接");
            
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}
