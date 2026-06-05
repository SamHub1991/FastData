namespace FastData.Base
{
    /// <summary>
    /// 缓存键常量集合
    /// 定义系统中各种缓存键的格式模板
    /// </summary>
    internal static class CacheKeys
    {
        /// <summary>
        /// 表缓存键格式，{0} 为表名
        /// </summary>
        public const string TableKeyFormat = "FastData.Cache.Table.{0}";
        
        /// <summary>
        /// FastMap API 缓存键
        /// </summary>
        public const string MapApiKey = "FastMap.Api";
        
        /// <summary>
        /// ForEach 类型缓存键格式，{0} 为名称
        /// </summary>
        public const string ForEachTypeKeyFormat = "ForEach.{0}";
    }

    /// <summary>
    /// 设计模式常量
    /// 用于标识当前使用的 ORM 设计模式（CodeFirst 或 DbFirst）
    /// </summary>
    internal static class DesignPatterns
    {
        /// <summary>
        /// 代码优先模式：从实体类生成数据库表结构
        /// </summary>
        public const string CodeFirst = "CodeFirst";
        
        /// <summary>
        /// 数据库优先模式：从数据库表结构生成实体类
        /// </summary>
        public const string DbFirst = "DbFirst";
    }

    /// <summary>
    /// 缓存类型常量
    /// 定义系统支持的缓存存储类型
    /// </summary>
    internal static class CacheType
    {
        /// <summary>
        /// Web 内存缓存
        /// </summary>
        public const string Web = "web";
        
        /// <summary>
        /// Redis 分布式缓存
        /// </summary>
        public const string Redis = "redis";
    }

    /// <summary>
    /// SQL 错误日志类型常量
    /// 定义错误日志的存储方式
    /// </summary>
    internal static class SqlErrorType
    {
        /// <summary>
        /// 数据库存储错误日志（Data_LogError 表）
        /// </summary>
        public const string Database = "db";
        
        /// <summary>
        /// 数据库存储错误日志（Data_LogError 表），与 Database 别名
        /// </summary>
        public const string Db = "db";
        
        /// <summary>
        /// 文件存储错误日志
        /// </summary>
        public const string File = "file";
    }

    /// <summary>
    /// Map XML 缓存键常量
    /// 定义 XML 映射中各种缓存键的格式模板
    /// </summary>
    internal static class MapXmlKeys
    {
        /// <summary>
        /// 格式化键格式，{0} 为名称，{1} 为格式类型
        /// </summary>
        public const string FormatKeyFormat = "{0}.format.{1}";
        
        /// <summary>
        /// 参数键格式，{0} 为名称
        /// </summary>
        public const string ParamKeyFormat = "{0}.param";
        
        /// <summary>
        /// ForEach 名称键格式，{0} 为名称，{1} 为索引
        /// </summary>
        public const string ForeachNameKeyFormat = "{0}.foreach.name.{1}";
        
        /// <summary>
        /// ForEach 字段键格式，{0} 为名称，{1} 为索引
        /// </summary>
        public const string ForeachFieldKeyFormat = "{0}.foreach.field.{1}";
        
        /// <summary>
        /// ForEach SQL 键格式，{0} 为名称，{1} 为索引
        /// </summary>
        public const string ForeachSqlKeyFormat = "{0}.foreach.sql.{1}";
        
        /// <summary>
        /// ForEach 类型键格式，{0} 为名称，{1} 为索引
        /// </summary>
        public const string ForeachTypeKeyFormat = "{0}.foreach.type.{1}";
        
        /// <summary>
        /// 类型键格式，{0} 为名称
        /// </summary>
        public const string TypeKeyFormat = "{0}.type";
        
        /// <summary>
        /// 视图键格式，{0} 为名称
        /// </summary>
        public const string ViewKeyFormat = "{0}.view";
        
        /// <summary>
        /// 备注键格式，{0} 为名称
        /// </summary>
        public const string RemarkKeyFormat = "{0}.remark";
        
        /// <summary>
        /// 日志键格式，{0} 为名称
        /// </summary>
        public const string LogKeyFormat = "{0}.log";
        
        /// <summary>
        /// 数据库键格式，{0} 为名称
        /// </summary>
        public const string DbKeyFormat = "{0}.db";
    }
}
