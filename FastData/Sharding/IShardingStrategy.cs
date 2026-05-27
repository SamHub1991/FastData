using System;
using System.Collections.Generic;

namespace FastData.Sharding
{
    /// <summary>
    /// 分表策略接口
    /// </summary>
    public interface IShardingStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 策略类型
        /// </summary>
        ShardingType Type { get; }

        /// <summary>
        /// 根据实体获取分表名称
        /// </summary>
        /// <param name="config">分表配置</param>
        /// <param name="entity">实体对象</param>
        /// <returns>分表名称</returns>
        string GetTableName(ShardingConfig config, object entity);

        /// <summary>
        /// 根据查询条件获取所有可能的分表名称
        /// </summary>
        /// <param name="config">分表配置</param>
        /// <param name="queryParams">查询参数</param>
        /// <returns>分表名称列表</returns>
        List<string> GetTableNames(ShardingConfig config, Dictionary<string, object> queryParams);

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        /// <param name="config">分表配置</param>
        /// <returns>所有分表名称</returns>
        List<string> GetAllTableNames(ShardingConfig config);

        /// <summary>
        /// 创建分表
        /// </summary>
        /// <param name="config">分表配置</param>
        /// <param name="tableName">分表名称</param>
        /// <returns>是否成功</returns>
        bool CreateTable(ShardingConfig config, string tableName);
    }

    /// <summary>
    /// 分表类型
    /// </summary>
    public enum ShardingType
    {
        /// <summary>
        /// 时间分表（日志、流水、操作记录）
        /// </summary>
        Time = 1,

        /// <summary>
        /// 哈希分表（业务唯一键）
        /// </summary>
        Hash = 2,

        /// <summary>
        /// 列表分表（状态、类型枚举）
        /// </summary>
        List = 3,

        /// <summary>
        /// 组合键分表（多字段组合唯一标识）
        /// </summary>
        Composite = 4,

        /// <summary>
        /// 地理位置分表
        /// </summary>
        Geo = 5
    }

    /// <summary>
    /// 时间分表粒度
    /// </summary>
    public enum TimeGranularity
    {
        /// <summary>
        /// 按天分表
        /// </summary>
        Day = 1,

        /// <summary>
        /// 按周分表
        /// </summary>
        Week = 2,

        /// <summary>
        /// 按月分表
        /// </summary>
        Month = 3,

        /// <summary>
        /// 按季度分表
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// 按年分表
        /// </summary>
        Year = 5
    }

    /// <summary>
    /// 哈希分表算法
    /// </summary>
    public enum HashAlgorithm
    {
        /// <summary>
        /// 取模哈希
        /// </summary>
        Modulo = 1,

        /// <summary>
        /// 一致性哈希
        /// </summary>
        Consistent = 2,

        /// <summary>
        /// CRC32 哈希
        /// </summary>
        Crc32 = 3
    }
}
