using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using FastUntility.Page;
using FastUntility.Base;
using FastData.Base;
using FastData.Model;
using FastData.DbTypes;
using FastData.Config;
using System.Linq.Expressions;
using System.Data;
using FastData.Property;
using FastData.Aop;
using FastData.Core.Base;

namespace FastData.Context
{
    public partial class DataContext : IDisposable
    {
        #region 获取列表
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        public DataReturn<T> GetList<T>(DataQuery item) where T : class,new()
        {
            var param = new List<DbParameter>();
            var result = new DataReturn<T>();
            var sql = new StringBuilder();
            object data;

            try
            {
                // SQL Server TOP 特殊处理
                if (item.Config.DbType == DataDbType.SqlServer && item.Take != 0)
                    sql.AppendFormat("select top {2} {0} from {1}", string.Join(",", item.Field), item.Table[0], item.Take);
                
                BuildBaseSelectQuery(item, param, sql);

                result.Sql = ParameterToSql.ObjectParamToSql(param, sql.ToString(), item.Config);

                Dispose(cmd);

                if (param.Count != 0)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(item.Table, sql.ToString(), param, config, true,AopType.Query_List_Lambda);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql.ToString());

                if (item.Take == 1)
                {
                    result.Item = BaseDataReader.ToList<T>(dr, item.Config, item.AsName).FirstOrDefault<T>() ?? new T();
                    data = result.Item;
                }
                else
                {
                    result.List = BaseDataReader.ToList<T>(dr, item.Config, item.AsName);
                    data = result.List;
                }

                dr.Close();
                dr.Dispose();

                AopAfter(item.Table, sql.ToString(), param, config, true, AopType.Query_List_Lambda, data);

                return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "to List tableName:" + typeof(T).Name,config, AopType.Query_List_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "GetList<T>", "");
                else
                    DbLog.LogException<T>(item.Config.IsOutError, item.Config.DbType, ex, "GetList<T>", result.Sql);
                return result;
            }
        }
        #endregion

        #region 获取分页
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <returns></returns>
        public DataReturn<T> GetPage<T>(DataQuery item, PageModel pModel) where T : class,new()
        {
            var param = new List<DbParameter>();
            var result = new DataReturn<T>();
            var sql = "";

            try
            {
                pModel.StarId = (pModel.PageId - 1) * pModel.PageSize + 1;
                pModel.EndId = pModel.PageId * pModel.PageSize;
                Dispose(cmd);
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                pModel.TotalRecord = BaseExecute.ToPageCount(item, cmd, ref sql);

                if (pModel.TotalRecord > 0)
                {
                    if ((pModel.TotalRecord % pModel.PageSize) == 0)
                        pModel.TotalPage = pModel.TotalRecord / pModel.PageSize;
                    else
                        pModel.TotalPage = (pModel.TotalRecord / pModel.PageSize) + 1;

                    if (pModel.PageId > pModel.TotalPage)
                        pModel.PageId = pModel.TotalPage;

                    AopBefore(item.Table, sql.ToString(), param, config, true,AopType.Query_Page_Lambda_Model);

                    Dispose(cmd);
                    var dr = BaseExecute.ToPageDataReader(item, cmd, pModel, ref sql);
                    result.PageResult.list = BaseDataReader.ToList<T>(dr, item.Config, item.AsName);
                    result.Sql = sql;

                    AopAfter(item.Table, sql.ToString(), param, config, true, AopType.Query_Page_Lambda_Model, result.PageResult.list);

                    dr.Close();
                    dr.Dispose();
                }
                else
                    result.PageResult.list = new List<T>();

                result.PageResult.pModel = pModel;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Page tableName:" + typeof(T).Name,config, AopType.Query_Page_Lambda_Model);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "GetPage<T>", "");
                else
                    DbLog.LogException<T>(item.Config.IsOutError, item.Config.DbType, ex, "GetPage<T>", result.Sql);
            }

            return result;
        }
        #endregion

        #region 获取分页
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <returns>数据返回对象</returns>
        public DataReturn GetPage(DataQuery item, PageModel pModel)
        {
            var param = new List<DbParameter>();
            var result = new DataReturn();
            var sql = "";

            try
            {
                pModel.StarId = (pModel.PageId - 1) * pModel.PageSize + 1;
                pModel.EndId = pModel.PageId * pModel.PageSize;
                Dispose(cmd);
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                pModel.TotalRecord = BaseExecute.ToPageCount(item, cmd, ref sql);

                if (pModel.TotalRecord > 0)
                {
                    if ((pModel.TotalRecord % pModel.PageSize) == 0)
                        pModel.TotalPage = pModel.TotalRecord / pModel.PageSize;
                    else
                        pModel.TotalPage = (pModel.TotalRecord / pModel.PageSize) + 1;

                    if (pModel.PageId > pModel.TotalPage)
                        pModel.PageId = pModel.TotalPage;

                    AopBefore(item.Table, sql.ToString(), param, config, true,AopType.Query_Page_Lambda_Dic);

                    Dispose(cmd);
                    var dr = BaseExecute.ToPageDataReader(item, cmd, pModel, ref sql);
                    result.PageResult.list = BaseJson.DataReaderToDic(dr, config.DbType == DataDbType.Oracle);
                    result.Sql = sql;

                    dr.Close();
                    dr.Dispose();

                    AopAfter(item.Table, sql.ToString(), param, config, true, AopType.Query_Page_Lambda_Dic, result.PageResult.list);
                }
                else
                    result.PageResult.list = new List<Dictionary<string, object>>();

                result.PageResult.pModel = pModel;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Page",config, AopType.Query_Page_Lambda_Dic);

                if (item.Config.SqlErrorType.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(item.Config, ex, "GetPage", result.Sql);
                else
                    DbLog.LogException(item.Config.IsOutError, item.Config.DbType, ex, "GetPage", result.Sql);
            }

            return result;
        }
        #endregion

        #region 获取分页sql
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="pModel">分页模型</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="isAop">是否启用AOP</param>
        /// <returns>数据返回对象</returns>
        public DataReturn GetPageSql(PageModel pModel, string sql, DbParameter[] param,bool isAop=true)
        {
            var result = new DataReturn();
            var countSql = "";
            var pageSql = "";

            try
            {
                pModel.StarId = (pModel.PageId - 1) * pModel.PageSize + 1;
                pModel.EndId = pModel.PageId * pModel.PageSize;
                Dispose(cmd);
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                pModel.TotalRecord = BaseExecute.ToPageCountSql(param, cmd, sql, config, ref countSql);

                if (pModel.TotalRecord > 0)
                {
                    if ((pModel.TotalRecord % pModel.PageSize) == 0)
                        pModel.TotalPage = pModel.TotalRecord / pModel.PageSize;
                    else
                        pModel.TotalPage = (pModel.TotalRecord / pModel.PageSize) + 1;

                    if (pModel.PageId > pModel.TotalPage)
                        pModel.PageId = pModel.TotalPage;

                    if (isAop)
                        AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Query_Page_Lambda_Dic);

                    Dispose(cmd);
                    var dr = BaseExecute.ToPageDataReaderSql(param, cmd, pModel, sql, config, ref pageSql);

                    result.PageResult.list = BaseJson.DataReaderToDic(dr, config.DbType == DataDbType.Oracle);
                    result.Sql = string.Format("count:{0},page:{1}", countSql, pageSql);

                    dr.Close();
                    dr.Dispose();

                    if(isAop)
                        AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Query_Page_Lambda_Dic, result.PageResult.list);
                }
                else
                    result.PageResult.list = new List<Dictionary<string, object>>();

                result.PageResult.pModel = pModel;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Page sql",config, AopType.Query_Page_Lambda_Dic);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetPageSql", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "GetPageSql", result.Sql);
            }

            return result;
        }
        #endregion
        
        #region 获取分页sql
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <returns></returns>
        public DataReturn<T> GetPageSql<T>(PageModel pModel, string sql, DbParameter[] param,bool isAop=true) where T : class, new()
        {
            var result = new DataReturn<T>();
            var countSql = "";
            var pageSql = "";

            try
            {
                pModel.StarId = (pModel.PageId - 1) * pModel.PageSize + 1;
                pModel.EndId = pModel.PageId * pModel.PageSize;
                Dispose(cmd);
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                pModel.TotalRecord = BaseExecute.ToPageCountSql(param, cmd, sql, config, ref countSql);

                if (pModel.TotalRecord > 0)
                {
                    if ((pModel.TotalRecord % pModel.PageSize) == 0)
                        pModel.TotalPage = pModel.TotalRecord / pModel.PageSize;
                    else
                        pModel.TotalPage = (pModel.TotalRecord / pModel.PageSize) + 1;

                    if (pModel.PageId > pModel.TotalPage)
                        pModel.PageId = pModel.TotalPage;

                    if (isAop)
                        AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Query_Page_Lambda_Model);

                    Dispose(cmd);
                    var dr = BaseExecute.ToPageDataReaderSql(param, cmd, pModel, sql, config, ref pageSql);

                    result.PageResult.list = BaseDataReader.ToList<T>(dr, config, null);
                    result.Sql = string.Format("count:{0},page:{1}", countSql, pageSql);

                    dr.Close();
                    dr.Dispose();

                    if (isAop)
                        AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Query_Page_Lambda_Model, result.PageResult.list);
                }

                result.PageResult.pModel = pModel;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Page tableName:" + typeof(T).Name,config, AopType.Query_Page_Lambda_Model);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetPageSql", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "GetPageSql", result.Sql);
            }

            return result;
        }
        #endregion

        #region 获取json
        /// <summary>
        /// 获取json多表
        /// </summary>
        /// <returns></returns>
        public DataReturn GetJson(DataQuery item)
        {
            var param = new List<DbParameter>();
            var result = new DataReturn();
            var sql = new StringBuilder();

            try
            {
                // SQL Server TOP 特殊处理
                if (item.Config.DbType == DataDbType.SqlServer && item.Take != 0)
                    sql.AppendFormat("select top {2} {0} from {1}", string.Join(",", item.Field), item.Table[0], item.Take);
                
                BuildBaseSelectQuery(item, param, sql);

                if (item.Predicate[0].Param.Count != 0)
                    param.AddRange(item.Predicate[0].Param);

                result.Sql = ParameterToSql.ObjectParamToSql(param, sql.ToString(), item.Config);

                Dispose(cmd);

                if (param.Count != 0)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Query_Json_Lambda);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql.ToString());

                result.Json = BaseJson.DataReaderToJson(dr, config.DbType == DataDbType.Oracle);

                dr.Close();
                dr.Dispose();

                AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Query_Json_Lambda, result.Json);

                return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Json",config, AopType.Query_Json_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetJson", result.Sql);
                else
                    DbLog.LogException(item.Config.IsOutError, item.Config.DbType, ex, "GetJson", result.Sql);
                return result;
            }
        }
        #endregion

        #region 获取条数
        /// <summary>
        /// 获取条数
        /// </summary>
        /// <returns></returns>
        public DataReturn GetCount(DataQuery item)
        {
            var sql = new StringBuilder();
            var result = new DataReturn();
            var param = new List<DbParameter>();

            try
            {
                sql.AppendFormat("select count(0) from {0}", item.Table[0]);

                for (var i = 1; i < item.Predicate.Count; i++)
                {
                    sql.AppendFormat(" {0} on {1}", item.Table[i], item.Predicate[i].Where);

                    if (item.Predicate[i].Param.Count != 0)
                        param.AddRange(item.Predicate[i].Param);
                }

                // 使用 WhereBuilder 构建完整的 WHERE 子句（支持链式条件）
                if (WhereBuilder.HasWhereClause(item))
                {
                    var whereClause = WhereBuilder.BuildWhereClause(item, ref param);
                    sql.AppendFormat(" where {0}", whereClause);
                }

                if (item.GroupBy.Count > 0)
                    sql.AppendFormat(" group by {0}", string.Join(",", item.GroupBy));

                if (item.OrderBy.Count > 0)
                    sql.AppendFormat(" order by {0}", string.Join(",", item.OrderBy));

                result.Sql = ParameterToSql.ObjectParamToSql(param, sql.ToString(), item.Config);

                Dispose(cmd);

                if (param.Count != 0)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Query_Json_Lambda);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dt = BaseExecute.ToDataTable(cmd, sql.ToString());

                if (dt.Rows.Count > 0)
                    result.Count = dt.Rows[0][0].ToString().ToInt(0);
                else
                    result.Count = 0;

                AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Query_Json_Lambda, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Count",config, AopType.Query_Json_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetCount", result.Sql);
                else
                    DbLog.LogException(item.Config.IsOutError, item.Config.DbType, ex, "GetCount", result.Sql);
                return result;
            }
        }
        #endregion

        #region 执行sql
        /// <summary>
        /// 执行sql
        /// </summary>
        /// <returns></returns>
        public DataReturn<T> ExecuteSql<T>(string sql, DbParameter[] param=null) where T : class,new()
        {
            var result = new DataReturn<T>();
            try
            {
                if (param != null)
                    result.Sql = ParameterToSql.ObjectParamToSql(param.ToList(), sql, config);
                else
                    result.Sql = sql;

                Dispose(cmd);

                if (param != null)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Execute_Sql_Model);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql);

                result.List = BaseDataReader.ToList<T>(dr, config);

                dr.Close();
                dr.Dispose();

                AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Execute_Sql_Model, result.List);
            }
            catch (Exception ex)
            {
                AopException(ex, "ExecuteSql tableName:" + typeof(T).Name,config, AopType.Execute_Sql_Model);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "ExecuteSql<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "ExecuteSql<T>", result.Sql);
            }

            return result;
        }
        #endregion
        
        #region 执行sql
        /// <summary>
        /// 执行sql
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="isLog">是否记录日志</param>
        /// <param name="isAop">是否启用AOP</param>
        /// <returns>数据返回对象</returns>
        public DataReturn ExecuteSqlList(string sql, DbParameter[] param=null,bool isLog=false,bool isAop=true)
        {
            var result = new DataReturn();
            try
            {
                if (param != null)
                    result.Sql = ParameterToSql.ObjectParamToSql(param.ToList(), sql, config);
                else
                    result.Sql = sql;

                DbLog.LogSql(isLog, result.Sql, config.DbType, 0);

                Dispose(cmd);

                if (param != null)
                    cmd.Parameters.AddRange(param.ToArray());

                if(isAop)
                    AopBefore(null, sql.ToString(), param?.ToList(), config, true,AopType.Execute_Sql_Dic);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql);

                result.DicList = BaseJson.DataReaderToDic(dr, config.DbType == DataDbType.Oracle);
                result.WriteReturn.IsSuccess = true;

                dr.Close();
                dr.Dispose();

                if (isAop)
                    AopAfter(null, sql.ToString(), param?.ToList(), config, true, AopType.Execute_Sql_Dic, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Execute Sql",config, AopType.Execute_Sql_Dic);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "ExecuteSqlList", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "ExecuteSqlList", result.Sql);
            }

            return result;
        }
        #endregion

        #region 获取dic
        /// <summary>
        /// 获取dic
        /// </summary>
        /// <returns></returns>
        public DataReturn GetDic(DataQuery item)
        {
            var param = new List<DbParameter>();
            var result = new DataReturn();
            var sql = new StringBuilder();
            object data;

            try
            {
                // SQL Server TOP 特殊处理
                if (item.Config.DbType == DataDbType.SqlServer && item.Take != 0)
                    sql.AppendFormat("select top {2} {0} from {1}", string.Join(",", item.Field), item.Table[0], item.Take);
                
                BuildBaseSelectQuery(item, param, sql);

                result.Sql = ParameterToSql.ObjectParamToSql(param, sql.ToString(), item.Config);

                Dispose(cmd);

                if (param.Count != 0)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(item.Table, sql.ToString(), param, config, true,AopType.Query_Dic_Lambda);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql.ToString());

                if (item.Take == 1)
                {
                    result.Dic = BaseJson.DataReaderToDic(dr, config.DbType == DataDbType.Oracle).FirstOrDefault() ?? new Dictionary<string, object>();
                    data = result.Dic;
                }
                else
                {
                    result.DicList = BaseJson.DataReaderToDic(dr, config.DbType == DataDbType.Oracle);
                    data = result.DicList;
                }

                dr.Close();
                dr.Dispose();

                AopAfter(item.Table, sql.ToString(), param, config, true, AopType.Query_Dic_Lambda, data);

                return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "to Dic",config, AopType.Query_Dic_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetDic", result.Sql);
                else
                    DbLog.LogException(item.Config.IsOutError, item.Config.DbType, ex, "GetDic", result.Sql);
                return result;
            }
        }
        #endregion

        #region 获取DataTable
        /// <summary>
        /// 获取DataTable
        /// </summary>
        /// <returns></returns>
        public DataReturn GetDataTable(DataQuery item)
        {
            var param = new List<DbParameter>();
            var result = new DataReturn();
            var sql = new StringBuilder();

            try
            {
                // SQL Server TOP 特殊处理
                if (item.Config.DbType == DataDbType.SqlServer && item.Take != 0)
                    sql.AppendFormat("select top {2} {0} from {1}", string.Join(",", item.Field), item.Table[0], item.Take);
                
                BuildBaseSelectQuery(item, param, sql);

                result.Sql = ParameterToSql.ObjectParamToSql(param, sql.ToString(), item.Config);

                Dispose(cmd);

                if (param.Count != 0)
                    cmd.Parameters.AddRange(param.ToArray());

                AopBefore(item.Table, sql.ToString(), param, config, true,AopType.Query_DataTable_Lambda);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                var dr = BaseExecute.ToDataReader(cmd, sql.ToString());

                result.Table.Load(dr);

                dr.Close();
                dr.Dispose();

                AopAfter(item.Table, sql.ToString(), param, config, true, AopType.Query_DataTable_Lambda, result.Table);

                return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "to DataTable",config, AopType.Query_DataTable_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GetDataTable", result.Sql);
                else
                    DbLog.LogException(item.Config.IsOutError, item.Config.DbType, ex, "GetDataTable", result.Sql);
                return result;
            }
        }
        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 构建基础 SELECT + JOIN + WHERE + GROUP BY + ORDER BY 查询
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="param">数据库参数列表</param>
        /// <param name="sql">SQL构建器</param>
        private void BuildBaseSelectQuery(DataQuery item, List<DbParameter> param, StringBuilder sql)
        {
            // SELECT 子句
            sql.AppendFormat("select {0} from {1}", string.Join(",", item.Field), item.Table[0]);

            // JOIN 子句
            for (var i = 1; i < item.Predicate.Count; i++)
            {
                sql.AppendFormat(" {0} on {1}", item.Table[i], item.Predicate[i].Where);
                if (item.Predicate[i].Param.Count != 0)
                    param.AddRange(item.Predicate[i].Param);
            }

            // WHERE 子句
            var whereClause = WhereBuilder.BuildWhereClause(item, ref param);
            if (!string.IsNullOrEmpty(whereClause))
                sql.AppendFormat(" where {0}", whereClause);

            // TAKE 子句（数据库特定语法）
            AppendTakeClause(item, sql);

            // GROUP BY 子句
            if (item.GroupBy.Count > 0)
                sql.AppendFormat(" group by {0}", string.Join(",", item.GroupBy));

            // ORDER BY 子句
            if (item.OrderBy.Count > 0)
                sql.AppendFormat(" order by {0}", string.Join(",", item.OrderBy));
        }

        /// <summary>
        /// 追加 TAKE/LIMIT 子句（数据库特定语法）
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="sql">SQL构建器</param>
        private void AppendTakeClause(DataQuery item, StringBuilder sql)
        {
            if (item.Take == 0)
                return;

            if (item.Config.DbType == DataDbType.SqlServer)
            {
                // SQL Server 的 TOP 已在主查询中处理
            }
            else if (item.Config.DbType == DataDbType.Oracle)
            {
                sql.AppendFormat(" and rownum <={0}", item.Take);
            }
            else if (item.Config.DbType == DataDbType.DB2)
            {
                sql.AppendFormat(" and fetch first {0} rows only", item.Take);
            }
            else if (item.Config.DbType == DataDbType.MySql || 
                     item.Config.DbType == DataDbType.PostgreSql || 
                     item.Config.DbType == DataDbType.SQLite)
            {
                sql.AppendFormat(" limit {0}", item.Take);
            }
        }

        #endregion
    }
}
