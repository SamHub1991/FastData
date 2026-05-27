#if !NETFRAMEWORK
using System;
using FastData.Queue;

namespace FastData
{
    /// <summary>
    /// FastWrite 扩展方法
    /// 提供消息队列配置和查询功能
    /// </summary>
    public static class FastWriteExtensions
    {
        /// <summary>
        /// 从 TableSyncConfig 加载队列配置
        /// </summary>
        /// <param name="write">FastWrite 实例</param>
        /// <param name="configPath">配置文件路径（可选）</param>
        public static void LoadQueueConfig(string configPath = null)
        {
            WriteBehindRegistry.LoadFromConfig(configPath);
        }

        /// <summary>
        /// 初始化写入后端执行器
        /// </summary>
        /// <param name="redisConnectionString">Redis 连接字符串</param>
        /// <param name="redisDb">Redis 数据库索引</param>
        public static void InitializeWriteBehind(string redisConnectionString = "127.0.0.1:6379", int redisDb = 7)
        {
            WriteBehindExecutor.Initialize(redisConnectionString, redisDb);
        }
    }
}
#endif
