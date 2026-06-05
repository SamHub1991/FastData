using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FastData.Property;
using FastData.DbTypes;
using FastData.Model;
using System.Linq;

namespace FastData.Base
{
    /// <summary>
    /// lambda 表达式字段解析
    /// 将 LINQ 表达式中的查询字段、分组字段、排序字段转换为 SQL 列名
    /// </summary>
    internal static class BaseField
    {
        #region query field 单表查询
        /// <summary>
        /// 单表查询字段解析
        /// 将 LINQ 表达式中的字段转换为 SQL 查询列
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件表达式</param>
        /// <param name="field">返回字段表达式，为 null 时返回所有字段</param>
        /// <param name="config">数据库配置模型</param>
        /// <returns>字段模型（包含字段列表和别名列表）</returns>
        public static FieldModel QueryField<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field, ConfigModel config)
        {
            try
            {
                var result = new FieldModel();
                var queryFields = new List<string>();
                var i = 0;

                if (field == null)
                {
                    #region 无返回列
                    var list = PropertyCache.GetPropertyInfo<T>(config.IsPropertyCache);

                    PropertyCache.GetPropertyInfo<T>(config.IsPropertyCache).ForEach(p =>
                    {
                        if (list.Exists(a => a.Name == p.Name))
                            queryFields.Add(string.Format("{0}.{1}", predicate.Parameters[0].Name, p.Name));
                        else
                            queryFields.Add(p.Name);
                        result.AsName.Add(p.Name);
                    });

                    result.Field = string.Join(",", queryFields);

                    #endregion
                }
                else
                {
                    #region 有返回列
                    (field.Body as NewExpression).Arguments.ToList().ForEach(a =>
                    {
                        if (a is MethodCallExpression)
                        {
                            var methodName = "";
                            var ower = "";
                            var propertyName = GetPropertyMethod(a, out methodName, false, out ower);

                            if (methodName.ToLower() == "distinct")
                            {
                                queryFields.Add(string.Format("{2}{0} {3}.{1} ", methodName, propertyName, ower, predicate.Parameters[0].Name));
                                result.AsName.Add((a as MemberExpression).Member.Name);
                            }
                            else if (methodName.ToLower() == "sum")
                            {
                                if (config.DbType == DataDbType.SqlServer)
                                    propertyName = string.Format("isnull({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                                if (config.DbType == DataDbType.MySql || config.DbType == DataDbType.SQLite)
                                    propertyName = string.Format("ifnull({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                                if (config.DbType == DataDbType.Oracle)
                                    propertyName = string.Format("nvl({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                                if (config.DbType == DataDbType.DB2)
                                    propertyName = string.Format("coalesce({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                                queryFields.Add(string.Format("{0}({2}.{1})", methodName, propertyName, predicate.Parameters[0].Name));
                                result.AsName.Add((field.Body as NewExpression).Members[i].Name);
                            }
                            else
                            {
                                queryFields.Add(string.Format("{2}{0}({3}.{1})", methodName, propertyName, ower, predicate.Parameters[0].Name));
                                result.AsName.Add((field.Body as NewExpression).Members[i].Name);
                            }
                        }
                        else
                        {
                            queryFields.Add(string.Format("{0}.{1}", predicate.Parameters[0].Name, (a as MemberExpression).Member.Name));
                            result.AsName.Add((a as MemberExpression).Member.Name);
                        }
                        i++;
                    });
                    #endregion
                }

                result.Field = string.Join(",", queryFields);

                return result;
            }
            catch (Exception ex)
            {
                if (config.SqlErrorType.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException<T>(config, ex, "QueryField<T>", "");
                else
                    DbLog.LogException<T>(config.IsOutError, config.DbType, ex, "QueryField<T>", "");

                return new FieldModel
                {
                    Field = string.Format("{0}.*", predicate.Parameters[0].Name),
                    AsName = new List<string> { predicate.Parameters[0].Name }
                };
            }
        }
        #endregion
        
        #region query field 多表联查
        /// <summary>
        /// 多表联查字段解析
        /// 将 JOIN 查询中的 LINQ 表达式字段转换为 SQL 查询列
        /// </summary>
        /// <typeparam name="T">第一个表类型</typeparam>
        /// <typeparam name="T1">第二个表类型</typeparam>
        /// <param name="predicate">查询条件表达式</param>
        /// <param name="field">返回字段表达式</param>
        /// <param name="config">配置模型</param>
        /// <returns>字段模型</returns>
        public static FieldModel QueryField<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field, ConfigModel config)
        {
            try
            {
                var result = new FieldModel();
                var queryFields = new List<string>();

                if (field == null)
                {
                    var list = PropertyCache.GetPropertyInfo<T1>(config.IsPropertyCache);

                    PropertyCache.GetPropertyInfo<T1>(config.IsPropertyCache).ForEach(p =>
                    {
                        if (list.Exists(a => a.Name == a.Name))
                            queryFields.Add(string.Format("{0}.{1}", predicate.Parameters[1].Name, p.Name));
                        else
                            queryFields.Add(p.Name);

                        result.AsName.Add(p.Name);
                    });

                    result.Field = string.Join(",", queryFields);
                    return result;
                }

                var i = 0;
                (field.Body as NewExpression).Arguments.ToList().ForEach(a =>
                {
                    if (a is MethodCallExpression)
                    {
                        var methodName = "";
                        var ower = "";
                        var propertyName = GetPropertyMethod(a, out methodName, true, out ower);

                        if (methodName.ToLower() == "distinct")
                        {
                            queryFields.Add(string.Format("{2}{0} {2}.{1}", methodName, propertyName, ower, predicate.Parameters[0].Name));
                            result.AsName.Add((a as MemberExpression).Member.Name);
                        }
                        else if (methodName.ToLower() == "sum")
                        {
                            if (config.DbType == DataDbType.SqlServer)
                                propertyName = string.Format("isnull({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                            if (config.DbType == DataDbType.MySql || config.DbType == DataDbType.SQLite)
                                propertyName = string.Format("ifnull({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                            if (config.DbType == DataDbType.Oracle)
                                propertyName = string.Format("nvl({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                            if (config.DbType == DataDbType.DB2)
                                propertyName = string.Format("coalesce({1}.{0},0)", propertyName, predicate.Parameters[0].Name);

                            queryFields.Add(string.Format("{0}({2}.{1})", methodName, propertyName, predicate.Parameters[0].Name));
                            result.AsName.Add((field.Body as NewExpression).Members[i].Name);
                        }
                        else
                        {
                            queryFields.Add(string.Format("{2}{0}({3}.{1})", methodName, propertyName, ower, predicate.Parameters[0].Name));
                            result.AsName.Add((field.Body as NewExpression).Members[i].Name);
                        }
                    }
                    else
                    {
                        if (a is MemberExpression)
                        {
                            queryFields.Add(string.Format("{0}.{1}", ((a as MemberExpression).Expression as ParameterExpression).Name, (a as MemberExpression).Member.Name));
                            result.AsName.Add((a as MemberExpression).Member.Name);
                        }
                    }
                    i++;
                });

                result.Field = string.Join(",", queryFields);
                return result;
            }
            catch (Exception ex)
            {
                if (config.SqlErrorType.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "QueryField<T1,T2,T>", "");
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "QueryField<T1,T2,T>", "");

                return new FieldModel { Field = "*" };
            }
        }
        #endregion

        #region group by 
        /// <summary>
        /// GROUP BY 分组字段解析
        /// 将 LINQ 表达式中的分组字段转换为 SQL GROUP BY 列名
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="field">分组字段表达式</param>
        /// <param name="config">数据库配置模型</param>
        /// <returns>分组字段列表</returns>
        public static List<string> GroupBy<T>(Expression<Func<T, object>> field, ConfigModel config)
        {
            try
            {
                var result = new List<string>();
                (field.Body as NewExpression).Arguments.ToList().ForEach(a =>
                {
                    var asName = ((a as MemberExpression).Expression as ParameterExpression).Name;
                    result.Add(string.Format("{0}.{1}", asName, (a as MemberExpression).Member.Name));
                });

                return result;
            }
            catch (Exception ex)
            {
                if (config.SqlErrorType.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "GroupBy<T>", "");
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "GroupBy<T>", "");

                return new List<string>();
            }
        }
        #endregion
        
        #region order by
        /// <summary>
        /// ORDER BY 排序字段解析
        /// 将 LINQ 表达式中的排序字段转换为 SQL ORDER BY 列名
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="field">排序字段表达式</param>
        /// <param name="config">数据库配置模型</param>
        /// <param name="isDesc">是否降序排列，默认 true（降序）</param>
        /// <returns>排序字段列表</returns>
        public static List<string> OrderBy<T>(Expression<Func<T, object>> field, ConfigModel config, bool isDesc = true)
        {
            try
            {
                var result = new List<string>();
                (field.Body as NewExpression).Arguments.ToList().ForEach(a =>
                {
                    var asName = ((a as MemberExpression).Expression as ParameterExpression).Name;
                    result.Add(string.Format("{0}.{1} {2}", asName, (a as MemberExpression).Member.Name, isDesc ? "desc" : "asc"));
                });

                return result;
            }
            catch (Exception ex)
            {
                if (config.SqlErrorType.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "OrderBy<T>", "");
                else
                    DbLog.LogException(config.IsOutError, config.DbType, ex, "OrderBy<T>", "");

                return new List<string>();
            }
        }
        #endregion

        #region 获取属性方法
        /// <summary>
        /// 获取属性方法
        /// </summary>
        /// <param name="item">表达式</param>
        /// <param name="methodName">方法名（输出参数）</param>
        /// <param name="IsMoreTable">是否多表查询</param>
        /// <param name="ower">所有者（输出参数）</param>
        /// <returns>属性名称</returns>
        private static string GetPropertyMethod(Expression item, out string methodName, bool IsMoreTable, out string ower)
        {
            var result = new List<string>();
            methodName = (item as MethodCallExpression).Method.Name;
            var _ower = "";

            var meExp = (MethodCallExpression)(item.ReduceExtensions().Reduce());
            var count = 0;

            meExp.Arguments.ToList().ForEach(a => {
                count++;
                if (a is UnaryExpression)
                {
                    if (IsMoreTable)
                    {
                        var asName = "";
                        var name = "";
                        if (a is MemberExpression)
                        {
                            asName = ((a as MemberExpression).Expression as ParameterExpression).Name;
                            name = (a as MemberExpression).Member.Name;
                        }
                        else if (a is UnaryExpression)
                        {
                            asName = (((a as UnaryExpression).Operand as MemberExpression).Expression as ParameterExpression).Name;
                            name = ((a as UnaryExpression).Operand as MemberExpression).Member.Name;
                        }

                        result.Add(string.Format("{0}.{1}", asName, name));
                    }
                    else
                        result.Add(a is MemberExpression ? (a as MemberExpression).Member.Name : ((a as UnaryExpression).Operand as MemberExpression).Member.Name);
                }

                if (a is MemberExpression || a is NewArrayExpression)
                {
                    if ((a as MemberExpression).Expression is ConstantExpression)
                        result.Add(Expression.Lambda(a).Compile().DynamicInvoke().ToString());

                    if ((a as MemberExpression).Expression is MemberExpression)
                        result.Add(Expression.Lambda(a).Compile().DynamicInvoke().ToString());
                }

                if (a is ConstantExpression && count != meExp.Arguments.Count)
                    result.Add((a as ConstantExpression).Value.ToString());

                if (a is ConstantExpression && count == meExp.Arguments.Count)
                    _ower = (a as ConstantExpression).Value.ToString();
            });

            ower = _ower;
            return string.Join(",", result);

        }
        #endregion
    }
}