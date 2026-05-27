using System;
using System.Collections.Generic;
using System.Linq;

namespace FastData.Sharding.Strategies
{
    /// <summary>
    /// 列表分表策略
    /// 适用场景：状态、类型等有限枚举值
    /// </summary>
    public class ListShardingStrategy : IShardingStrategy
    {
        public string Name => "ListSharding";
        public ShardingType Type => ShardingType.List;

        /// <summary>
        /// 根据实体获取分表名称
        /// </summary>
        public string GetTableName(ShardingConfig config, object entity)
        {
            if (config.ListConfig == null)
                throw new InvalidOperationException("列表分表配置不能为空");

            var listField = config.ListConfig.ListField;
            var entityType = entity.GetType();
            var property = entityType.GetProperty(listField);

            if (property == null)
                throw new InvalidOperationException($"实体类型 {entityType.Name} 中找不到字段 {listField}");

            var fieldValue = property.GetValue(entity)?.ToString();
            if (string.IsNullOrEmpty(fieldValue))
                throw new InvalidOperationException($"字段 {listField} 的值不能为空");

            return GetTableNameByValue(config, fieldValue);
        }

        /// <summary>
        /// 根据查询条件获取所有可能的分表名称
        /// </summary>
        public List<string> GetTableNames(ShardingConfig config, Dictionary<string, object> queryParams)
        {
            var result = new List<string>();

            if (config.ListConfig == null)
                return result;

            var listField = config.ListConfig.ListField;

            // 如果查询条件包含列表字段，直接定位到分表
            if (queryParams.ContainsKey(listField))
            {
                var fieldValue = queryParams[listField]?.ToString();
                if (!string.IsNullOrEmpty(fieldValue))
                {
                    // 支持 IN 查询（多个值）
                    if (fieldValue.Contains(","))
                    {
                        var values = fieldValue.Split(',');
                        foreach (var value in values)
                        {
                            var tableName = GetTableNameByValue(config, value.Trim());
                            if (!result.Contains(tableName))
                                result.Add(tableName);
                        }
                    }
                    else
                    {
                        result.Add(GetTableNameByValue(config, fieldValue));
                    }
                    return result;
                }
            }

            // 没有列表字段条件，返回所有分表
            return GetAllTableNames(config);
        }

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        public List<string> GetAllTableNames(ShardingConfig config)
        {
            var result = new List<string>();

            if (config.ListConfig?.ValueMapping == null)
                return result;

            foreach (var mapping in config.ListConfig.ValueMapping)
            {
                var tableName = $"{config.BaseTableName}_{mapping.Value}";
                if (!result.Contains(tableName))
                    result.Add(tableName);
            }

            return result;
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        public bool CreateTable(ShardingConfig config, string tableName)
        {
            return true;
        }

        #region 私有方法

        private string GetTableNameByValue(ShardingConfig config, string value)
        {
            if (config.ListConfig.ValueMapping.TryGetValue(value, out var suffix))
            {
                return $"{config.BaseTableName}_{suffix}";
            }

            // 如果没有映射，使用值本身作为后缀
            return $"{config.BaseTableName}_{value}";
        }

        #endregion
    }
}
