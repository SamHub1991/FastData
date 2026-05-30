using System.Collections.Generic;

namespace FastData.Tooling.Database
{
    /// <summary>
    /// 数据库元数据读取器接口
    /// 
    /// 定义读取数据库表结构的方法，用于 CodeFirst 和代码生成。
    /// </summary>
    public interface IDatabaseMetadataReader
    {
        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        bool TestConnection();

        /// <summary>
        /// 获取所有表信息
        /// </summary>
        /// <returns>表列表</returns>
        IList<DatabaseTable> GetTables();

        /// <summary>
        /// 获取指定表的列信息
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>列列表</returns>
        IList<DatabaseColumn> GetColumns(string tableName);
    }
}
