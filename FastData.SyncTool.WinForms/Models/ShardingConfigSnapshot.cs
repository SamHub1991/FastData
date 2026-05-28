using System;
using System.Collections.Generic;
using System.Linq;
using FastData.Sharding;
using ShardingTypeEnum = FastData.Sharding.ShardingType;

namespace FastData.SyncTool.WinForms.Models
{
    /// <summary>
    /// 分表配置快照
    /// 用于在开发环境和同步工具之间保持一致性
    /// </summary>
    public class ShardingConfigSnapshot
    {
        /// <summary>
        /// 快照ID
        /// </summary>
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 快照名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 来源环境（开发/测试/生产）
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// 分表配置列表
        /// </summary>
        public List<ShardingConfigItem> Configs { get; set; } = new List<ShardingConfigItem>();

        /// <summary>
        /// 从 ShardingManager 导入当前配置
        /// </summary>
        public static ShardingConfigSnapshot FromCurrentConfig(string environment = "开发环境")
        {
            var snapshot = new ShardingConfigSnapshot
            {
                Name = $"配置快照 - {DateTime.Now:yyyyMMddHHmmss}",
                Description = $"从 {environment} 导入",
                Environment = environment
            };

            // 这里需要从 ShardingManager 获取所有已注册的配置
            // 由于 ShardingManager 的设计，我们需要遍历实体类型
            return snapshot;
        }

        /// <summary>
        /// 验证与目标快照的一致性
        /// </summary>
        public ValidationResult Validate(ShardingConfigSnapshot target)
        {
            var result = new ValidationResult();

            // 检查配置数量
            if (Configs.Count != target.Configs.Count)
            {
                result.AddWarning($"配置数量不一致: 当前 {Configs.Count} 个, 目标 {target.Configs.Count} 个");
            }

            // 逐个比较配置
            foreach (var currentConfig in Configs)
            {
                var targetConfig = target.Configs.FirstOrDefault(c => c.BaseTableName == currentConfig.BaseTableName);

                if (targetConfig == null)
                {
                    result.AddWarning($"表 {currentConfig.BaseTableName} 在目标配置中不存在");
                    continue;
                }

                // 比较分表类型
                if (currentConfig.ShardingType != targetConfig.ShardingType)
                {
                    result.AddError($"表 {currentConfig.BaseTableName} 分表类型不一致: 当前 {currentConfig.ShardingType}, 目标 {targetConfig.ShardingType}");
                }

                // 比较详细配置
                var configErrors = CompareConfigDetails(currentConfig, targetConfig);
                result.Errors.AddRange(configErrors);
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private List<string> CompareConfigDetails(ShardingConfigItem current, ShardingConfigItem target)
        {
            var errors = new List<string>();

            switch (current.ShardingType)
            {
                case "Time":
                    if (current.TimeField != target.TimeField)
                        errors.Add($"表 {current.BaseTableName} 时间字段不一致: {current.TimeField} vs {target.TimeField}");
                    if (current.TimeGranularity != target.TimeGranularity)
                        errors.Add($"表 {current.BaseTableName} 时间粒度不一致: {current.TimeGranularity} vs {target.TimeGranularity}");
                    break;

                case "Hash":
                    if (current.HashField != target.HashField)
                        errors.Add($"表 {current.BaseTableName} 哈希字段不一致: {current.HashField} vs {target.HashField}");
                    if (current.ShardCount != target.ShardCount)
                        errors.Add($"表 {current.BaseTableName} 分表数量不一致: {current.ShardCount} vs {target.ShardCount}");
                    break;

                case "List":
                    if (current.ListField != target.ListField)
                        errors.Add($"表 {current.BaseTableName} 列表字段不一致: {current.ListField} vs {target.ListField}");
                    break;

                case "Composite":
                    if (current.CompositeFields != target.CompositeFields)
                        errors.Add($"表 {current.BaseTableName} 组合字段不一致: {current.CompositeFields} vs {target.CompositeFields}");
                    break;

                case "QueryFrequency":
                    if (current.FrequencyField != target.FrequencyField)
                        errors.Add($"表 {current.BaseTableName} 频率字段不一致: {current.FrequencyField} vs {target.FrequencyField}");
                    break;
            }

            return errors;
        }
    }

    /// <summary>
    /// 分表配置项
    /// </summary>
    public class ShardingConfigItem
    {
        /// <summary>
        /// 基础表名
        /// </summary>
        public string BaseTableName { get; set; }

        /// <summary>
        /// 分表类型
        /// </summary>
        public string ShardingType { get; set; }

        // 时间分表配置
        public string TimeField { get; set; }
        public string TimeGranularity { get; set; }
        public DateTime? StartTime { get; set; }

        // 哈希分表配置
        public string HashField { get; set; }
        public int ShardCount { get; set; }
        public string HashAlgorithm { get; set; }

        // 列表分表配置
        public string ListField { get; set; }
        public Dictionary<string, string> ValueMapping { get; set; }

        // 组合键分表配置
        public string CompositeFields { get; set; }
        public bool UseHash { get; set; }

        // 查询频率分表配置
        public string FrequencyField { get; set; }
        public long HotThreshold { get; set; }
        public string HotSuffix { get; set; }
        public string ColdSuffix { get; set; }

        /// <summary>
        /// 转换为 ShardingConfig
        /// </summary>
        public ShardingConfig ToShardingConfig()
        {
            var config = new ShardingConfig
            {
                BaseTableName = BaseTableName
            };

            switch (ShardingType)
            {
                case "Time":
                    config.ShardingType = ShardingTypeEnum.Time;
                    config.TimeConfig = new TimeShardingConfig
                    {
                        TimeField = TimeField,
                        Granularity = Enum.Parse<TimeGranularity>(TimeGranularity),
                        StartTime = StartTime ?? new DateTime(2025, 1, 1)
                    };
                    break;

                case "Hash":
                    config.ShardingType = ShardingTypeEnum.Hash;
                    config.HashConfig = new HashShardingConfig
                    {
                        HashField = HashField,
                        ShardCount = ShardCount,
                        Algorithm = Enum.Parse<HashAlgorithm>(HashAlgorithm ?? "Modulo")
                    };
                    break;

                case "List":
                    config.ShardingType = ShardingTypeEnum.List;
                    config.ListConfig = new ListShardingConfig
                    {
                        ListField = ListField,
                        ValueMapping = ValueMapping ?? new Dictionary<string, string>()
                    };
                    break;

                case "Composite":
                    config.ShardingType = ShardingTypeEnum.Composite;
                    config.CompositeConfig = new CompositeShardingConfig
                    {
                        CompositeFields = CompositeFields?.Split(',').Select(f => f.Trim()).ToList() ?? new List<string>(),
                        UseHash = UseHash,
                        ShardCount = ShardCount
                    };
                    break;

                case "QueryFrequency":
                    config.ShardingType = ShardingTypeEnum.QueryFrequency;
                    config.FrequencyConfig = new QueryFrequencyShardingConfig
                    {
                        Field = FrequencyField,
                        HotThreshold = HotThreshold,
                        HotSuffix = HotSuffix ?? "_hot",
                        ColdSuffix = ColdSuffix ?? "_cold",
                        ColdShardingType = ColdShardingType.ByHash,
                        ColdShardCount = 4
                    };
                    break;
            }

            return config;
        }

        /// <summary>
        /// 从 ShardingConfig 创建
        /// </summary>
        public static ShardingConfigItem FromShardingConfig(ShardingConfig config)
        {
            var item = new ShardingConfigItem
            {
                BaseTableName = config.BaseTableName,
                ShardingType = config.ShardingType.ToString()
            };

            switch (config.ShardingType)
            {
                case ShardingTypeEnum.Time:
                    item.TimeField = config.TimeConfig?.TimeField;
                    item.TimeGranularity = config.TimeConfig?.Granularity.ToString();
                    item.StartTime = config.TimeConfig?.StartTime;
                    break;

                case ShardingTypeEnum.Hash:
                    item.HashField = config.HashConfig?.HashField;
                    item.ShardCount = config.HashConfig?.ShardCount ?? 0;
                    item.HashAlgorithm = config.HashConfig?.Algorithm.ToString();
                    break;

                case ShardingTypeEnum.List:
                    item.ListField = config.ListConfig?.ListField;
                    item.ValueMapping = config.ListConfig?.ValueMapping;
                    break;

                case ShardingTypeEnum.Composite:
                    item.CompositeFields = string.Join(",", config.CompositeConfig?.CompositeFields ?? new List<string>());
                    item.UseHash = config.CompositeConfig?.UseHash ?? false;
                    item.ShardCount = config.CompositeConfig?.ShardCount ?? 0;
                    break;

                case ShardingTypeEnum.QueryFrequency:
                    item.FrequencyField = config.FrequencyConfig?.Field;
                    item.HotThreshold = config.FrequencyConfig?.HotThreshold ?? 0;
                    item.HotSuffix = config.FrequencyConfig?.HotSuffix;
                    item.ColdSuffix = config.FrequencyConfig?.ColdSuffix;
                    break;
            }

            return item;
        }

        /// <summary>
        /// 获取配置描述
        /// </summary>
        public string GetDescription()
        {
            switch (ShardingType)
            {
                case "Time":
                    return $"时间分表: {TimeField} ({TimeGranularity})";
                case "Hash":
                    return $"哈希分表: {HashField} ({ShardCount}个分表)";
                case "List":
                    return $"列表分表: {ListField}";
                case "Composite":
                    return $"组合分表: {CompositeFields}";
                case "QueryFrequency":
                    return $"频率分表: {FrequencyField}";
                default:
                    return ShardingType;
            }
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public void AddError(string message)
        {
            Errors.Add(message);
            IsValid = false;
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        public string GetSummary()
        {
            if (IsValid && Warnings.Count == 0)
                return "验证通过，配置完全一致";

            var summary = new List<string>();

            if (Errors.Count > 0)
                summary.Add($"错误 ({Errors.Count}): {string.Join("; ", Errors)}");

            if (Warnings.Count > 0)
                summary.Add($"警告 ({Warnings.Count}): {string.Join("; ", Warnings)}");

            return string.Join("\n", summary);
        }
    }
}
