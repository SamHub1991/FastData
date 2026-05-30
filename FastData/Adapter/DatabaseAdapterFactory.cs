using FastData.Base;
using FastData.DbTypes;

namespace FastData.Adapter
{
    public static class DatabaseAdapterFactory
    {
        public static IDatabaseAdapter Create(DataDbType dbType)
        {
            switch (dbType)
            {
                case DataDbType.Oracle:
                    return new DatabaseAdapter(DataDbType.Oracle, Provider.Oracle, new OracleDialect());
                case DataDbType.MySql:
                    return new DatabaseAdapter(DataDbType.MySql, Provider.MySql, new LimitDialect());
                case DataDbType.DB2:
                    return new DatabaseAdapter(DataDbType.DB2, Provider.DB2, new Db2Dialect());
                case DataDbType.SQLite:
                    return new DatabaseAdapter(DataDbType.SQLite, Provider.SQLite, new SqliteDialect());
                case DataDbType.PostgreSql:
                    return new DatabaseAdapter(DataDbType.PostgreSql, Provider.PostgreSql, new LimitDialect());
                default:
                    return new DatabaseAdapter(DataDbType.SqlServer, Provider.SqlServer, new SqlServerDialect());
            }
        }
    }
}
