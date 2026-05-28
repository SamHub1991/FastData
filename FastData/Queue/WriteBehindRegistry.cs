#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FastData.Queue
{
    /// <summary>
    /// 写入后端队列注册表
    /// 管理表级别的队列配置，支持消息队列降级和自动恢复
    /// </summary>
    public static class WriteBehindRegistry
    {
        private static readonly Dictionary<string, WriteBehindConfig> _configs = new Dictionary<string, WriteBehindConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

/// <summary>
        /// 注册表的队列配置
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="config">队列配置</param>
        public static void Register<T>(WriteBehindConfig config) where T : class
        {
            var tableName = Base.TableNameHelper.GetTableName<T>();
            Register(tableName, config);
        }

        /// <summary>
        /// 注册表的队列配置
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="config">队列配置</param>
        public static void Register(string tableName, WriteBehindConfig config)
        {
            if (string.IsNullOrEmpty(tableName))
                return;

            lock (_lock)
            {
                _configs[tableName] = config;
            }
        }

        /// <summary>

        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>队列配置，如果未注册则返回 null</returns>
        public static WriteBehindConfig GetConfig<T>() where T : class
        {
            var tableName = Base.TableNameHelper.GetTableName<T>();
            return GetConfig(tableName);
        }

        /// <summary>
        /// 获取表的队列配置
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>队列配置，如果未注册则返回 null</returns>
        public static WriteBehindConfig GetConfig(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            lock (_lock)
            {
                return _configs.TryGetValue(tableName, out var config) ? config : null;
            }
        }

        /// <summary>
        /// 检查表是否启用了队列
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>是否启用队列</returns>
        public static bool IsQueueEnabled<T>() where T : class
        {
            var config = GetConfig<T>();
            return config != null && config.QueueType != WriteBehindQueueType.None;
        }

        /// <summary>
        /// 检查表是否启用了队列
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>是否启用队列</returns>
        public static bool IsQueueEnabled(string tableName)
        {
            var config = GetConfig(tableName);
            return config != null && config.QueueType != WriteBehindQueueType.None;
        }

        /// <summary>
        /// 移除表的队列配置
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        public static void Unregister<T>() where T : class
        {
            var tableName = Base.TableNameHelper.GetTableName<T>();
            Unregister(tableName);
        }

        /// <summary>
        /// 移除表的队列配置
        /// </summary>
        /// <param name="tableName">表名</param>
        public static void Unregister(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return;

            lock (_lock)
            {
                _configs.Remove(tableName);
            }
        }

        /// <summary>
        /// 获取所有已注册的配置
        /// </summary>
        /// <returns>配置字典（键为表名）</returns>
        public static Dictionary<string, WriteBehindConfig> GetAllConfigs()
        {
            lock (_lock)
            {
                return new Dictionary<string, WriteBehindConfig>(_configs, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 清空所有配置
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _configs.Clear();
            }
        }

        /// <summary>
        /// 从配置文件加载队列配置
        /// 配置文件格式：JSON 数组，每个元素包含 TableName 和 Config
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        public static void LoadFromConfig(string configPath = null)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "writebehind.json");
            }

            if (!File.Exists(configPath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var entries = JsonConvert.DeserializeObject<List<WriteBehindConfigEntry>>(json);

                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        if (!string.IsNullOrEmpty(entry.TableName) && entry.Config != null)
                        {
                            Register(entry.TableName, entry.Config);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载写入后端配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        public static void SaveToConfig(string configPath = null)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "writebehind.json");
            }

            var entries = new List<WriteBehindConfigEntry>();
            var configs = GetAllConfigs();

            foreach (var kvp in configs)
            {
                entries.Add(new WriteBehindConfigEntry
                {
                    TableName = kvp.Key,
                    Config = kvp.Value
                });
            }

            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// 配置文件条目
        /// </summary>
        private class WriteBehindConfigEntry
        {
            public string TableName { get; set; }
            public WriteBehindConfig Config { get; set; }
        }
    }
}
#endif
