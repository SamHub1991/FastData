using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FastData.Sharding.Strategies
{
    /// <summary>
    /// 哈希分表策略
    /// 适用场景：有业务唯一键但无自增ID的表
    /// </summary>
    public class HashShardingStrategy : IShardingStrategy
    {
        public string Name => "HashSharding";
        public ShardingType Type => ShardingType.Hash;

        /// <summary>
        /// 根据实体获取分表名称
        /// </summary>
        /// <param name="config">分片配置</param>
        /// <param name="entity">实体对象</param>
        /// <returns>表名</returns>
        public string GetTableName(ShardingConfig config, object entity)
        {
            if (config.HashConfig == null)
                throw new InvalidOperationException("哈希分表配置不能为空");

            var hashField = config.HashConfig.HashField;
            var entityType = entity.GetType();
            var property = entityType.GetProperty(hashField);

            if (property == null)
                throw new InvalidOperationException($"实体类型 {entityType.Name} 中找不到字段 {hashField}");

            var hashValue = property.GetValue(entity)?.ToString();
            if (string.IsNullOrEmpty(hashValue))
                throw new InvalidOperationException($"字段 {hashField} 的值不能为空");

            var shardIndex = GetShardIndex(config, hashValue);
            return $"{config.BaseTableName}_{shardIndex:D4}";
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

            if (config.HashConfig == null)
                return result;

            var hashField = config.HashConfig.HashField;

            // 如果查询条件包含哈希字段，直接定位到分表
            if (queryParams.ContainsKey(hashField))
            {
                var hashValue = queryParams[hashField]?.ToString();
                if (!string.IsNullOrEmpty(hashValue))
                {
                    var shardIndex = GetShardIndex(config, hashValue);
                    result.Add($"{config.BaseTableName}_{shardIndex:D4}");
                    return result;
                }
            }

            // 没有哈希字段条件，返回所有分表
            return GetAllTableNames(config);
        }

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        public List<string> GetAllTableNames(ShardingConfig config)
        {
            var result = new List<string>();

            if (config.HashConfig == null)
                return result;

            for (int i = 0; i < config.HashConfig.ShardCount; i++)
            {
                result.Add($"{config.BaseTableName}_{i:D4}");
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

        private int GetShardIndex(ShardingConfig config, string hashValue)
        {
            var hash = ComputeHash(config, hashValue);
            return hash % config.HashConfig.ShardCount;
        }

        private int ComputeHash(ShardingConfig config, string value)
        {
            return config.HashConfig.Algorithm switch
            {
                HashAlgorithm.Modulo => ComputeModuloHash(value),
                HashAlgorithm.Consistent => ComputeConsistentHash(value),
                HashAlgorithm.Crc32 => ComputeCrc32Hash(value),
                _ => ComputeModuloHash(value)
            };
        }

        private int ComputeModuloHash(string value)
        {
            // 简单取模哈希
            int hash = 0;
            foreach (char c in value)
            {
                hash = (hash * 31 + c) & 0x7FFFFFFF;
            }
            return Math.Abs(hash);
        }

        private int ComputeConsistentHash(string value)
        {
            // 一致性哈希（简化版）
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(value);
                var hashBytes = md5.ComputeHash(inputBytes);
                return Math.Abs(BitConverter.ToInt32(hashBytes, 0));
            }
        }

        private int ComputeCrc32Hash(string value)
        {
            // CRC32 哈希
            var crc32 = new Crc32();
            var inputBytes = Encoding.UTF8.GetBytes(value);
            var hash = crc32.ComputeHash(inputBytes);
            return Math.Abs(BitConverter.ToInt32(hash, 0));
        }

        #endregion
    }

    /// <summary>
    /// CRC32 哈希算法实现
    /// </summary>
    internal class Crc32
    {
        private static readonly uint[] Table = new uint[256];
        private const uint Polynomial = 0xEDB88320;

        static Crc32()
        {
            for (uint i = 0; i < 256; i++)
            {
                var crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
                }
                Table[i] = crc;
            }
        }

        public byte[] ComputeHash(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            foreach (var b in bytes)
            {
                crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
            }
            crc ^= 0xFFFFFFFF;
            return BitConverter.GetBytes(crc);
        }
    }
}
