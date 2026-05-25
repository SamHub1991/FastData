using System;
using System.Collections.Generic;
using System.IO;

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
                
                // 简单 JSON 解析（避免依赖 Newtonsoft.Json）
                var lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                SyncTaskConfig currentConfig = null;
                string currentTaskId = null;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("\"TaskId\""))
                    {
                        currentTaskId = ExtractStringValue(trimmed);
                        currentConfig = new SyncTaskConfig { TaskId = currentTaskId };
                    }
                    else if (currentConfig != null)
                    {
                        if (trimmed.StartsWith("\"TaskName\""))
                            currentConfig.TaskName = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"SourceTable\""))
                            currentConfig.SourceTable = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"TargetTable\""))
                            currentConfig.TargetTable = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"PrimaryKeyColumns\""))
                            currentConfig.PrimaryKeyColumns = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"TimeColumn\""))
                            currentConfig.TimeColumn = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"EnableTimeRange\""))
                            currentConfig.EnableTimeRange = trimmed.Contains("true");
                        else if (trimmed.StartsWith("\"LastSyncTime\""))
                            currentConfig.LastSyncTime = ExtractDateTimeValue(trimmed);
                        else if (trimmed.StartsWith("\"RangeDays\""))
                            currentConfig.RangeDays = ExtractIntValue(trimmed);
                        else if (trimmed.StartsWith("\"DataType\""))
                            currentConfig.DataType = ExtractStringValue(trimmed) == "Dynamic" ? SyncDataType.Dynamic : SyncDataType.Static;
                        else if (trimmed.StartsWith("\"SyncColumns\""))
                            currentConfig.SyncColumns = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"SourceConnection\""))
                            currentConfig.SourceConnection = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"TargetConnection\""))
                            currentConfig.TargetConnection = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("\"IntermediateConnection\""))
                            currentConfig.IntermediateConnection = ExtractStringValue(trimmed);
                        else if (trimmed.StartsWith("}") && currentTaskId != null)
                        {
                            configs[currentTaskId] = currentConfig;
                            currentConfig = null;
                            currentTaskId = null;
                        }
                    }
                }
            }
            catch
            {
                // 解析失败时使用空配置
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

            var json = new System.Text.StringBuilder();
            json.AppendLine("[");
            
            var i = 0;
            foreach (var config in configs.Values)
            {
                if (i > 0) json.AppendLine(",");
                json.AppendLine("  {");
                json.AppendLine(string.Format("    \"TaskId\": \"{0}\",", EscapeJson(config.TaskId)));
                json.AppendLine(string.Format("    \"TaskName\": \"{0}\",", EscapeJson(config.TaskName ?? "")));
                json.AppendLine(string.Format("    \"SourceConnection\": \"{0}\",", EscapeJson(config.SourceConnection ?? "")));
                json.AppendLine(string.Format("    \"TargetConnection\": \"{0}\",", EscapeJson(config.TargetConnection ?? "")));
                json.AppendLine(string.Format("    \"IntermediateConnection\": \"{0}\",", EscapeJson(config.IntermediateConnection ?? "")));
                json.Append("  }");
                i++;
            }
            
            json.AppendLine();
            json.AppendLine("]");
            
            File.WriteAllText(configPath, json.ToString());
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

        private static string ExtractStringValue(string line)
        {
            var start = line.IndexOf('"', line.IndexOf(':') + 1);
            if (start < 0) return "";
            var end = line.IndexOf('"', start + 1);
            return end > start ? line.Substring(start + 1, end - start - 1) : "";
        }

        private static DateTime? ExtractDateTimeValue(string line)
        {
            var value = ExtractStringValue(line);
            if (string.IsNullOrEmpty(value)) return null;
            DateTime result;
            return DateTime.TryParse(value, out result) ? (DateTime?)result : null;
        }

        private static int ExtractIntValue(string line)
        {
            var start = line.IndexOf(':') + 1;
            while (start < line.Length && (line[start] == ' ' || line[start] == ','))
                start++;
            var end = start;
            while (end < line.Length && char.IsDigit(line[end]))
                end++;
            int result;
            return int.TryParse(line.Substring(start, end - start), out result) ? result : 3;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\r", "\\r")
                       .Replace("\n", "\\n");
        }
    }
}
