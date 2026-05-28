namespace FastData.Model
{
    /// <summary>
    /// Redis 配置模型
    /// </summary>
    public class RedisConfig
    {
        /// <summary>
        /// Redis 服务器地址（格式：host:port）
        /// </summary>
        public string Server { get; set; } = "127.0.0.1:6379";

        /// <summary>
        /// Redis 数据库索引
        /// </summary>
        public int Db { get; set; } = 0;

        /// <summary>
        /// Redis 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// 同步超时（毫秒）
        /// </summary>
        public int SyncTimeout { get; set; } = 5000;
    }
}
