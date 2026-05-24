namespace FastData.Adapter
{
    internal class LimitDialect : ISqlDialect
    {
        public string ApplyTake(string sql, int take)
        {
            return take <= 0 ? sql : sql + " limit " + take;
        }

        public string BuildParameterName(string name)
        {
            return "@" + name.TrimStart('@', ':', '?');
        }
    }
}
