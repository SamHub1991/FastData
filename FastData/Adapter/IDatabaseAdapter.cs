using FastData.DbTypes;

namespace FastData.Adapter
{
    public interface IDatabaseAdapter
    {
        DataDbType DbType { get; }

        string ProviderName { get; }

        ISqlDialect Dialect { get; }
    }
}
