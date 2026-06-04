using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastData.Base
{
    /// <summary>
    /// 数据库驱动名称常量集合
    ///
    /// 职责：统一管理所有支持的数据库 ADO.NET 提供程序名称，避免硬编码。
    ///
    /// .NET Framework 4.5.2 兼容性约束（已查阅官方 NuGet）：
    /// - System.Data.SqlClient  ：✅ 支持（.NET Framework 内置，net4.5+ 原生可用）
    /// - MySql.Data.MySqlClient ：✅ 支持（官方 Oracle 包，net4.5+ 可用）
    /// - MySqlConnector        ：⚠️ 不支持 net452（最新版本要求 net461+）
    /// - Microsoft.Data.SqlClient：❌ 不支持 net452（要求 net46+）
    /// - Microsoft.Data.Sqlite ：❌ 不支持 net452（要求 net461+）
    /// - System.Data.SQLite    ：✅ 支持（官方 SQLite，net4.5+ 可用）
    /// - Npgsql                ：⚠️ 4.x 支持（4.0.x 最后一个支持 net45 的版本）
    /// - IBM.Data.DB2.Core     ：✅ 支持（IBM 官方，net4.5+ 可用）
    ///
    /// 实际提供程序由调用方通过 NuGet 引用并注册 DbProviderFactory。
    /// 本类只保存 InvariantName 字符串常量；选择哪个驱动由消费者决定。
    /// </summary>
    public static class Provider
    {
        /// <summary>
        /// Oracle 数据库驱动名称（Oracle.ManagedDataAccess.Client，支持 .NET Framework 4.5+）
        /// </summary>
        public readonly static string Oracle = "Oracle.ManagedDataAccess.Client";

        /// <summary>
        /// MySQL 数据库驱动名称（MySql.Data.MySqlClient，.NET Framework 4.5+ 原生支持）
        /// </summary>
        public readonly static string MySql = "MySql.Data.MySqlClient";

        /// <summary>
        /// SQL Server 数据库驱动名称（System.Data.SqlClient，.NET Framework 内置）
        /// </summary>
        public readonly static string SqlServer = "System.Data.SqlClient";

        /// <summary>
        /// SQLite 数据库驱动名称（System.Data.SQLite，.NET Framework 4.5+ 支持）
        /// </summary>
        public readonly static string SQLite = "System.Data.SQLite";

        /// <summary>
        /// SQLite 数据库驱动名称（Microsoft.Data.Sqlite，.NET 6+ 推荐，跨平台纯托管）
        /// </summary>
        public readonly static string MicrosoftDataSqlite = "Microsoft.Data.Sqlite";

        /// <summary>
        /// DB2 数据库驱动名称（IBM.Data.DB2.Core）
        /// </summary>
        public readonly static string DB2 = "IBM.Data.DB2.Core";

        /// <summary>
        /// PostgreSQL 数据库驱动名称（Npgsql，4.x 支持 .NET Framework 4.5）
        /// </summary>
        public readonly static string PostgreSql = "Npgsql";
    }
}
