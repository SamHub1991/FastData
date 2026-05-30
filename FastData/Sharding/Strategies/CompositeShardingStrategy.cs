using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FastData.Sharding.Strategies
{
    /// <summary>
    /// 组合键分表策略
    /// 适用场景：多个字段组合唯一标识记录
    /// </summary>
    public class CompositeShardingStrategy : IShardingStrategy
    {
        public string Name => "CompositeSharding";
        public ShardingType Type => ShardingType.Composite;

        /// <summary>
        /// 根据实体获取分表名称
        /// </summary>
        /// <param name="config">分片配置</param>
        /// <param name="entity">实体对象</param>
        /// <returns>表名</returns>
        public string GetTableName(ShardingConfig config, object entity)
        {
            if (config.CompositeConfig == null)
                throw new InvalidOperationException("组合键分表配置不能为空");

            var compositeFields = config.CompositeConfig.CompositeFields;
            if (compositeFields == null || compositeFields.Count == 0)
                throw new InvalidOperationException("组合字段列表不能为空");

            var entityType = entity.GetType();
            var values = new List<string>();

            foreach (var field in compositeFields)
            {
                var property = entityType.GetProperty(field);
                if (property == null)
                    throw new InvalidOperationException($"实体类型 {entityType.Name} 中找不到字段 {field}");

                var value = property.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(value))
                    throw new InvalidOperationException($"字段 {field} 的值不能为空");

                values.Add(value);
            }

            return GetTableNameByValues(config, values);
        }

        /// <summary>
        /// 根据查询条件获取所有可能的分表名称
        /// </summary>
        /// <param name="config">分片配置</param>
        /// <param name="queryParams">查询参数</param>
        /// <returns>表名列表</returns>
        public List<string> GetTableNames(ShardingConfig config, Dictionary<string, object> queryParams)
        {
            var result = new List<string>();

            if (config.CompositeConfig == null)
                return result;

            var compositeFields = config.CompositeConfig.CompositeFields;
            var values = new List<string>();
            var allFieldsPresent = true;

            // 检查查询条件是否包含所有组合字段
            foreach (var field in compositeFields)
            {
                if (queryParams.ContainsKey(field) && queryParams[field] != null)
                {
                    values.Add(queryParams[field].ToString());
                }
                else
                {
                    allFieldsPresent = false;
                    break;
                }
            }

            // 如果包含所有组合字段，直接定位到分表
            if (allFieldsPresent && values.Count > 0)
            {
                result.Add(GetTableNameByValues(config, values));
                return result;
            }

            // 没有所有组合字段条件，返回所有分表
            return GetAllTableNames(config);
        }

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        public List<string> GetAllTableNames(ShardingConfig config)
        {
            var result = new List<string>();

            if (config.CompositeConfig == null)
                return result;

            if (config.CompositeConfig.UseHash)
            {
                // 使用哈希方式，返回所有分表
                for (int i = 0; i < config.CompositeConfig.ShardCount; i++)
                {
                    result.Add($"{config.BaseTableName}_{i:D4}");
                }
            }

            return result;
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        /// <param name="config">分片配置</param>
        /// <param name="tableName">表名</param>
        /// <returns>是否成功</returns>
        public bool CreateTable(ShardingConfig config, string tableName)
        {
            return true;
        }

        #region 私有方法

        private string GetTableNameByValues(ShardingConfig config, List<string> values)
        {
            var separator = config.CompositeConfig.Separator;
            var combinedValue = string.Join(separator, values);

            if (config.CompositeConfig.UseHash)
            {
                // 使用哈希方式
                var hash = ComputeHash(combinedValue);
                var shardIndex = hash % config.CompositeConfig.ShardCount;
                return $"{config.BaseTableName}_{shardIndex:D4}";
            }
            else
            {
                // 使用拼接方式
                return $"{config.BaseTableName}_{combinedValue}";
            }
        }

        private int ComputeHash(string value)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(value);
                var hashBytes = md5.ComputeHash(inputBytes);
                return Math.Abs(BitConverter.ToInt32(hashBytes, 0));
            }
        }

        #endregion
    }
}
