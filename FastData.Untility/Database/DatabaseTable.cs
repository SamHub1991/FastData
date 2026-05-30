namespace FastData.Tooling.Database
{
    /// <summary>
    /// 数据库表信息
    /// 
    /// 存储从数据库中读取的表结构信息。
    /// </summary>
    public class DatabaseTable
    {
        /// <summary>
        /// 架构名（如 dbo、public）
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 表注释
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 是否为视图
        /// </summary>
        public bool IsView { get; set; }

        /// <summary>
        /// 完整表名（架构名.表名）
        /// </summary>
        public string FullName
        {
            get
            {
                return string.IsNullOrEmpty(Schema) ? Name : Schema + "." + Name;
            }
        }
    }
}
