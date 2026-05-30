namespace FastData.Tooling.Database
{
    /// <summary>
    /// 数据库列信息
    /// 
    /// 存储从数据库中读取的列结构信息。
    /// </summary>
    public class DatabaseColumn
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据库数据类型（如 nvarchar、int、datetime）
        /// </summary>
        public string DbType { get; set; }

        /// <summary>
        /// 长度（字符串类型）
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 精度（数值类型）
        /// </summary>
        public int Precision { get; set; }

        /// <summary>
        /// 小数位数（数值类型）
        /// </summary>
        public int Scale { get; set; }

        /// <summary>
        /// 是否允许为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 列注释
        /// </summary>
        public string Comment { get; set; }
    }
}
