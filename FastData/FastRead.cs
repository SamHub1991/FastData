using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Data.Common;
using FastUntility.Page;
using FastData.Base;
using FastData.Config;
using FastData.Model;
using FastData.Repository;
using System.Diagnostics;
using FastData.Context;
using System.Data;
using System.Reflection;
#if !NETFRAMEWORK
using FastData.Queue;
#endif

namespace FastData
{
    /// <summary>
    /// orm查询
    /// </summary>
    public static class FastRead
    {
        public static FastReadDb Use(string key)
        {
            return new FastReadDb(key);
        }

#if !NETFRAMEWORK
        /// <summary>
        /// 创建链式读取构建器（带消息队列支持）
        /// 支持 Fluent API：FastRead.QueueBuilder&lt;User&gt;().Where(u => u.IsActive).Execute()
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="databaseKey">数据库 Key（可选）</param>
        /// <returns>链式构建器</returns>
        public static FastReadQueueBuilder<T> QueueBuilder<T>(string databaseKey = null) where T : class, new()
        {
            return new FastReadQueueBuilder<T>(databaseKey);
        }

        /// <summary>
        /// 配置表级别的消息队列
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="config">队列配置</param>
        public static void ConfigureQueue<T>(WriteBehindConfig config) where T : class
        {
            WriteBehindRegistry.Register<T>(config);
        }

        /// <summary>
        /// 配置表级别的消息队列
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="config">队列配置</param>
        public static void ConfigureQueue(string tableName, WriteBehindConfig config)
        {
            WriteBehindRegistry.Register(tableName, config);
        }

        /// <summary>
        /// 检查表是否启用了消息队列
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>是否启用队列</returns>
        public static bool IsQueueEnabled<T>() where T : class
        {
            return WriteBehindRegistry.IsQueueEnabled<T>();
        }

        /// <summary>
        /// 检查表是否启用了消息队列
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>是否启用队列</returns>
        public static bool IsQueueEnabled(string tableName)
        {
            return WriteBehindRegistry.IsQueueEnabled(tableName);
        }
#endif

        /// <summary>
        /// 查询join
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <typeparam name="T1">泛型</typeparam>
        /// <param name="joinType">left join,right join,inner join</param>
        /// <param name="item"></param>
        /// <param name="predicate">条件</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        private static DataQuery JoinType<T, T1>(string joinType, DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false)
        {
            var queryField = BaseField.QueryField<T, T1>(predicate, field, item.Config);
            item.Field.Add(queryField.Field);
            item.AsName.AddRange(queryField.AsName);

            var condtion = VisitExpression.LambdaWhere<T, T1>(predicate, item.Config);
            item.Predicate.Add(condtion);
            item.Table.Add(string.Format("{2} {0}{3} {1}", typeof(T1).Name, predicate.Parameters[1].Name
            , joinType, isDblink && !string.IsNullOrEmpty(item.Config.DbLinkName) ? string.Format("@", item.Config.DbLinkName) : ""));

            return item;
        }

        /// <summary>
        /// 表查询
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="field">字段</param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static DataQuery<T> Query<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, string key = null, string dbFile = "db.config") where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            var result = new DataQuery<T>();
            result.Config = DataConfig.GetConfig(key, projectName, dbFile);
            result.Key = key;

            var queryField = BaseField.QueryField<T>(predicate, field, result.Config);
            result.Field.Add(queryField.Field);
            result.AsName.AddRange(queryField.AsName);

            var condtion = VisitExpression.LambdaWhere<T>(predicate, result.Config);
            result.Predicate.Add(condtion);
            result.Table.Add(string.Format("{0} {1}", TableNameHelper.GetTableName<T>(), predicate.Parameters[0].Name));

            return result;
        }

        /// <summary>
        /// 投影查询（支持匿名类型）
        /// 用法：FastRead.Query&lt;User&gt;(u => u.IsActive).Select(p => new { p.Id, p.UserName }).ToList()
        /// </summary>
        /// <typeparam name="T">源实体类型</typeparam>
        /// <typeparam name="TResult">投影结果类型（可以是匿名类型）</typeparam>
        /// <param name="query">查询条件</param>
        /// <param name="selector">投影表达式</param>
        /// <returns>投影查询对象</returns>
        public static ProjectedQuery<T, TResult> Select<T, TResult>(this DataQuery query, Expression<Func<T, TResult>> selector) where T : class, new()
        {
            return new ProjectedQuery<T, TResult>(query, selector);
        }

        /// <summary>
        /// 链式追加 WHERE 条件（AND）
        /// 用法：FastRead.Query&lt;User&gt;().Where&lt;User&gt;(u => u.IsActive).And&lt;User&gt;(u => u.Age > 18).ToList()
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <returns>查询对象（支持链式调用）</returns>
        public static DataQuery Where<T>(this DataQuery query, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            if (predicate == null)
                return query;

            query.EntityType = typeof(T);
            var visitModel = VisitExpression.LambdaWhere<T>(predicate, query.Config);
            if (visitModel.IsSuccess)
            {
                query.ChainedConditions.Add(new ChainedCondition
                {
                    Operator = "AND",
                    Where = visitModel.Where,
                    Param = visitModel.Param
                });
            }

            return query;
        }

        /// <summary>
        /// 链式追加 WHERE 条件（AND）- And 别名
        /// </summary>
        public static DataQuery And<T>(this DataQuery query, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            return Where<T>(query, predicate);
        }

        /// <summary>
        /// 链式追加 WHERE 条件（OR）
        /// 用法：FastRead.Query&lt;User&gt;(u => u.Department == "IT").Or&lt;User&gt;(u => u.Department == "HR").ToList()
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <returns>查询对象（支持链式调用）</returns>
        public static DataQuery Or<T>(this DataQuery query, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            if (predicate == null)
                return query;

            query.EntityType = typeof(T);
            var visitModel = VisitExpression.LambdaWhere<T>(predicate, query.Config);
            if (visitModel.IsSuccess)
            {
                query.ChainedConditions.Add(new ChainedCondition
                {
                    Operator = "OR",
                    Where = visitModel.Where,
                    Param = visitModel.Param
                });
            }

            return query;
        }

        /// <summary>
        /// 链式追加 LIKE 条件
        /// 用法：FastRead.Query&lt;User&gt;().Like&lt;User&gt;(u => u.UserName, "张%")
        /// </summary>
        public static DataQuery Like<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            if (field == null || string.IsNullOrEmpty(value))
                return query;

            query.EntityType = typeof(T);
            var fieldName = GetMemberName(field);
            var param = new DbParameter[0]; // dummy for using clause
            var paramName = $"@like_{query.ChainedConditions.Count}";
            var dbParam = GetDbParameter(query, paramName, value);
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = $"{fieldName} like {paramName}",
                Param = new List<DbParameter> { dbParam }
            });

            return query;
        }

        /// <summary>
        /// 链式追加 LIKE 条件（包含 - LIKE '%value%'）
        /// </summary>
        public static DataQuery Contains<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, $"%{value}%");
        }

        /// <summary>
        /// 链式追加 LIKE 条件（开头 - LIKE 'value%'）
        /// </summary>
        public static DataQuery StartsWith<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, $"{value}%");
        }

        /// <summary>
        /// 链式追加 LIKE 条件（结尾 - LIKE '%value'）
        /// </summary>
        public static DataQuery EndsWith<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, $"%{value}");
        }

        /// <summary>
        /// 链式追加 IN 条件
        /// 用法：FastRead.Query&lt;User&gt;().In&lt;User&gt;(u => u.Department, new[] { "IT", "HR" })
        /// </summary>
        public static DataQuery In<T>(this DataQuery query, Expression<Func<T, object>> field, IEnumerable<object> values) where T : class, new()
        {
            if (field == null || values == null)
                return query;

            query.EntityType = typeof(T);
            var fieldName = GetMemberName(field);
            var paramList = new List<DbParameter>();
            var placeholders = new List<string>();
            var index = 0;
            foreach (var v in values)
            {
                var paramName = $"@in_{query.ChainedConditions.Count}_{index}";
                paramList.Add(GetDbParameter(query, paramName, v?.ToString() ?? ""));
                placeholders.Add(paramName);
                index++;
            }
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = $"{fieldName} in ({string.Join(",", placeholders)})",
                Param = paramList
            });

            return query;
        }

        /// <summary>
        /// 链式追加 BETWEEN 条件
        /// 用法：FastRead.Query&lt;User&gt;().Between&lt;User&gt;(u => u.Age, 18, 65)
        /// </summary>
        public static DataQuery Between<T>(this DataQuery query, Expression<Func<T, object>> field, object start, object end) where T : class, new()
        {
            if (field == null)
                return query;

            query.EntityType = typeof(T);
            var fieldName = GetMemberName(field);
            var startParam = GetDbParameter(query, $"@btw_{query.ChainedConditions.Count}_s", start?.ToString() ?? "");
            var endParam = GetDbParameter(query, $"@btw_{query.ChainedConditions.Count}_e", end?.ToString() ?? "");
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = $"{fieldName} between {startParam.ParameterName} and {endParam.ParameterName}",
                Param = new List<DbParameter> { startParam, endParam }
            });

            return query;
        }

        private static DbParameter GetDbParameter(DataQuery query, string name, object value)
        {
            var dbParam = DbProviderFactories.GetFactory(query.Config.ProviderName).CreateParameter();
            dbParam.ParameterName = name;
            dbParam.Value = value ?? DBNull.Value;
            return dbParam;
        }

        /// <summary>
        /// 从表达式中获取成员名称
        /// </summary>
        private static string GetMemberName<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;

            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                return unaryMember.Member.Name;

            throw new ArgumentException("表达式必须是成员访问表达式");
        }

        /// <summary>
        /// 查询left join
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="item"></param>
        /// <param name="predicate"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery LeftJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false)
        {
            return JoinType("left join", item, predicate, field);
        }

        /// <summary>
        /// 查询right join
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="item"></param>
        /// <param name="predicate"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery RightJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new()
        {
            return JoinType("right join", item, predicate, field);
        }

        /// <summary>
        /// 查询inner join
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="item"></param>
        /// <param name="predicate"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery InnerJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new()
        {
            return JoinType("inner join", item, predicate, field);
        }

        /// <summary>
        /// 查询order by
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery OrderBy<T>(this DataQuery item, Expression<Func<T, object>> field, bool isDesc = true)
        {
            var orderBy = BaseField.OrderBy<T>(field, item.Config, isDesc);
            item.OrderBy.AddRange(orderBy);
            return item;
        }

        /// <summary>
        /// 查询group by
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery GroupBy<T>(this DataQuery item, Expression<Func<T, object>> field)
        {
            var groupBy = BaseField.GroupBy<T>(field, item.Config);
            item.GroupBy.AddRange(groupBy);
            return item;
        }

        /// <summary>
        /// 查询take
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DataQuery Take(this DataQuery item, int i)
        {
            item.Take = i;
            return item;
        }


        /// <summary>
        /// 返回list
        /// </summary>
        public static List<T> ToList<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return ExecuteQueryTemplate<List<T>, DataReturn<T>>(
                item, db, isOutSql,
                (ctx, q) => ctx.GetList<T>(q),
                r => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.list,
                r => r.sql);
        }

        /// <summary>
        /// 返回list asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<List<T>> ToListAsy<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToList<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<List<T>> ToLazyList<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<List<T>>(() => ToList<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<list> asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<List<T>>> ToLazyListAsy<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<T>>(() => ToList<T>(item, db, isOutSql)));
        }


        /// <summary>
        /// 返回json
        /// </summary>
        public static string ToJson(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return ExecuteQueryTemplate<string, DataReturn>(
                item, db, isOutSql,
                (ctx, q) => ctx.GetJson(q),
                r => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.Json,
                r => r.Sql);
        }

        /// <summary>
        /// 返回json asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<string> ToJsonAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToJson(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<json>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<string> ToLazyJson(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToJson(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<json> asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<string>> ToLazyJsonAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => AsyncHelper.ToLazy(() => ToJson(item, db, isOutSql)));
        }


        /// <summary>
        /// 返回item
        /// </summary>
        public static T ToItem<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return ExecuteQueryTemplate<T, DataReturn<T>>(
                item, db, isOutSql,
                (ctx, q) => ctx.GetList<T>(q),
                r => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.item,
                r => r.sql,
                () => item.Take = 1);
        }

        /// <summary>
        /// 返回item asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<T> ToItemAsy<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToItem<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回Lazy<item>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<T> ToLazyItem<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.ToLazy(() => ToItem<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回Lazy<item> asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<T>> ToLazyItemAsy<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => AsyncHelper.ToLazy(() => ToItem<T>(item, db, isOutSql)));
        }


        /// <summary>
        /// 返回条数
        /// </summary>
        public static int ToCount(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return ExecuteQueryTemplate<int, DataReturn>(
                item, db, isOutSql,
                (ctx, q) => ctx.GetCount(q),
                r => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.Count,
                r => r.Sql);
        }

        /// <summary>
        /// 返回条数 asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<int> ToCountAsy<T, T1>(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToCount(item, db, isOutSql));
        }


        /// <summary>
        /// 返回分页
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static PageResult<T> ToPage<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.pageResult;

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = tempDb.GetPage<T>(item, pModel);
                }
            }
            else
                result = db.GetPage<T>(item, pModel);

            stopwatch.Stop();

            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, result.sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.pageResult;
        }

        /// <summary>
        /// 返回分页 asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Task<PageResult<T>> ToPageAsync<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToPage<T>(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页lazy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Lazy<PageResult<T>> ToLazyPage<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<PageResult<T>>(() => ToPage<T>(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页lazy asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Task<Lazy<PageResult<T>>> ToLazyPageAsync<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<PageResult<T>>(() => ToPage<T>(item, pModel, db, isOutSql)));
        }


        /// <summary>
        /// 返回分页Dictionary<string, object>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static PageResult ToPage(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.PageResult;

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = tempDb.GetPage(item, pModel);
                }
            }
            else
                result = db.GetPage(item, pModel);

            stopwatch.Stop();

            // Check per-query SQL log setting, then global setting, then per-database setting
            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, result.Sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.PageResult;
        }

        /// <summary>
        /// 返回分页Dictionary<string, object> asy
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Task<PageResult> ToPageAsync(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToPage(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页Dictionary<string, object> lazy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Lazy<PageResult> ToLazyPage(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToPage(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页Dictionary<string, object> lazy asy
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pModel"></param>
        /// <returns></returns>
        public static Task<Lazy<PageResult>> ToLazyPageAsync(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => AsyncHelper.ToLazy(() => ToPage(item, pModel, db, isOutSql)));
         }

        /// <summary>
        /// 分页查询（简化API）
        /// 返回格式：{ Total, TotalPages, Page, PageSize, Data }
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">查询条件</param>
        /// <param name="page">页码（从 1 开始）</param>
        /// <param name="pageSize">每页条数</param>
        /// <param name="db">数据库上下文（可选）</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页结果</returns>
        public static PaginationResult<T> ToPagination<T>(this DataQuery item, int page, int pageSize, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var pModel = new PageModel
            {
                PageId = page,
                PageSize = pageSize
            };

            var pageResult = ToPage<T>(item, pModel, db, isOutSql);
            var total = pageResult.pModel.TotalRecord;

            return new PaginationResult<T>
            {
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Page = page,
                PageSize = pageSize,
                Data = pageResult.list
            };
        }

        /// <summary>
        /// 分页查询（简化API）异步版本
        /// </summary>
        public static Task<PaginationResult<T>> ToPaginationAsync<T>(this DataQuery item, int page, int pageSize, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return Task.Run(() => ToPagination<T>(item, page, pageSize, db, isOutSql));
        }

        /// <summary>
        /// 分页查询（使用 PaginationRequest 对象）
        /// </summary>
        public static PaginationResult<T> ToPagination<T>(this DataQuery item, PaginationRequest request, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return ToPagination<T>(item, request.Page, request.PageSize, db, isOutSql);
        }

        /// <summary>
        /// 分页查询（使用 PaginationRequest 对象）异步版本
        /// </summary>
        public static Task<PaginationResult<T>> ToPaginationAsync<T>(this DataQuery item, PaginationRequest request, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return Task.Run(() => ToPagination<T>(item, request, db, isOutSql));
        }

        /// <summary>
        /// 分页查询（返回字典格式）
        /// </summary>
        public static PaginationResult ToPagination(this DataQuery item, int page, int pageSize, DataContext db = null, bool isOutSql = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var pModel = new PageModel
            {
                PageId = page,
                PageSize = pageSize
            };

            var pageResult = ToPage(item, pModel, db, isOutSql);
            return PaginationResult.FromPageResult(pageResult, page, pageSize);
        }

        /// <summary>
        /// 分页查询（返回字典格式）异步版本
        /// </summary>
        public static Task<PaginationResult> ToPaginationAsync(this DataQuery item, int page, int pageSize, DataContext db = null, bool isOutSql = false)
        {
            return Task.Run(() => ToPagination(item, page, pageSize, db, isOutSql));
        }


        /// <summary>
        /// 执行sql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static List<T> ExecuteSql<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key))
                {
                    result = tempDb.ExecuteSql<T>(sql, param);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.ExecuteSql<T>(sql, param);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.list;
        }

        /// <summary>
        /// 执行sql asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Task<List<T>> ExecuteSqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ExecuteSql<T>(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Lazy<List<T>> ExecuteLazySql<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<List<T>>(() => ExecuteSql<T>(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Task<Lazy<List<T>>> ExecuteLazySqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<T>>(() => ExecuteSql<T>(sql, param, db, key, isOutSql)));
        }


        /// <summary>
        /// 返回List<Dictionary<string, object>>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> ToDics(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.DicList;

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = tempDb.GetDic(item);
                }
            }
            else
                result = db.GetDic(item);

            stopwatch.Stop();

            // Check per-query SQL log setting, then global setting, then per-database setting
            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, result.Sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.DicList;
        }

        /// <summary>
        /// 返回List<Dictionary<string, object>> asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<List<Dictionary<string, object>>> ToDicsAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDics(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<List<Dictionary<string, object>>>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<List<Dictionary<string, object>>> ToLazyDics(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return new Lazy<List<Dictionary<string, object>>>(() => ToDics(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<List<Dictionary<string, object>>> asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<List<Dictionary<string, object>>>> ToLazyDicsAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<Dictionary<string, object>>>(() => ToDics(item, db, isOutSql)));
        }


        /// <summary>
        /// Dictionary<string, object>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ToDic(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.Dic;

            stopwatch.Start();
            item.Take = 1;

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = tempDb.GetDic(item);
                }
            }
            else
                result = db.GetDic(item);

            stopwatch.Stop();

            // Check per-query SQL log setting, then global setting, then per-database setting
            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, result.Sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.Dic;
        }

        /// <summary>
        /// Dictionary<string, object> asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Dictionary<string, object>> ToDicAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDic(item, db, isOutSql));
        }

        /// <summary>
        /// Dictionary<string, object>>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<Dictionary<string, object>> ToLazyDic(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return new Lazy<Dictionary<string, object>>(() => ToDic(item, db, isOutSql));
        }

        /// <summary>
        /// Dictionary<string, object> asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<Dictionary<string, object>>> ToLazyDicAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<Dictionary<string, object>>(() => ToDic(item, db, isOutSql)));
        }


        /// <summary>
        /// DataTable
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static DataTable ToDataTable(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.Table;

            stopwatch.Start();
            item.Take = 1;

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = tempDb.GetDataTable(item);
                }
            }
            else
                result = db.GetDataTable(item);

            stopwatch.Stop();

            // Check per-query SQL log setting, then global setting, then per-database setting
            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, result.Sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.Table;
        }

        /// <summary>
        /// DataTable asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<DataTable> ToDataTableAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDataTable(item, db, isOutSql));
        }

        /// <summary>
        /// DataTable lazy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Lazy<DataTable> ToLazyDataTable(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToDataTable(item, db, isOutSql));
        }

        /// <summary>
        /// DataTable lazy asy
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Task<Lazy<DataTable>> ToLazyDataTableAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => AsyncHelper.ToLazy(() => ToDataTable(item, db, isOutSql)));
        }


        /// <summary>
        /// 执行sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> ExecuteSql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            key = key ?? FastDb.CurrentKey;
            ConfigModel config = null;
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key))
                {
                    result = tempDb.ExecuteSqlList(sql, param, false);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.ExecuteSqlList(sql, param, false);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.DicList;
        }

        /// <summary>
        /// 执行sql asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Task<List<Dictionary<string, object>>> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ExecuteSql(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Lazy<List<Dictionary<string, object>>> ExecuteLazySql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return new Lazy<List<Dictionary<string, object>>>(() => ExecuteSql(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy asy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Task<Lazy<List<Dictionary<string, object>>>> ExecuteLazySqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<Dictionary<string, object>>>(() => ExecuteSql(sql, param, db, key, isOutSql)));
        }

        #region 私有模板方法

        /// <summary>
        /// 查询执行模板 - 提取公共逻辑
        /// </summary>
        private static TResult ExecuteQueryTemplate<TResult, TReturn>(
            DataQuery item,
            DataContext db,
            bool isOutSql,
            Func<DataContext, DataQuery, TReturn> execute,
            Func<TReturn, bool> predicateFailed,
            Func<TReturn, TResult> defaultResult,
            Func<TReturn, string> getSql,
            Action preExecute = null)
        {
            var stopwatch = new Stopwatch();
            var result = default(TReturn);

            if (predicateFailed(result))
                return defaultResult(result);

            preExecute?.Invoke();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(item.Key))
                {
                    result = execute(tempDb, item);
                }
            }
            else
                result = execute(db, item);

            stopwatch.Stop();

            var shouldLog = item.IsSqlLogEnabled || FastDb.EnableSqlLog || item.Config.IsOutSql || isOutSql;
            DbLog.LogSql(shouldLog, getSql(result), item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return defaultResult(result);
        }

        #endregion
    }
}
