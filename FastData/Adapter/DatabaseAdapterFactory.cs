using FastData.Base;
using FastData.Type;

namespace FastData.Adapter
{
    public static class DatabaseAdapterFactory
    {
        public static IDatabaseAdapter Create(string dbType)
        {
            var value = (dbType ?? string.Empty).ToLower();
            if (value == DataDbType.Oracle.ToLower())
                return new DatabaseAdapter(DataDbType.Oracle, Provider.Oracle, new OracleDialect());

            if (value == DataDbType.MySql.ToLower())
                return new DatabaseAdapter(DataDbType.MySql, Provider.MySql, new LimitDialect());

            if (value == DataDbType.DB2.ToLower())
                return new DatabaseAdapter(DataDbType.DB2, Provider.DB2, new Db2Dialect());

            if (value == DataDbType.SQLite.ToLower())
                return new DatabaseAdapter(DataDbType.SQLite, Provider.SQLite, new SqliteDialect());

            if (value == DataDbType.PostgreSql.ToLower())
                return new DatabaseAdapter(DataDbType.PostgreSql, Provider.PostgreSql, new LimitDialect());

            return new DatabaseAdapter(DataDbType.SqlServer, Provider.SqlServer, new SqlServerDialect());
        }
    }
}
