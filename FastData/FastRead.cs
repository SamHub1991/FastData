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
#if !NETFRAMEWORK
using FastData.Queue;
#endif

namespace FastData
{
    /// <summary>
    /// FastData 读取操作（静态方法）
    /// 
    /// 职责：
    /// 1. LINQ 查询构建（Query / Where / And / Or / Like / In / Between）
    /// 2. 多表联查（LeftJoin / RightJoin / InnerJoin）
    /// 3. 查询结果转换（ToList / ToItem / ToCount / ToPage / ToJson）
    /// 4. 原生 SQL 查询（ExecuteSql）
    /// 5. 延迟加载查询（ToLazyList / ToLazyItem）
    /// 
    /// 使用示例：
    /// <code>
    /// // ========== LINQ 查询 ==========
    /// 
    /// // 简单查询
    /// var users = FastRead.Query&lt;User&gt;(u =&gt; u.Age &gt; 18).ToList();
    /// 
    /// // 带条件、排序、分页
    /// var page = FastRead.Query&lt;User&gt;(u =&gt; u.Age &gt; 18)
    ///     .Where&lt;User&gt;(u =&gt; u.Name.Contains("张"))
    ///     .OrderBy&lt;User&gt;(u =&gt; u.CreateTime, isDesc: true)
    ///     .Take(100)
    ///     .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });
    /// 
    /// // 指定字段查询
    /// var users = FastRead.Query&lt;User&gt;(u =&gt; u.Age &gt; 18, u =&gt; new { u.Id, u.Name }).ToList();
    /// 
    /// // 多表联查
    /// var list = FastRead.Query&lt;Order&gt;(o =&gt; true)
    ///     .InnerJoin&lt;Order, User&gt;((o, u) =&gt; o.UserId == u.Id)
    ///     .Select&lt;Order, dynamic&gt;((o, u) =&gt; new { Order = o, UserName = u.Name })
    ///     .ToList();
    /// 
    /// // ========== 原生 SQL ==========
    /// 
    /// var param = new[] { new SqlParameter("@Age", 18) };
    /// var users = FastRead.ExecuteSql&lt;User&gt;("SELECT * FROM Users WHERE Age &gt; @Age", param);
    /// var dicts = FastRead.ExecuteSql("SELECT * FROM Users WHERE Age &gt; @Age", param);
    /// 
    /// // ========== 绑定 Key ==========
    /// 
    /// // 方式1：使用 Use 方法
    /// var db1 = FastRead.Use("db1");
    /// var users = db1.Query&lt;User&gt;(u =&gt; true).ToList();
    /// 
    /// // 方式2：使用 FastDataClient（推荐）
    /// var client = new FastDataClient("db1");
    /// var users = client.Query&lt;User&gt;(u =&gt; true).ToList();
    /// </code>
    /// 
    /// 相关类：
    /// - FastReadDb: 绑定 Key 的读取操作（实例方法）
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - FastWrite: 写入操作
    /// - FastMap: XML 映射操作
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
        /// <typeparam name="T">第一个表类型</typeparam>
        /// <typeparam name="T1">第二个表类型</typeparam>
        /// <param name="joinType">join类型</param>
        /// <param name="item">数据查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <param name="field">字段表达式</param>
        /// <param name="isDblink">是否跨库查询</param>
        /// <returns>数据查询对象</returns>
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
        /// <param name="key">数据库连接键</param>
        /// <returns>查询构建器对象</returns>
        public static DataQuery<T> Query<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, string key = null, string dbFile = "db.config") where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = FastDb.GetProjectName();
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
            var paramName = string.Format("@like_{0}", query.ChainedConditions.Count);
            var dbParam = GetDbParameter(query, paramName, value);
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} like {1}", fieldName, paramName),
                Param = new List<DbParameter> { dbParam }
            });

            return query;
        }

        /// <summary>
        /// 链式追加 LIKE 条件（包含 - LIKE '%value%'）
        /// </summary>
        public static DataQuery Contains<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, string.Format("%{0}%", value));
        }

        /// <summary>
        /// 链式追加 LIKE 条件（开头 - LIKE 'value%'）
        /// </summary>
        public static DataQuery StartsWith<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, string.Format("{0}%", value));
        }

        /// <summary>
        /// 链式追加 LIKE 条件（结尾 - LIKE '%value'）
        /// </summary>
        public static DataQuery EndsWith<T>(this DataQuery query, Expression<Func<T, object>> field, string value) where T : class, new()
        {
            return Like(query, field, string.Format("%{0}", value));
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
                var paramName = string.Format("@in_{0}_{1}", query.ChainedConditions.Count, index);
                paramList.Add(GetDbParameter(query, paramName, v?.ToString() ?? ""));
                placeholders.Add(paramName);
                index++;
            }
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} in ({1})", fieldName, string.Join(",", placeholders)),
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
            var startParam = GetDbParameter(query, string.Format("@btw_{0}_s", query.ChainedConditions.Count), start?.ToString() ?? "");
            var endParam = GetDbParameter(query, string.Format("@btw_{0}_e", query.ChainedConditions.Count), end?.ToString() ?? "");
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} between {1} and {2}", fieldName, startParam.ParameterName, endParam.ParameterName),
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
        /// <typeparam name="T">第一个表类型</typeparam>
        /// <typeparam name="T1">第二个表类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <param name="field">字段表达式</param>
        /// <param name="isDblink">是否跨库查询</param>
        /// <returns>数据查询对象</returns>
        public static DataQuery LeftJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false)
        {
            return JoinType("left join", item, predicate, field);
        }

        /// <summary>
        /// 查询right join
        /// </summary>
        /// <typeparam name="T">第一个表类型</typeparam>
        /// <typeparam name="T1">第二个表类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <param name="field">字段表达式</param>
        /// <param name="isDblink">是否跨库查询</param>
        /// <returns>数据查询对象</returns>
        public static DataQuery RightJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new()
        {
            return JoinType("right join", item, predicate, field);
        }

        /// <summary>
        /// 查询inner join
        /// </summary>
        /// <typeparam name="T">第一个表类型</typeparam>
        /// <typeparam name="T1">第二个表类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="predicate">条件表达式</param>
        /// <param name="field">字段表达式</param>
        /// <param name="isDblink">是否跨库查询</param>
        /// <returns>数据查询对象</returns>
        public static DataQuery InnerJoin<T, T1>(this DataQuery item, Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new()
        {
            return JoinType("inner join", item, predicate, field);
        }

        /// <summary>
        /// 查询order by
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="field">排序字段表达式</param>
        /// <param name="isDesc">是否降序</param>
        /// <returns>数据查询对象</returns>
        public static DataQuery OrderBy<T>(this DataQuery item, Expression<Func<T, object>> field, bool isDesc = true)
        {
            var orderBy = BaseField.OrderBy<T>(field, item.Config, isDesc);
            item.OrderBy.AddRange(orderBy);
            return item;
        }

        /// <summary>
        /// 查询group by
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="field">分组字段表达式</param>
        /// <returns>数据查询对象</returns>
        public static DataQuery GroupBy<T>(this DataQuery item, Expression<Func<T, object>> field)
        {
            var groupBy = BaseField.GroupBy<T>(field, item.Config);
            item.GroupBy.AddRange(groupBy);
            return item;
        }

        /// <summary>
        /// 查询take
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="i">限制返回的记录数</param>
        /// <returns>数据查询对象</returns>
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
                () => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.List,
                r => r.Sql);
        }

        /// <summary>
        /// 返回list asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>查询结果列表任务</returns>
        public static Task<List<T>> ToListAsync<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToList<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<list>
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的查询结果列表</returns>
        public static Lazy<List<T>> ToLazyList<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<List<T>>(() => ToList<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<list> asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的查询结果列表任务</returns>
        public static Task<Lazy<List<T>>> ToLazyListAsync<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<T>>(() => ToList<T>(item, db, isOutSql)));
        }


        /// <summary>
        /// 返回json
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>JSON字符串</returns>
        public static string ToJson(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return ExecuteQueryTemplate<string, DataReturn>(
                item, db, isOutSql,
                (ctx, q) => ctx.GetJson(q),
                () => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.Json,
                r => r.Sql);
        }

        /// <summary>
        /// 返回json asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>JSON字符串任务</returns>
        public static Task<string> ToJsonAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToJson(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<json>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的JSON字符串</returns>
        public static Lazy<string> ToLazyJson(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToJson(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<json> asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的JSON字符串任务</returns>
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
                () => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.Item,
                r => r.Sql,
                () => item.Take = 1);
        }

        /// <summary>
        /// 返回item asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>单条记录任务</returns>
        public static Task<T> ToItemAsync<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToItem<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回Lazy<item>
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的单条记录</returns>
        public static Lazy<T> ToLazyItem<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.ToLazy(() => ToItem<T>(item, db, isOutSql));
        }

        /// <summary>
        /// 返回Lazy<item> asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的单条记录任务</returns>
        public static Task<Lazy<T>> ToLazyItemAsync<T>(this DataQuery item, DataContext db = null, bool isOutSql = false) where T : class, new()
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
                () => item.Predicate.Exists(a => a.IsSuccess == false),
                r => r.Count,
                r => r.Sql);
        }

        /// <summary>
        /// 返回条数 asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>记录总数任务</returns>
        public static Task<int> ToCountAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToCount(item, db, isOutSql));
        }


        /// <summary>
        /// 返回分页
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页查询结果</returns>
        public static PageResult<T> ToPage<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (item.Predicate.Exists(a => a.IsSuccess == false))
                return result.PageResult;

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
            DbLog.LogSql(shouldLog, result.Sql, item.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.PageResult;
        }

        /// <summary>
        /// 返回分页 asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页查询结果任务</returns>
        public static Task<PageResult<T>> ToPageAsync<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ToPage<T>(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页lazy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的分页查询结果</returns>
        public static Lazy<PageResult<T>> ToLazyPage<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<PageResult<T>>(() => ToPage<T>(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页lazy asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的分页查询结果任务</returns>
        public static Task<Lazy<PageResult<T>>> ToLazyPageAsync<T>(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<PageResult<T>>(() => ToPage<T>(item, pModel, db, isOutSql)));
        }


        /// <summary>
        /// 返回分页Dictionary<string, object>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页查询结果（字典格式）</returns>
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
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页查询结果任务（字典格式）</returns>
        public static Task<PageResult> ToPageAsync(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToPage(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页Dictionary<string, object> lazy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的分页查询结果（字典格式）</returns>
        public static Lazy<PageResult> ToLazyPage(this DataQuery item, PageModel pModel, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToPage(item, pModel, db, isOutSql));
        }

        /// <summary>
        /// 返回分页Dictionary<string, object> lazy asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="pModel">分页模型</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的分页查询结果任务（字典格式）</returns>
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
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>查询结果列表</returns>
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
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.List;
        }

        /// <summary>
        /// 执行sql asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>查询结果列表任务</returns>
        public static Task<List<T>> ExecuteSqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => ExecuteSql<T>(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的查询结果列表</returns>
        public static Lazy<List<T>> ExecuteLazySql<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return new Lazy<List<T>>(() => ExecuteSql<T>(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy asy
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的查询结果列表任务</returns>
        public static Task<Lazy<List<T>>> ExecuteLazySqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<T>>(() => ExecuteSql<T>(sql, param, db, key, isOutSql)));
        }


        /// <summary>
        /// 返回List<Dictionary<string, object>>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>字典列表查询结果</returns>
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
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>字典列表查询结果任务</returns>
        public static Task<List<Dictionary<string, object>>> ToDicsAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDics(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<List<Dictionary<string, object>>>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的字典列表</returns>
        public static Lazy<List<Dictionary<string, object>>> ToLazyDics(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return new Lazy<List<Dictionary<string, object>>>(() => ToDics(item, db, isOutSql));
        }

        /// <summary>
        /// 返回lazy<List<Dictionary<string, object>>> asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的字典列表任务</returns>
        public static Task<Lazy<List<Dictionary<string, object>>>> ToLazyDicsAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<Dictionary<string, object>>>(() => ToDics(item, db, isOutSql)));
        }


        /// <summary>
        /// Dictionary<string, object>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>单条字典记录</returns>
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
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>单条字典记录任务</returns>
        public static Task<Dictionary<string, object>> ToDicAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDic(item, db, isOutSql));
        }

        /// <summary>
        /// Dictionary<string, object>>
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的单条字典记录</returns>
        public static Lazy<Dictionary<string, object>> ToLazyDic(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return new Lazy<Dictionary<string, object>>(() => ToDic(item, db, isOutSql));
        }

        /// <summary>
        /// Dictionary<string, object> asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的单条字典记录任务</returns>
        public static Task<Lazy<Dictionary<string, object>>> ToLazyDicAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<Dictionary<string, object>>(() => ToDic(item, db, isOutSql)));
        }


        /// <summary>
        /// DataTable
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>DataTable查询结果</returns>
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
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>DataTable查询结果任务</returns>
        public static Task<DataTable> ToDataTableAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ToDataTable(item, db, isOutSql));
        }

        /// <summary>
        /// DataTable lazy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的DataTable查询结果</returns>
        public static Lazy<DataTable> ToLazyDataTable(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.ToLazy(() => ToDataTable(item, db, isOutSql));
        }

        /// <summary>
        /// DataTable lazy asy
        /// </summary>
        /// <param name="item">数据查询对象</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的DataTable查询结果任务</returns>
        public static Task<Lazy<DataTable>> ToLazyDataTableAsync(this DataQuery item, DataContext db = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => AsyncHelper.ToLazy(() => ToDataTable(item, db, isOutSql)));
        }


        /// <summary>
        /// 执行sql
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>字典列表查询结果</returns>
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
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>字典列表查询结果任务</returns>
        public static Task<List<Dictionary<string, object>>> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ExecuteSql(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的字典列表查询结果</returns>
        public static Lazy<List<Dictionary<string, object>>> ExecuteLazySql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return new Lazy<List<Dictionary<string, object>>>(() => ExecuteSql(sql, param, db, key, isOutSql));
        }

        /// <summary>
        /// 执行sql lazy asy
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">SQL参数列表</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库连接键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>延迟加载的字典列表查询结果任务</returns>
        public static Task<Lazy<List<Dictionary<string, object>>>> ExecuteLazySqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => new Lazy<List<Dictionary<string, object>>>(() => ExecuteSql(sql, param, db, key, isOutSql)));
        }

        #region 私有模板方法

        /// <summary>
        /// 查询执行模板 - 提取公共逻辑
        /// predicateFailed 在查询执行前验证 Predicate 列表，避免无效查询
        /// </summary>
        private static TResult ExecuteQueryTemplate<TResult, TReturn>(
            DataQuery item,
            DataContext db,
            bool isOutSql,
            Func<DataContext, DataQuery, TReturn> execute,
            Func<bool> predicateFailed,
            Func<TReturn, TResult> defaultResult,
            Func<TReturn, string> getSql,
            Action preExecute = null)
        {
            if (predicateFailed())
                return default(TResult);

            preExecute?.Invoke();

            var stopwatch = new Stopwatch();
            TReturn result;

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

            if (result == null)
                return default(TResult);

            return defaultResult(result);
        }

        #endregion
    }
}
