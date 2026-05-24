namespace FastData.Adapter
{
    public interface ISqlDialect
    {
        string ApplyTake(string sql, int take);

        string BuildParameterName(string name);
    }
}
