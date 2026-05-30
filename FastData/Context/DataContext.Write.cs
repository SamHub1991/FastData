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
using FastData.Type;
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

                result.sql = ParameterToSql.ObjectParamToSql(visitModel.Param, sql.ToString(), config);

                Dispose(cmd);

                if (visitModel.Param.Count != 0)
                    cmd.Parameters.AddRange(visitModel.Param.ToArray());

                tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, sql.ToString(), visitModel.Param, config, false,AopType.Delete_Lambda);

                if (visitModel.IsSuccess)
                {
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, sql.ToString());
                }
                else
                    result.writeReturn.IsSuccess = false;

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, sql.ToString(), visitModel.Param, config, false, AopType.Delete_Lambda, result.writeReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Delete by Lambda tableName"+typeof(T).Name,config, AopType.Delete_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Delete<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Delete<T>", result.sql);

                if (isTrans)
                    RollbackTrans();

                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
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

                result.sql = ParameterToSql.ObjectParamToSql(optionModel.Param, optionModel.Sql, config);

                Dispose(cmd);

                if (optionModel.Param.Count != 0)
                    cmd.Parameters.AddRange(optionModel.Param.ToArray());

                tableName.Add(TableNameHelper.GetTableName<T>());
                AopBefore(tableName, optionModel.Sql, optionModel.Param, config, false,AopType.Delete_PrimaryKey);

                if (optionModel.IsSuccess)
                {
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, optionModel.Sql);
                }
                else
                {
                    result.writeReturn.IsSuccess = false;
                    result.writeReturn.Message = optionModel.Message;
                }

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, optionModel.Sql, optionModel.Param, config, false, AopType.Delete_PrimaryKey, result.writeReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Delete by Primary Key tableName" + typeof(T).Name,config, AopType.Delete_PrimaryKey);

                if (isTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Delete<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Delete<T>", result.sql);

                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
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

                    result.sql = ParameterToSql.ObjectParamToSql(Parameter.ParamMerge(update.Param, visitModel.Param), sql, config);

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, sql, Parameter.ParamMerge(update.Param, visitModel.Param), config, false,AopType.Update_Lambda);

                    if (visitModel.IsSuccess)
                    {
                        if (conn.State == ConnectionState.Closed)
                            conn.Open();
                        result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, sql);
                    }
                    else
                        result.writeReturn.IsSuccess = false;
                }
                else
                {
                    result.writeReturn.Message = update.Message;
                    result.writeReturn.IsSuccess = false;
                }

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, sql, Parameter.ParamMerge(update.Param, visitModel.Param), config, false, AopType.Update_Lambda, result.writeReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Update by Lambda tableName:" + typeof(T).Name,config, AopType.Update_Lambda);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "Update<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "Update<T>", result.sql);
                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;

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

                    result.sql = ParameterToSql.ObjectParamToSql(update.Param, update.Sql, config);

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, update.Sql, update.Param, config, false,AopType.Update_PrimaryKey);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, update.Sql);
                }
                else
                {
                    result.writeReturn.Message = update.Message;
                    result.writeReturn.IsSuccess = false;
                }

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                AopAfter(tableName, update.Sql, update.Param, config, false, AopType.Update_PrimaryKey, result.writeReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Update by Primary Key tableName:" + typeof(T).Name,config, AopType.Update_PrimaryKey);

                if (isTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "UpdateModel<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "UpdateModel<T>", result.sql);
                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
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
                    result.writeReturn.IsSuccess = false;
                    result.writeReturn.Message = "更新数据不能为空";
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

                        result.sql = ParameterToSql.ObjectParamToSql(update.Param, update.Sql, config);
                        
                        tableName.Add(TableNameHelper.GetTableName<T>());
                        AopBefore(tableName, update.Sql, update.Param, config, false,AopType.UpdateList);

                        result.writeReturn.IsSuccess = adapter.Update(update.table) > 0;
                        if (result.writeReturn.IsSuccess)
                            SubmitTrans();
                        else
                            RollbackTrans();

                        AopAfter(tableName, update.Sql, update.Param, config, false, AopType.UpdateList, result.writeReturn.IsSuccess);
                    }
                }
                else
                {
                    result.writeReturn.Message = update.Message;
                    result.writeReturn.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                AopException(ex, "Update List tableName:" + typeof(T).Name,config, AopType.UpdateList);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "UpdateList<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "UpdateList<T>", result.sql);
                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
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
                    result.sql = ParameterToSql.ObjectParamToSql(insert.Param, insert.Sql, config);

                    Dispose(cmd);

                    if (insert.Param.Count != 0)
                        cmd.Parameters.AddRange(insert.Param.ToArray());

                    tableName.Add(TableNameHelper.GetTableName<T>());
                    AopBefore(tableName, insert.Sql, insert.Param, config, false,AopType.Add);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, insert.Sql);

                    if (isTrans)
                        SubmitTrans();

                    AopAfter(tableName, insert.Sql, insert.Param, config, false, AopType.Add, result.writeReturn.IsSuccess);

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
                    DbLog.LogException<T>(config?.IsOutError ?? false, config?.DbType ?? "Unknown", ex, "Add<T>", result.sql);

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                result.writeReturn.Message = $"{ex.GetType().Name}: {ex.Message}";
                result.writeReturn.IsSuccess = false;
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

                    result.writeReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;

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

                    result.writeReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;
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
                    result.writeReturn.IsSuccess = cmd.ExecuteNonQuery() > 0;
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
                    
                    result.writeReturn.IsSuccess = true;
                    result.sql = $"{insertSql} (x{list.Count})";
                    
                    AopAfter(new List<string> { pgTableName }, result.sql, null, config, false, AopType.AddList, result.writeReturn.IsSuccess);
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
                    
                    result.writeReturn.IsSuccess = true;
                    result.sql = $"{insertSql} (x{list.Count})";
                    
                    AopAfter(new List<string> { sqliteTableName }, result.sql, null, config, false, AopType.AddList, result.writeReturn.IsSuccess);
                    #endregion
                }

                if (result.writeReturn.IsSuccess && IsTrans)
                    SubmitTrans();
                else if (result.writeReturn.IsSuccess == false && IsTrans)
                    RollbackTrans();

                AopAfter(tableName, cmd.CommandText, null, config, false, AopType.AddList, result.writeReturn.IsSuccess);
            }
            catch (Exception ex)
            {
                AopException(ex, "Add List tableName:" + typeof(T).Name,config,AopType.AddList);

                if (IsTrans)
                    RollbackTrans();

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "AddList<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "AddList<T>", result.sql);

                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 执行sql
        /// <summary>
        /// 执行sql
        /// </summary>
        /// <returns></returns>
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
                result.writeReturn.IsSuccess = BaseExecute.ToBool(cmd, sql, IsProcedure);

                if (isTrans && result.writeReturn.IsSuccess)
                    SubmitTrans();
                else if (isTrans && result.writeReturn.IsSuccess == false)
                    RollbackTrans();

                if (isAop)
                    AopAfter(null, sql, param?.ToList(), config, false, AopType.Execute_Sql_Bool, result.writeReturn.IsSuccess);
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
                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                BeginTrans();
                var tableName = TableNameHelper.GetTableName<T>();
                var properties = typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite).ToList();
                var columns = properties.Select(p => p.Name).ToList();
                var columnNames = string.Join(", ", columns);
                
                int successCount = 0;
                var sqlBuilder = new StringBuilder();
                
                foreach (var item in list)
                {
                    var values = new List<string>();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item);
                        if (value == null)
                            values.Add("NULL");
                        else if (value is string || value is DateTime)
                            values.Add($"'{value.ToString().Replace("'", "''")}'");
                        else if (value is bool)
                            values.Add((bool)value ? "1" : "0");
                        else
                            values.Add(value.ToString());
                    }
                    
                    var valueStr = string.Join(", ", values);
                    sqlBuilder.AppendLine($"INSERT INTO {tableName} ({columnNames}) VALUES ({valueStr});");
                }
                
                var sql = sqlBuilder.ToString();
                result.Sql = sql;
                
                if (isLog)
                    DbLog.LogSql(isLog, sql, config.DbType, 0);

                AopBefore(new List<string> { tableName }, sql, new List<DbParameter>(), config, false, AopType.AddList);

                // 执行批量插入
                Dispose(cmd);
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                successCount = list.Count;
                result.writeReturn.IsSuccess = true;
                
                SubmitTrans();

                AopAfter(new List<string> { tableName }, sql, new List<DbParameter>(), config, false, AopType.AddList, successCount);
            }
            catch (Exception ex)
            {
                RollbackTrans();
                AopException(ex, "BulkInsert", config, AopType.AddList);
                
                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "BulkInsert", result.Sql);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "BulkInsert", result.Sql);
                
                result.writeReturn.IsSuccess = false;
                result.writeReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            return result;
        }
        #endregion
    }
}
