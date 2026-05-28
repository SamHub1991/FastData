using System;
using System.Collections.Generic;

namespace FastData.Config
{
    /// <summary>
    /// 公共配置 API（供外部项目调用，密码脱敏）
    /// </summary>
    public static class FastDataConfig
    {
        /// <summary>
        /// 获取当前活跃环境名称
        /// </summary>
        public static string GetActiveEnvironment()
        {
            return DataConfig.GetActiveEnvironment();
        }

        /// <summary>
        /// 获取数据库连接列表（密码脱敏）
        /// </summary>
        public static List<Dictionary<string, string>> GetConnectionSummaries()
        {
            return DataConfig.GetConnectionSummaries();
        }

        /// <summary>
        /// 获取指定数据库连接配置（密码脱敏）
        /// </summary>
        public static Dictionary<string, string> GetConnectionSummary(string key)
        {
            var list = DataConfig.GetConnectionSummaries();
            foreach (var c in list)
            {
                if (string.Equals(c["key"], key, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        /// <summary>
        /// 获取 Redis 配置摘要（密码脱敏）
        /// </summary>
        public static Dictionary<string, string> GetRedisSummary()
        {
            return DataConfig.GetRedisSummary();
        }

        /// <summary>
        /// 获取 Redis 配置（完整对象，供内部项目使用）
        /// </summary>
        public static Model.RedisConfig GetRedisConfig()
        {
            return DataConfig.GetRedisConfigPublic();
        }
    }
}
