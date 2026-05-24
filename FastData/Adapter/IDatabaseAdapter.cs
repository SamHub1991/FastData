namespace FastData.Adapter
{
    public interface IDatabaseAdapter
    {
        string DbType { get; }

        string ProviderName { get; }

        ISqlDialect Dialect { get; }
    }
}
