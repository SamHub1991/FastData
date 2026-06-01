using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Diagnostics;
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
        #region 删除(Lambda表达式)
        /// <summary>
        /// 删除(Lambda表达式)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <returns></returns>
        public DataReturn<T> Delete<T>(Expression<Func<T, bool>> predicate, bool isTrans = false) where T : class, new()
        {
            var result = new DataReturn<T>();
            var sql = new StringBuilder();
            var visitModel = new VisitModel();
            var tableName = new List<string>();

            try
            {
                if (isTrans)
                    BeginTrans();

                visitModel = VisitExpression.LambdaWhere<T>(predicate, config);

                sql.AppendFormat("delete from {0} {1}", TableNameHelper.GetTableName<T>()
                    , string.IsNullOrEmpty(visitModel.Where) ? "" : string.Format("where {0}", visitModel.Where.Replace(string.Format("{0}.", predicate.Parameters[0].Name), "")));

                result.Sql = ParameterToSql.ObjectParamToSql(visitModel.Param, sql.ToString(), config);

                Dispose(cmd);

                if (visitModel.Param.Count != 0)
                    cmd.Parameters.AddRange(visitModel.Param.ToArray());

                tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, sql.ToString(), visitModel.Param, config, false,AopType.Delete_Lambda);

                if (visitModel.IsSuccess)
                {
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, sql.ToString());
                }
                else
                    result.WriteReturn.IsSuccess = false;

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, sql.ToString(), visitModel.Param, config, false, AopType.Delete_Lambda, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Delete by Lambda tableName"+typeof(T).Name,config, AopType.Delete_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Delete<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Delete<T>", result.Sql);

                if (isTrans)
                    RollbackTrans();

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 删除
        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <returns></returns>
        public DataReturn<T> Delete<T>(T model, bool isTrans = false) where T : class, new()
        {
            var result = new DataReturn<T>();
            var optionModel = new OptionModel();
            var tableName = new List<string>();

            try
            {
                if (isTrans)
                    BeginTrans();

                optionModel = BaseModel.DeleteToSql<T>(cmd, model, config);

                result.Sql = ParameterToSql.ObjectParamToSql(optionModel.Param, optionModel.Sql, config);

                Dispose(cmd);

                if (optionModel.Param.Count != 0)
                    cmd.Parameters.AddRange(optionModel.Param.ToArray());

                tableName.Add(TableNameHelper.GetTableName<T>());
                AopBefore(tableName, optionModel.Sql, optionModel.Param, config, false,AopType.Delete_PrimaryKey);

                if (optionModel.IsSuccess)
                {
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, optionModel.Sql);
                }
                else
                {
                    result.WriteReturn.IsSuccess = false;
                    result.WriteReturn.Message = optionModel.Message;
                }

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, optionModel.Sql, optionModel.Param, config, false, AopType.Delete_PrimaryKey, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Delete by Primary Key tableName" + typeof(T).Name,config, AopType.Delete_PrimaryKey);

                if (isTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Delete<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Delete<T>", result.Sql);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 修改(Lambda表达式)
        /// <summary>
        /// 修改(Lambda表达式)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="predicate">表达式</param>
        /// <returns></returns>
        public DataReturn<T> Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, bool isTrans = false) where T : class, new()
        {
            string sql = "";
            var result = new DataReturn<T>();
            var visitModel = new VisitModel();
            var update = new OptionModel();
            var tableName = new List<string>();

            try
            {
                if (isTrans)
                    BeginTrans();

                update = BaseModel.UpdateToSql<T>(model, config, field, cmd);

                if (update.IsSuccess)
                {
                    visitModel = VisitExpression.LambdaWhere<T>(predicate, config);

                    sql = string.Format("{0} {1}", update.Sql, string.IsNullOrEmpty(visitModel.Where) ? "" : string.Format("where {0}", visitModel.Where.Replace(string.Format("{0}.", predicate.Parameters[0].Name), "")));

                    Dispose(cmd);

                    if (update.Param.Count != 0)
                        cmd.Parameters.AddRange(update.Param.ToArray());

                    if (visitModel.Param.Count != 0)
                        cmd.Parameters.AddRange(visitModel.Param.ToArray());

                    result.Sql = ParameterToSql.ObjectParamToSql(Parameter.ParamMerge(update.Param, visitModel.Param), sql, config);

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, sql, Parameter.ParamMerge(update.Param, visitModel.Param), config, false,AopType.Update_Lambda);

                    if (visitModel.IsSuccess)
                    {
                        if (conn.State == ConnectionState.Closed)
                            conn.Open();
                        result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, sql);
                    }
                    else
                        result.WriteReturn.IsSuccess = false;
                }
                else
                {
                    result.WriteReturn.Message = update.Message;
                    result.WriteReturn.IsSuccess = false;
                }

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, sql, Parameter.ParamMerge(update.Param, visitModel.Param), config, false, AopType.Update_Lambda, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Update by Lambda tableName:" + typeof(T).Name,config, AopType.Update_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Update<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Update<T>", result.Sql);
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;

                if (isTrans)
                    RollbackTrans();
            }

            return result;
        }
        #endregion

        #region 修改
        /// <summary>
        /// 修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="field"></param>
        /// <param name="isTrans"></param>
        /// <returns></returns>
        public DataReturn<T> Update<T>(T model, Expression<Func<T, object>> field = null, bool isTrans = false) where T : class, new()
        {
            var result = new DataReturn<T>();
            var update = new OptionModel();
            var tableName = new List<string>();
            try
            {

                update = BaseModel.UpdateToSql<T>(cmd, model, config, field);
                if (isTrans)
                    BeginTrans();
                if (update.IsSuccess)
                {
                    Dispose(cmd);

                    if (update.Param.Count != 0)
                        cmd.Parameters.AddRange(update.Param.ToArray());

                    result.Sql = ParameterToSql.ObjectParamToSql(update.Param, update.Sql, config);

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, update.Sql, update.Param, config, false,AopType.Update_PrimaryKey);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, update.Sql);
                }
                else
                {
                    result.WriteReturn.Message = update.Message;
                    result.WriteReturn.IsSuccess = false;
                }

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, update.Sql, update.Param, config, false, AopType.Update_PrimaryKey, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Update by Primary Key tableName:" + typeof(T).Name,config, AopType.Update_PrimaryKey);

                if (isTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "UpdateModel<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "UpdateModel<T>", result.Sql);
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            return result;
        }
        #endregion


        #region 修改list
        /// <summary>
        /// 修改list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="field"></param>
        /// <param name="isTrans"></param>
        /// <returns></returns>
        public DataReturn<T> UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null) where T : class, new()
        {
            var result = new DataReturn<T>();
            var update = new OptionModel();
            var tableName = new List<string>();
            try
            {
                if (list.Count == 0)
                {
                    result.WriteReturn.IsSuccess = false;
                    result.WriteReturn.Message = "更新数据不能为空";
                    return result;
                }

                update = BaseModel.UpdateListToSql<T>(cmd, list, config, field);

                if (update.IsSuccess)
                {
                    using (var adapter = DbProviderFactories.GetFactory(config.ProviderName).CreateDataAdapter())
                    {
                        BeginTrans();
                        Dispose(cmd);
                        adapter.InsertCommand = cmd;
                        adapter.InsertCommand.CommandText = update.Sql;
                        adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
                        adapter.UpdateBatchSize = 0;

                        if (update.Param.Count != 0)
                            adapter.InsertCommand.Parameters.AddRange(update.Param.ToArray());

                        result.Sql = ParameterToSql.ObjectParamToSql(update.Param, update.Sql, config);
                        
                        tableName.Add(TableNameHelper.GetTableName<T>());
                        AopBefore(tableName, update.Sql, update.Param, config, false,AopType.UpdateList);

                        result.WriteReturn.IsSuccess = adapter.Update(update.table) > 0;
                        if (result.WriteReturn.IsSuccess)
                            SubmitTrans();
                        else
                            RollbackTrans();

                        AopAfter(tableName, update.Sql, update.Param, config, false, AopType.UpdateList, result.WriteReturn.IsSuccess);
                    }
                }
                else
                {
                    result.WriteReturn.Message = update.Message;
                    result.WriteReturn.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                AopException(ex, "Update List tableName:" + typeof(T).Name,config, AopType.UpdateList);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "UpdateList<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "UpdateList<T>", result.Sql);
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 增加
        /// <summary>
        /// 增加
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <returns></returns>
        public DataReturn<T> Add<T>(T model, bool isTrans = false, Expression<Func<T, object>> notAddField = null) where T : class, new()
        {
            var result = new DataReturn<T>();
            var insert = new OptionModel();
            var tableName = new List<string>();

            try
            {
                if (isTrans)
                    BeginTrans();

                insert = BaseModel.InsertToSql<T>(model, config);

                if (!insert.IsSuccess)
                {
                }

                if (insert.IsSuccess)
                {
                    result.Sql = ParameterToSql.ObjectParamToSql(insert.Param, insert.Sql, config);

                    Dispose(cmd);

                    if (insert.Param.Count != 0)
                        cmd.Parameters.AddRange(insert.Param.ToArray());

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, insert.Sql, insert.Param, config, false,AopType.Add);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, insert.Sql);

                    if (isTrans)
                        SubmitTrans();

                    AopAfter(tableName, insert.Sql, insert.Param, config, false, AopType.Add, result.WriteReturn.IsSuccess);

                    return result;
                }
                else
                    return result;
            }
            catch (Exception ex)
            {
                AopException(ex, "Add tableName: " + typeof(T).Name,config, AopType.Add);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Add<T>", "");
                else
                    DbLog.LogException<T>(config?.IsOutError ?? false, config?.DbType ?? DataDbType.SqlServer, ex, "Add<T>", result.Sql);

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                result.WriteReturn.Message = $"{ex.GetType().Name}: {ex.Message}";
                result.WriteReturn.IsSuccess = false;
                return result;
            }
        }
        #endregion

        #region 批量增加 
        /// <summary>
        /// 批量增加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="IsTrans"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public DataReturn<T> AddList<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            var result = new DataReturn<T>();
            var sql = new StringBuilder();
            var dyn = new Property.DynamicGet<T>();
            var tableName = new List<string>();

            try
            {
                if (IsTrans)
                    BeginTrans();

                if (config.DbType == DataDbType.Oracle)
                {
                    #region oracle
                    Dispose(cmd);
                    if (!isLog)
                    {
                        cmd.CommandText = string.Format("alter table {0} nologging", TableNameHelper.GetTableName<T>());
                        cmd.ExecuteNonQuery();
                    }

                    cmd.GetType().GetMethods().ToList().ForEach(a =>
                    {
                        if (a.Name == "set_ArrayBindCount")
                        {
                            var param = new object[1];
                            param[0] = list.Count;
                            a.Invoke(cmd, param);
                        }

                        if (a.Name == "set_BindByName")
                        {
                            var param = new object[1];
                            param[0] = true;
                            a.Invoke(cmd, param);
                        }
                    });

                    sql.AppendFormat("insert into {0} values(", TableNameHelper.GetTableName<T>());

                    PropertyCache.GetPropertyInfo<T>().ForEach(a =>
                    {
                        var pValue = new List<object>();
                        var param = DbProviderFactories.GetFactory(config.ProviderName).CreateParameter();

                        if (a.PropertyType.Name.ToLower() == "nullable`1")
                            param.DbType = CommandParam.GetOracleDbType(a.PropertyType.GetGenericArguments()[0].Name);
                        else
                            param.DbType = CommandParam.GetOracleDbType(a.PropertyType.Name);

                        param.Direction = ParameterDirection.Input;
                        param.ParameterName = a.Name;
                        sql.AppendFormat("{0}{1},", config.Flag, a.Name);

                        list.ForEach(l =>
                        {
                            var value = dyn.GetValue(l, a.Name, true);
                            if (value == null)
                                value = DBNull.Value;
                            pValue.Add(value);
                        });

                        param.Value = pValue.ToArray();
                        cmd.Parameters.Add(param);
                    });

                    sql.Append(")");
                    cmd.CommandText = sql.ToString().Replace(",)", ")");

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, cmd.CommandText, null, config, false, AopType.AddList);

                    result.WriteReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;

                    if (!isLog)
                    {
                        cmd.CommandText = string.Format("alter table {0} logging", TableNameHelper.GetTableName<T>());
                        cmd.ExecuteNonQuery();
                    }
                    #endregion
                }

                if (config.DbType == DataDbType.SqlServer)
                {
                    #region sqlserver
                    Dispose(cmd);
                    
                    // 打开连接以支持 InitTvps 和 GetTable 中的 ExecuteReader
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    
                    CommandParam.InitTvps<T>(cmd);
                    foreach (var method in cmd.Parameters.GetType().GetMethods())
                    {
                        if (method.Name == "AddWithValue")
                        {
                            var param = new object[2];
                            param[0] = string.Format("@{0}_TVP", typeof(T).Name);
                            param[1] = CommandParam.GetTable<T>(cmd, list);
                            var sqlParam = method.Invoke(cmd.Parameters, param);

                            sqlParam.GetType().GetMethods().ToList().ForEach(a =>
                            {
                                if (a.Name == "set_SqlDbType")
                                {
                                    param = new object[1];
                                    param[0] = SqlDbType.Structured;
                                    a.Invoke(sqlParam, param);
                                }
                                if (a.Name == "set_TypeName")
                                {
                                    param = new object[1];
                                    // 使用新的 TVP 类型名称（排除 Identity 列）
                                    param[0] = $"{typeof(T).Name}_TVP";
                                    a.Invoke(sqlParam, param);
                                }
                            });

                            break;
                        }
                    }

                    cmd.CommandText = CommandParam.GetTvps<T>();

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, cmd.CommandText, null, config, false, AopType.AddList);

                    result.WriteReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;
                    #endregion
                }

                if (config.DbType == DataDbType.MySql)
                {
                    #region mysql
                    Dispose(cmd);
                    
                    // 获取属性信息，排除 Identity 列
                    var mysqlProperties = PropertyCache.GetPropertyInfo<T>();
                    var mysqlEntityType = typeof(T);
                    var nonIdentityProperties = mysqlProperties.Where(p => 
                    {
                        var propInfo = mysqlEntityType.GetProperty(p.Name);
                        if (propInfo == null) return true;
                        var columnAttr = propInfo.GetCustomAttributes(typeof(ColumnAttribute), false)
                            .FirstOrDefault() as ColumnAttribute;
                        return columnAttr == null || !columnAttr.IsIdentity;
                    }).ToList();
                    
                    // 构建排除 Identity 列的 SQL
                    var mysqlTableName = TableNameHelper.GetTableName<T>();
                    var mysqlColumns = string.Join(", ", nonIdentityProperties.Select(p => p.Name));
                    var mysqlValues = string.Join(", ", list.Select(item => 
                        $"({string.Join(", ", nonIdentityProperties.Select(p => {
                            var value = dyn.GetValue(item, p.Name, true);
                            if (value is bool boolVal)
                                return boolVal ? "1" : "0";
                            else if (value == null)
                                return "NULL";
                            else if (value is DateTime dtVal)
                                return $"'{dtVal:yyyy-MM-dd HH:mm:ss}'";
                            else
                                return $"'{value}'";
                        }))})"));
                    cmd.CommandText = $"INSERT INTO {mysqlTableName} ({mysqlColumns}) VALUES {mysqlValues}";

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, cmd.CommandText, null, config, false, AopType.AddList);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.WriteReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;
                    #endregion
                }

                if (config.DbType == DataDbType.PostgreSql)
                {
                    #region postgresql
                    Dispose(cmd);
                    
                    var pgTableName = TableNameHelper.GetTableName<T>();
                    var pgProperties = PropertyCache.GetPropertyInfo<T>();
                    
                    // 排除 Identity 列
                    var pgEntityType = typeof(T);
                    var pgNonIdentityProperties = pgProperties.Where(p => 
                    {
                        var propInfo = pgEntityType.GetProperty(p.Name);
                        if (propInfo == null) return true;
                        var columnAttr = propInfo.GetCustomAttributes(typeof(ColumnAttribute), false)
                            .FirstOrDefault() as ColumnAttribute;
                        return columnAttr == null || !columnAttr.IsIdentity;
                    }).ToList();
                    
                    var pgColumns = string.Join(", ", pgNonIdentityProperties.Select(p => p.Name));
                    var pgPlaceholders = string.Join(", ", pgNonIdentityProperties.Select((p, i) => $"{config.Flag}{p.Name}"));
                    var insertSql = $"INSERT INTO {pgTableName} ({pgColumns}) VALUES ({pgPlaceholders})";
                    
                    AopBefore(new List<string> { pgTableName }, insertSql, null, config, false, AopType.AddList);
                    
                    // 使用事务包裹批量插入
                    if (!IsTrans)
                        BeginTrans();
                    
                    cmd.CommandText = insertSql;
                    
                    foreach (var item in list)
                    {
                        cmd.Parameters.Clear();
                        foreach (var prop in pgNonIdentityProperties)
                        {
                            var param = cmd.CreateParameter();
                            param.ParameterName = $"{config.Flag}{prop.Name}";
                            var value = dyn.GetValue(item, prop.Name, true);
                            // 处理布尔值，PostgreSql 使用 true/false
                            if (value is bool pgBoolVal)
                                param.Value = pgBoolVal;
                            else
                                param.Value = value ?? DBNull.Value;
                            cmd.Parameters.Add(param);
                        }
                        
                        if (conn.State == ConnectionState.Closed)
                            conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    
                    if (!IsTrans)
                        SubmitTrans();
                    
                    result.WriteReturn.IsSuccess = true;
                    result.Sql = $"{insertSql} (x{list.Count})";
                    
                    AopAfter(new List<string> { pgTableName }, result.Sql, null, config, false, AopType.AddList, result.WriteReturn.IsSuccess);
                    #endregion
                }

                if (config.DbType == DataDbType.SQLite)
                {
                    #region sqlite
                    Dispose(cmd);
                    
                    var sqliteTableName = TableNameHelper.GetTableName<T>();
                    var properties = PropertyCache.GetPropertyInfo<T>();
                    var columns = string.Join(", ", properties.Select(p => p.Name));
                    var placeholders = string.Join(", ", properties.Select(p => $"{config.Flag}{p.Name}"));
                    var insertSql = $"INSERT INTO {sqliteTableName} ({columns}) VALUES ({placeholders})";
                    
                    AopBefore(new List<string> { sqliteTableName }, insertSql, null, config, false, AopType.AddList);
                    
                    // 使用事务包裹批量插入
                    if (!IsTrans)
                        BeginTrans();
                    
                    cmd.CommandText = insertSql;
                    
                    foreach (var item in list)
                    {
                        cmd.Parameters.Clear();
                        foreach (var prop in properties)
                        {
                            var param = cmd.CreateParameter();
                            param.ParameterName = $"{config.Flag}{prop.Name}";
                            var value = dyn.GetValue(item, prop.Name, true);
                            param.Value = value ?? DBNull.Value;
                            cmd.Parameters.Add(param);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    
                    if (!IsTrans)
                        SubmitTrans();
                    
                    result.WriteReturn.IsSuccess = true;
                    result.Sql = $"{insertSql} (x{list.Count})";
                    
                    AopAfter(new List<string> { sqliteTableName }, result.Sql, null, config, false, AopType.AddList, result.WriteReturn.IsSuccess);
                    #endregion
                }

                if (result.WriteReturn.IsSuccess && IsTrans)
                    SubmitTrans();
                else if (result.WriteReturn.IsSuccess == false && IsTrans)
                    RollbackTrans();

                AopAfter(tableName, cmd.CommandText, null, config, false, AopType.AddList, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Add List tableName:" + typeof(T).Name,config,AopType.AddList);

                if (IsTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "AddList<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "AddList<T>", result.Sql);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
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
        /// <param name="isTrans">是否事务</param>
        /// <param name="isLog">是否记录日志</param>
        /// <param name="IsProcedure">是否存储过程</param>
        /// <param name="isAop">是否启用AOP</param>
        /// <returns>数据返回对象</returns>
        public DataReturn ExecuteSql(string sql, DbParameter[] param = null, bool isTrans = false, bool isLog = false, bool IsProcedure = false,bool isAop=true)
        {
            var result = new DataReturn();
            try
            {
                if (isTrans)
                    BeginTrans();

                if (param != null)
                    result.Sql = ParameterToSql.ObjectParamToSql(param.ToList(), sql, config);
                else
                    result.Sql = sql;

                DbLog.LogSql(isLog, result.Sql, config.DbType, 0);

                Dispose(cmd);

                if (param != null)
                    cmd.Parameters.AddRange(param);

                if (isAop)
                    AopBefore(null, sql, param?.ToList(), config, false,AopType.Execute_Sql_Bool);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                result.WriteReturn.IsSuccess = BaseExecute.ToBool(cmd, sql, IsProcedure);

                if (isTrans && result.WriteReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.WriteReturn.IsSuccess == false)
                    RollbackTrans();

                if (isAop)
                    AopAfter(null, sql, param?.ToList(), config, false, AopType.Execute_Sql_Bool, result.WriteReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Excute Sql",config, AopType.Execute_Sql_Bool);

                if (isTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "ExecuteSql", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "ExecuteSql", result.Sql);
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

              return result;
        }
        #endregion

        #region 批量插入
        /// <summary>
        /// 批量插入（使用事务优化性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="isLog">是否记录SQL</param>
        /// <returns>插入结果</returns>
        public DataReturn BulkInsert<T>(List<T> list, bool isLog = false) where T : class, new()
        {
            var result = new DataReturn();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                BeginTrans();
                var tableName = TableNameHelper.GetTableName<T>();
                var properties = typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite).ToList();
                var columnNames = string.Join(", ", properties.Select(p => p.Name));

                Dispose(cmd);

                var paramNames = string.Join(", ", properties.Select((_, i) => $"@p{i}"));
                var insertSql = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})";

                foreach (var item in list)
                {
                    cmd.Parameters.Clear();
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var value = properties[i].GetValue(item);
                        var param = cmd.CreateParameter();
                        param.ParameterName = $"@p{i}";
                        param.Value = value ?? DBNull.Value;
                        cmd.Parameters.Add(param);
                    }

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    cmd.ExecuteNonQuery();
                }

                result.Sql = $"{insertSql} (x{list.Count})";
                if (isLog)
                    DbLog.LogSql(true, result.Sql, config.DbType, 0);

                AopBefore(new List<string> { tableName }, insertSql, new List<DbParameter>(), config, false, AopType.AddList);
                result.WriteReturn.IsSuccess = true;
                SubmitTrans();
                AopAfter(new List<string> { tableName }, insertSql, new List<DbParameter>(), config, false, AopType.AddList, list.Count);
            }
            catch (Exception ex)
            {
                RollbackTrans();
                AopException(ex, "BulkInsert", config, AopType.AddList);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "BulkInsert", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "BulkInsert", result.Sql);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            return result;
        }
        #endregion

        #region 批量更新/删除

        /// <summary>
        /// 批量更新实体（使用 SQL UPDATE ... WHERE IN）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="isLog">是否记录SQL</param>
        /// <returns>更新结果</returns>
        public DataReturn BulkUpdate<T>(List<T> list, Expression<Func<T, bool>> predicate, bool isLog = false) where T : class, new()
        {
            var result = new DataReturn();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                BeginTrans();

                var tableName = TableNameHelper.GetTableName<T>();
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite && p.Name != "Id")
                    .ToList();

                var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                var visitModel = VisitExpression.LambdaWhere<T>(predicate, config);
                if (!visitModel.IsSuccess)
                {
                    throw new InvalidOperationException("更新条件解析失败");
                }

                var whereClause = visitModel.Where;
                var paramList = visitModel.Param;

                foreach (var item in list)
                {
                    cmd.Parameters.Clear();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item);
                        var param = cmd.CreateParameter();
                        param.ParameterName = $"@{prop.Name}";
                        param.Value = value ?? DBNull.Value;
                        cmd.Parameters.Add(param);
                    }

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();

                    var updateSql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
                    cmd.CommandText = updateSql;
                    cmd.ExecuteNonQuery();
                }

                result.Sql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause} (x{list.Count})";
                if (isLog)
                    DbLog.LogSql(true, result.Sql, config.DbType, 0);

                AopBefore(new List<string> { tableName }, result.Sql, paramList, config, false, AopType.UpdateList);
                result.WriteReturn.IsSuccess = true;
                SubmitTrans();
                AopAfter(new List<string> { tableName }, result.Sql, paramList, config, false, AopType.UpdateList, list.Count);
            }
            catch (Exception ex)
            {
                RollbackTrans();
                AopException(ex, "BulkUpdate", config, AopType.UpdateList);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "BulkUpdate", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "BulkUpdate", result.Sql);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            return result;
        }

        /// <summary>
        /// 批量删除实体（使用 SQL DELETE FROM ... WHERE IN）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="isLog">是否记录SQL</param>
        /// <returns>删除结果</returns>
        public DataReturn BulkDelete<T>(Expression<Func<T, bool>> predicate, bool isLog = false) where T : class, new()
        {
            var result = new DataReturn();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                BeginTrans();

                var tableName = TableNameHelper.GetTableName<T>();
                var visitModel = VisitExpression.LambdaWhere<T>(predicate, config);
                if (!visitModel.IsSuccess)
                {
                    throw new InvalidOperationException("删除条件解析失败");
                }

                var whereClause = visitModel.Where;
                var paramList = visitModel.Param;

                if (conn.State == ConnectionState.Closed)
                    conn.Open();

                var deleteSql = $"DELETE FROM {tableName} WHERE {whereClause}";
                cmd.CommandText = deleteSql;

                foreach (var param in paramList)
                {
                    cmd.Parameters.Add(param);
                }

                int affectedRows = cmd.ExecuteNonQuery();
                result.Sql = $"DELETE FROM {tableName} WHERE {whereClause} (x{affectedRows})";
                if (isLog)
                    DbLog.LogSql(true, result.Sql, config.DbType, 0);

                AopBefore(new List<string> { tableName }, result.Sql, paramList, config, false, AopType.Delete_Lambda);
                result.WriteReturn.IsSuccess = true;
                result.WriteReturn.Message = $"删除 {affectedRows} 条记录";
                SubmitTrans();
                AopAfter(new List<string> { tableName }, result.Sql, paramList, config, false, AopType.Delete_Lambda, affectedRows);
            }
            catch (Exception ex)
            {
                RollbackTrans();
                AopException(ex, "BulkDelete", config, AopType.Delete_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "BulkDelete", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "BulkDelete", result.Sql);

                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            return result;
        }

        #endregion
    }
}
