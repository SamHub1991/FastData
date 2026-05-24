namespace FastData.Adapter
{
    internal class SqlServerDialect : ISqlDialect
    {
        public string ApplyTake(string sql, int take)
        {
            if (take <= 0)
                return sql;

            var value = sql ?? string.Empty;
            if (value.ToLower().StartsWith("select "))
                return "select top " + take + " " + value.Substring(7);

            return value;
        }

        public string BuildParameterName(string name)
        {
            return "@" + name.TrimStart('@', ':', '?');
        }
    }
}
