namespace FastData.Adapter
{
    internal class DatabaseAdapter : IDatabaseAdapter
    {
        public DatabaseAdapter(string dbType, string providerName, ISqlDialect dialect)
        {
            DbType = dbType;
            ProviderName = providerName;
            Dialect = dialect;
        }

        public string DbType { get; private set; }

        public string ProviderName { get; private set; }

        public ISqlDialect Dialect { get; private set; }
    }
}
