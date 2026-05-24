namespace FastData.Adapter
{
    internal class Db2Dialect : ISqlDialect
    {
        public string ApplyTake(string sql, int take)
        {
            return take <= 0 ? sql : sql + " fetch first " + take + " rows only";
        }

        public string BuildParameterName(string name)
        {
            return "@" + name.TrimStart('@', ':', '?');
        }
    }
}
