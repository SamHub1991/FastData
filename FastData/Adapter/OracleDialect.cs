namespace FastData.Adapter
{
    internal class OracleDialect : ISqlDialect
    {
        public string ApplyTake(string sql, int take)
        {
            return take <= 0 ? sql : sql + " and rownum <=" + take;
        }

        public string BuildParameterName(string name)
        {
            return ":" + name.TrimStart('@', ':', '?');
        }
    }
}
