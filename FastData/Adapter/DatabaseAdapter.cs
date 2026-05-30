using FastData.DbTypes;

namespace FastData.Adapter
{
    internal class DatabaseAdapter : IDatabaseAdapter
    {
        public DatabaseAdapter(DataDbType dbType, string providerName, ISqlDialect dialect)
        {
            DbType = dbType;
            ProviderName = providerName;
            Dialect = dialect;
        }

        public DataDbType DbType { get; private set; }

        public string ProviderName { get; private set; }

        public ISqlDialect Dialect { get; private set; }
    }
}
