using System;

namespace FastData.Property
{
    /// <summary>
    /// 表属性
    /// </summary>
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// 多数据库表名映射
        /// 格式: "数据库Key.表名,数据库Key.表名"
        /// 示例: "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users"
        /// </summary>
        public string DbTableNames { get; set; }
    }
}
