using System.Collections.Generic;
using System.Data;
using FastUntility.Page;

namespace FastData.Model
{
    /// <summary>
    /// 返回操作数据结果
    /// </summary>
    public sealed class DataReturn<T> where T : class,new()
    {
        /// <summary>
        /// 条数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 实体
        /// </summary>
        public T Item { get; set; } = new T();

        /// <summary>
        /// 列表
        /// </summary>
        public List<T> List { get; set; } = new List<T>();

        /// <summary>
        /// sql
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// 分页
        /// </summary>
        public PageResult<T> PageResult { get; set; } = new PageResult<T>();

        /// <summary>
        /// 写返回结果
        /// </summary>
        public WriteReturn WriteReturn { get; set; } = new WriteReturn();
    }

     /// <summary>
    /// 返回操作数据结果
    /// </summary>
    public class DataReturn
    {
        /// <summary>
        /// 条数
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// sql
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// json
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        /// dic list
        /// </summary>
        public List<Dictionary<string, object>> DicList { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// dic item
        /// </summary>
        public Dictionary<string, object> Dic { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// data table
        /// </summary>
        public DataTable Table { get; set; } = new DataTable();

        /// <summary>
        /// 分页
        /// </summary>
        public PageResult PageResult { set; get; } = new PageResult();

        /// <summary>
        /// 写返回结果
        /// </summary>
        public WriteReturn WriteReturn { get; set; } = new WriteReturn();
    }

    /// <summary>
    /// 写返回结果
    /// </summary>
    public class WriteReturn
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 出错信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 自增主键值（仅 INSERT 操作，跨数据库兼容）
        /// SQL Server: SCOPE_IDENTITY()
        /// MySQL: LAST_INSERT_ID()
        /// SQLite: last_insert_rowid()
        /// PostgreSQL: lastval()
        /// </summary>
        public long IdentityValue { get; set; }
    }
}
