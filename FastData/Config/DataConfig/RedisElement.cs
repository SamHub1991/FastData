using System.Configuration;

namespace FastData.Config
{
    /// <summary>
    /// Redis 配置节点
    /// </summary>
    internal class RedisElement : ConfigurationElement
    {
        /// <summary>
        /// Redis 服务器地址（格式：host:port）
        /// </summary>
        [ConfigurationProperty("Server", IsRequired = false, DefaultValue = "127.0.0.1:6379")]
        public string Server
        {
            get { return (string)base["Server"]; }
            set { base["Server"] = value; }
        }

        /// <summary>
        /// Redis 数据库索引
        /// </summary>
        [ConfigurationProperty("Db", IsRequired = false, DefaultValue = 0)]
        public int Db
        {
            get { return (int)base["Db"]; }
            set { base["Db"] = value; }
        }

        /// <summary>
        /// Redis 密码（可选）
        /// </summary>
        [ConfigurationProperty("Password", IsRequired = false, DefaultValue = "")]
        public string Password
        {
            get { return (string)base["Password"]; }
            set { base["Password"] = value; }
        }

        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        [ConfigurationProperty("ConnectTimeout", IsRequired = false, DefaultValue = 5000)]
        public int ConnectTimeout
        {
            get { return (int)base["ConnectTimeout"]; }
            set { base["ConnectTimeout"] = value; }
        }

        /// <summary>
        /// 同步超时（毫秒）
        /// </summary>
        [ConfigurationProperty("SyncTimeout", IsRequired = false, DefaultValue = 5000)]
        public int SyncTimeout
        {
            get { return (int)base["SyncTimeout"]; }
            set { base["SyncTimeout"] = value; }
        }
    }
}
