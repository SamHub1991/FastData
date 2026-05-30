using System;
using System.Data.Common;
using FastData.Context;

namespace FastData.ConnectionPool
{
    /// <summary>
    /// 连接池扩展方法
    /// </summary>
    public static class ConnectionPoolExtensions
    {
        /// <summary>
        /// 创建带连接池的 DataContext
        /// </summary>
        /// <param name="dbKey">数据库键</param>
        /// <param name="config">连接池配置</param>
        /// <returns>数据上下文</returns>
        public static DataContext CreatePooledDataContext(string dbKey, ConnectionPoolConfig config = null)
        {
            return new DataContext(dbKey, poolConfig: config);
        }

        /// <summary>
        /// 获取连接池指标
        /// </summary>
        public static ConnectionPoolMetrics GetPoolMetrics(string dbKey)
        {
            var pool = ConnectionPoolFactory.Instance.GetPool(dbKey);
            return pool?.GetMetrics();
        }

        /// <summary>
        /// 获取所有连接池指标
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, ConnectionPoolMetrics> GetAllPoolMetrics()
        {
            return ConnectionPoolFactory.Instance.GetAllMetrics();
        }
    }
}
