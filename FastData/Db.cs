namespace FastData
{
    /// <summary>
    /// ORM 简洁入口。
    /// </summary>
    public static class Db
    {
        /// <summary>
        /// 使用指定数据库配置 Key 创建客户端；未传 Key 时使用当前作用域或默认配置。
        /// </summary>
        public static FastDataClient Use(string key = null)
        {
            return new FastDataClient(key);
        }

        /// <summary>
        /// 使用默认数据库配置创建客户端。
        /// </summary>
        public static FastDataClient Default => Use();
    }
}
