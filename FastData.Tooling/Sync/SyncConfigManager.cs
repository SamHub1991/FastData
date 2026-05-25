using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 同步任务配置管理器
    /// </summary>
    public class SyncConfigManager
    {
        private readonly string configPath;
        private IDictionary<string, SyncTaskConfig> configs;

        public SyncConfigManager(string configFilePath = null)
        {
            configPath = configFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sync_tasks.json");
            configs = new Dictionary<string, SyncTaskConfig>(StringComparer.OrdinalIgnoreCase);
            LoadConfigs();
        }

        /// <summary>
        /// 加载所有配置
        /// </summary>
        private void LoadConfigs()
        {
            if (!File.Exists(configPath))
                return;

            try
            {
                var json = File.ReadAllText(configPath);
                configs = new Dictionary<string, SyncTaskConfig>(StringComparer.OrdinalIgnoreCase);

                var serializer = new JavaScriptSerializer();
                var items = serializer.Deserialize<List<SyncTaskConfig>>(json);

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.TaskId))
                        {
                            configs[item.TaskId] = item;
                        }
                    }
                }
            }
            catch
            {
                configs = new Dictionary<string, SyncTaskConfig>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        private void SaveConfigs()
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(new List<SyncTaskConfig>(configs.Values));

            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// 获取任务配置
        /// </summary>
        public SyncTaskConfig GetTaskConfig(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return null;

            SyncTaskConfig config;
            return configs.TryGetValue(taskId, out config) ? config : null;
        }

        /// <summary>
        /// 保存任务配置
        /// </summary>
        public void SaveTaskConfig(SyncTaskConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.TaskId))
                return;

            configs[config.TaskId] = config;
            SaveConfigs();
        }

        /// <summary>
        /// 更新最后同步时间
        /// </summary>
        public void UpdateLastSyncTime(string taskId, DateTime syncTime)
        {
            var config = GetTaskConfig(taskId);
            if (config != null)
            {
                config.LastSyncTime = syncTime;
                config.IsFirstSync = false;
                SaveTaskConfig(config);
            }
        }

        /// <summary>
        /// 获取所有任务配置
        /// </summary>
        public IList<SyncTaskConfig> GetAllTaskConfigs()
        {
            return new List<SyncTaskConfig>(configs.Values);
        }

        /// <summary>
        /// 获取所有任务配置（用于任务列表）
        /// </summary>
        public IList<SyncTaskConfig> GetAllConfigs()
        {
            return new List<SyncTaskConfig>(configs.Values);
        }

        /// <summary>
        /// 删除任务配置
        /// </summary>
        public void DeleteTaskConfig(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return;

            if (configs.ContainsKey(taskId))
            {
                configs.Remove(taskId);
                SaveConfigs();
            }
        }

        /// <summary>
        /// 更新任务状态
        /// </summary>
        public void UpdateTaskStatus(string taskId, string status, string message)
        {
            var config = GetTaskConfig(taskId);
            if (config != null)
            {
                config.LastSyncStatus = status;
                config.LastSyncMessage = message;
                config.ModifiedTime = DateTime.Now;
                SaveTaskConfig(config);
            }
        }
    }
}
