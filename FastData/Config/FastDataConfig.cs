using System;
using System.Collections.Generic;

namespace FastData.Config
{
    /// <summary>
    /// FastData 公共配置 API
    ///
    /// 职责：对外暴露统一的配置访问接口，供业务项目、Demo、Example 等调用。
    ///
    /// 设计原则：
    /// 1. 密码脱敏：默认所有摘要 API 自动脱敏密码字段
    /// 2. 完整访问：提供 GetConfig/GetConnectionString 等完整访问入口
    /// 3. 简化调用：内部封装 DataConfig 的复杂逻辑
    ///
    /// 主要功能：
    /// 1. 环境管理：GetActiveEnvironment() 获取当前数据库环境
    /// 2. 连接管理：GetConnectionSummary/ies() 获取连接摘要（脱敏）
    /// 3. Redis 管理：GetRedisSummary() 获取 Redis 摘要
    /// 4. 完整访问：GetConfig(key) 获取完整 ConfigModel
    /// 5. 直接连接：GetConnectionString(key) 获取原始连接字符串
    ///
    /// 注意事项：
    /// - 连接字符串未脱敏，仅供内部使用，对外暴露时需谨慎
    /// - 配置加载在首次调用时触发，可能抛出 FileNotFoundException
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

        /// <summary>
        /// 获取指定 key 的数据库连接字符串（未脱敏）
        /// </summary>
        public static string GetConnectionString(string key)
        {
            var config = DataConfig.GetConfig(key);
            return config?.ConnStr;
        }

        /// <summary>
        /// 获取指定 key 的完整配置对象（供内部项目使用）
        /// </summary>
        public static Model.ConfigModel GetConfig(string key = null, string projectName = null)
        {
            return DataConfig.GetConfig(key, projectName);
        }
    }
}
