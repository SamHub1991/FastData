namespace FastData.Base
{
    internal static class CacheKeys
    {
        public const string TableKeyFormat = "FastData.Cache.Table.{0}";
        
        public const string MapApiKey = "FastMap.Api";
        
        public const string ForEachTypeKeyFormat = "ForEach.{0}";
    }

    internal static class DesignPatterns
    {
        public const string CodeFirst = "CodeFirst";
        
        public const string DbFirst = "DbFirst";
    }

    internal static class CacheType
    {
        public const string Web = "web";
        
        public const string Redis = "redis";
    }

    internal static class SqlErrorType
    {
        public const string Database = "db";
        public const string Db = "db";
        
        public const string File = "file";
    }

    internal static class MapXmlKeys
    {
        public const string FormatKeyFormat = "{0}.format.{1}";
        
        public const string ParamKeyFormat = "{0}.param";
        
        public const string ForeachNameKeyFormat = "{0}.foreach.name.{1}";
        
        public const string ForeachFieldKeyFormat = "{0}.foreach.field.{1}";
        
        public const string ForeachSqlKeyFormat = "{0}.foreach.sql.{1}";
        
        public const string ForeachTypeKeyFormat = "{0}.foreach.type.{1}";
        
        public const string TypeKeyFormat = "{0}.type";
        
        public const string ViewKeyFormat = "{0}.view";
        
        public const string RemarkKeyFormat = "{0}.remark";
        
        public const string LogKeyFormat = "{0}.log";
        
        public const string DbKeyFormat = "{0}.db";
    }
}
