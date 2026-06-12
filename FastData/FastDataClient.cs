using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastData.Base;
using FastData.Config;
using FastData.ConnectionPool;
using FastData.Context;
using FastData.Core;
using FastData.Model;
using FastData.Property;
using FastUntility.Page;
#if !NETFRAMEWORK
using FastData.Queue;
using NewLife.Caching;
#endif

namespace FastData
{
    /// <summary>
    /// FastData 统一客户端门面（推荐入口）
    /// 
    /// 绑定数据库 Key 的实例级 API，覆盖查询、写入、Map 操作、分片、队列等全部功能。
    /// 推荐使用 FastDataClient 替代 FastRead.Use() / FastWrite.Use() 的静态门面方式。
    /// 
    /// 使用方式：
    /// <code>
    /// var client = new FastDataClient("db1");
    /// var users = client.Query&lt;User&gt;(u => u.Age > 18).ToList();
    /// </code>
    /// </summary>
    public sealed class FastDataClient : IDisposable
    {
        private readonly string _key;
        private bool _enableSqlLog;
#if !NETFRAMEWORK
        private readonly ConnectionPoolConfig _poolConfig;
        private readonly FullRedis _redis;
        private readonly ResilientWriteExecutor _resilientExecutor;
#endif

        /// <summary>
        /// 获取数据库 Key
        /// </summary>
        public string Key => _key;

        /// <summary>
        /// 创建 FastDataClient 实例
        /// </summary>
        /// <param name="key">数据库配置的 Key 名称（对应 db.config 中的 Key）</param>
        /// <exception cref="ArgumentException">key 为空时抛出</exception>
        /// <example>
        /// <code>
        /// var client = new FastDataClient("db1");
        /// var users = client.Query&lt;User&gt;(u => u.Age > 18).ToList();
        /// </code>
        /// </example>
        public FastDataClient(string key)
        {
            _key = ResolveKey(key);
        }

#if !NETFRAMEWORK
        /// <summary>
        /// 创建带连接池配置和消息队列支持的 FastDataClient 实例
        /// </summary>
        /// <param name="key">数据库配置的 Key 名称</param>
        /// <param name="poolConfig">连接池配置</param>
        /// <param name="redis">Redis 实例（用于消息队列降级）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <example>
        /// <code>
        /// var redis = new FullRedis { Server = "127.0.0.1:6379", Db = 7 };
        /// var poolConfig = new ConnectionPoolConfig { MinPoolSize = 2, MaxPoolSize = 10 };
        /// var client = new FastDataClient("db1", poolConfig, redis);
        /// 
        /// // 写入操作会自动降级到消息队列
        /// var result = client.Add(user);
        /// </code>
        /// </example>
        public FastDataClient(string key, ConnectionPoolConfig poolConfig, FullRedis redis = null, int maxRetries = 3)
        {
            key = ResolveKey(key);
            _key = key;
            _poolConfig = poolConfig;
            _redis = redis;
            
            if (redis != null)
            {
                _resilientExecutor = new ResilientWriteExecutor(
                    databaseKey: key,
                    poolConfig: poolConfig,
                    redis: redis,
                    maxRetries: maxRetries);
            }
        }
#endif

        private static string ResolveKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = FastDb.CurrentKey ?? FastDataConfig.GetConfig()?.Key;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("数据库 Key 不能为空，且未找到默认数据库配置", nameof(key));

            return key;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
#if !NETFRAMEWORK
            _resilientExecutor?.Dispose();
#endif
        }

        /// <summary>
        /// 启用当前客户端的 SQL 日志（覆盖全局设置）
        /// </summary>
        /// <returns>当前客户端实例（支持链式调用）</returns>
        /// <example>
        /// <code>
        /// var client = new FastDataClient("db1");
        /// client.EnableSqlLog().Add(user);  // 链式调用
        /// </code>
        /// </example>
        public FastDataClient EnableSqlLog()
        {
            _enableSqlLog = true;
            return this;
        }

        #region 查询操作（Query）

        /// <summary>
        /// 创建 LINQ 查询
        /// 
        /// 支持链式调用：Where / And / Or / Like / In / Between / LeftJoin / RightJoin / InnerJoin / OrderBy / GroupBy / Take
        /// 终结方法：ToList / ToItem / ToCount / ToPage / ToJson
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件表达式</param>
        /// <param name="field">查询字段表达式（可选，默认全部）</param>
        /// <returns>查询对象，支持链式调用</returns>
        /// <example>
        /// <code>
        /// // 简单查询
        /// var users = client.Query&lt;User&gt;(u => u.Age > 18).ToList();
        /// 
        /// // 带分页
        /// var page = client.Query&lt;User&gt;(u => u.Age > 18)
        ///     .OrderBy&lt;User&gt;(u => u.CreateTime)
        ///     .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });
        /// 
        /// // 指定字段
        /// var users = client.Query&lt;User&gt;(u => u.Age > 18, u => new { u.Id, u.Name }).ToList();
        /// </code>
        /// </example>
        public DataQuery<T> Query<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null) where T : class, new()
        {
            if (predicate == null)
                predicate = _ => true;

            return FastRead.Query<T>(predicate, field, _key);
        }

        /// <summary>
        /// 查询列表。比 Query(...).ToList() 更短。
        /// </summary>
        public List<T> List<T>(Expression<Func<T, bool>> predicate = null, Expression<Func<T, object>> field = null) where T : class, new()
        {
            return Query(predicate, field).ToList();
        }

        /// <summary>
        /// 查询单条记录。不存在时返回 null。
        /// </summary>
        public T First<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null) where T : class, new()
        {
            return Query(predicate, field).FirstOrDefault();
        }

        /// <summary>
        /// 查询数量。
        /// </summary>
        public int Count<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            return Query(predicate).Count();
        }

        /// <summary>
        /// 分页查询。
        /// </summary>
        public PaginationResult<T> Page<T>(Expression<Func<T, bool>> predicate, int page, int pageSize, Expression<Func<T, object>> field = null) where T : class, new()
        {
            return Query(predicate, field).ToPagination<T>(page, pageSize);
        }

        /// <summary>
        /// 查询全部分页。
        /// </summary>
        public PaginationResult<T> Page<T>(int page, int pageSize, Expression<Func<T, object>> field = null) where T : class, new()
        {
            return Page<T>(null, page, pageSize, field);
        }

        /// <summary>
        /// 执行原生 SQL 查询，返回强类型列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sql">SQL 语句（支持参数化：@Name, :Name, ?Name）</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选，用于事务）</param>
        /// <returns>实体列表</returns>
        /// <example>
        /// <code>
        /// var param = new[] { new SqlParameter("@Age", 18) };
        /// var users = client.ExecuteSql&lt;User&gt;("SELECT * FROM Users WHERE Age > @Age", param);
        /// </code>
        /// </example>
        public List<T> ExecuteSql<T>(string sql, DbParameter[] param = null, DataContext db = null) where T : class, new()
        {
            return FastRead.ExecuteSql<T>(sql, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 原生 SQL 查询强类型列表。ExecuteSql 的短别名。
        /// </summary>
        public List<T> Sql<T>(string sql, DbParameter[] param = null, DataContext db = null) where T : class, new()
        {
            return ExecuteSql<T>(sql, param, db);
        }

        /// <summary>
        /// 执行原生 SQL 查询，返回强类型列表（异步）
        /// </summary>
        public Task<List<T>> ExecuteSqlAsync<T>(string sql, DbParameter[] param = null, DataContext db = null) where T : class, new()
        {
            return FastRead.ExecuteSqlAsync<T>(sql, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 原生 SQL 异步查询强类型列表。ExecuteSqlAsync 的短别名。
        /// </summary>
        public Task<List<T>> SqlAsync<T>(string sql, DbParameter[] param = null, DataContext db = null) where T : class, new()
        {
            return ExecuteSqlAsync<T>(sql, param, db);
        }

        /// <summary>
        /// 执行原生 SQL 查询，返回字典列表（适合动态列）
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>字典列表，key 为列名</returns>
        /// <example>
        /// <code>
        /// var results = client.ExecuteSql("SELECT Name, Age FROM Users WHERE Age > @Age", param);
        /// foreach (var row in results)
        /// {
        ///     Console.WriteLine($"{row["Name"]}: {row["Age"]}");
        /// }
        /// </code>
        /// </example>
        public List<Dictionary<string, object>> ExecuteSql(string sql, DbParameter[] param = null, DataContext db = null)
        {
            return FastRead.ExecuteSql(sql, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 原生 SQL 查询动态列。ExecuteSql 的短别名。
        /// </summary>
        public List<Dictionary<string, object>> Sql(string sql, DbParameter[] param = null, DataContext db = null)
        {
            return ExecuteSql(sql, param, db);
        }

        /// <summary>
        /// 执行原生 SQL 查询，返回字典列表（异步）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <returns>字典列表任务</returns>
        public Task<List<Dictionary<string, object>>> ExecuteSqlAsync(string sql, DbParameter[] param = null, DataContext db = null)
        {
            return FastRead.ExecuteSqlAsync(sql, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 原生 SQL 异步查询动态列。ExecuteSqlAsync 的短别名。
        /// </summary>
        public Task<List<Dictionary<string, object>>> SqlAsync(string sql, DbParameter[] param = null, DataContext db = null)
        {
            return ExecuteSqlAsync(sql, param, db);
        }

        #endregion

        #region Map 映射查询

        /// <summary>
        /// 执行 XML 映射查询，返回强类型列表
        /// 
        /// SQL 定义在 XML 映射文件中（SqlMap.config），通过 name 引用
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">XML 中定义的 SQL 名称</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>实体列表</returns>
        /// <example>
        /// <code>
        /// // XML 定义：
        /// // &lt;sql id="GetUsersByAge"&gt;
        /// //   SELECT * FROM Users WHERE Age &gt; :Age
        /// // &lt;/sql&gt;
        /// 
        /// var param = new[] { new OracleParameter(":Age", 18) };
        /// var users = client.MapQuery&lt;User&gt;("GetUsersByAge", param);
        /// </code>
        /// </example>
        public List<T> MapQuery<T>(string name, DbParameter[] param, DataContext db = null) where T : class, new()
        {
            return FastMap.Query<T>(name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射查询，返回强类型列表（异步）
        /// </summary>
        public Task<List<T>> MapQueryAsync<T>(string name, DbParameter[] param, DataContext db = null) where T : class, new()
        {
            return FastMap.QueryAsync<T>(name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射查询，返回字典列表
        /// </summary>
        /// <param name="name">Map名称</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <returns>字典列表</returns>
        public List<Dictionary<string, object>> MapQuery(string name, DbParameter[] param, DataContext db = null)
        {
            return FastMap.Query(name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射查询，返回字典列表（异步）
        /// </summary>
        /// <param name="name">Map名称</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <returns>字典列表任务</returns>
        public Task<List<Dictionary<string, object>>> MapQueryAsync(string name, DbParameter[] param, DataContext db = null)
        {
            return FastMap.QueryAsync(name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页参数</param>
        /// <param name="name">XML 中定义的 SQL 名称</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>分页结果</returns>
        /// <example>
        /// <code>
        /// var pageModel = new PageModel { PageIndex = 1, PageSize = 10 };
        /// var page = client.MapQueryPage&lt;User&gt;(pageModel, "GetUsers", param);
        /// Console.WriteLine($"总数: {page.TotalCount}, 数据: {page.DataList.Count}");
        /// </code>
        /// </example>
        public PageResult<T> MapQueryPage<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null) where T : class, new()
        {
            return FastMap.QueryPage<T>(pModel, name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射分页查询（异步）
        /// </summary>
        public Task<PageResult<T>> MapQueryPageAsync<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null) where T : class, new()
        {
            return FastMap.QueryPageAsync<T>(pModel, name, param, db, _key, _enableSqlLog);
        }

        #endregion

        #region 写入操作（Add/Update/Delete）

#if !NETFRAMEWORK
        /// <summary>
        /// 使用弹性写入执行器执行写入（支持消息队列降级）
        /// </summary>
        private WriteReturn ExecuteResilientWrite<T>(WriteOperationType operationType, T model) where T : class, new()
        {
            if (_resilientExecutor == null)
            {
                return operationType == WriteOperationType.Add
                    ? FastWrite.Add<T>(model, null, _key, _enableSqlLog)
                    : throw new NotSupportedException(string.Format("不支持的操作类型: {0}", operationType));
            }

            var operation = new WriteOperation
            {
                OperationType = operationType,
                EntityType = typeof(T).AssemblyQualifiedName,
                TableName = TableNameHelper.GetTableName<T>(_key),
                DatabaseKey = _key,
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(model)
            };
            
            var result = _resilientExecutor.ExecuteWrite(operation);
            return result.Success
                ? new WriteReturn { IsSuccess = true }
                : new WriteReturn { IsSuccess = false, Message = result.ErrorMessage };
        }
#endif

        /// <summary>
        /// 添加单条数据
        /// </summary>
        /// <typeparam name="T">实体类型（需有 TableAttribute 或类名即表名）</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="db">数据上下文（可选，用于事务）</param>
        /// <returns>写入结果（含自增 ID）</returns>
        /// <example>
        /// <code>
        /// var user = new User { Name = "张三", Age = 25 };
        /// var result = client.Add(user);
        /// if (result.IsSuccess)
        ///     Console.WriteLine($"新增成功，ID: {result.GetIdentity()}");
        /// </code>
        /// </example>
        public WriteReturn Add<T>(T model, DataContext db = null) where T : class, new()
        {
#if !NETFRAMEWORK
            if (_resilientExecutor != null && db == null)
            {
                return ExecuteResilientWrite(WriteOperationType.Add, model);
            }
#endif
            return FastWrite.Add<T>(model, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 添加单条数据（异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="AddAsync{T}"/> 代替。</para>
        /// <para>示例：<c>await client.AddAsync(user);</c></para>
        /// </remarks>
        [Obsolete("请使用 AddAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> AddAsy<T>(T model, DataContext db = null) where T : class, new()
        {
            return AddAsync(model, db);
        }

        /// <summary>
        /// 添加单条数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var user = new User { Name = "张三", Age = 25 };
        /// var result = await client.AddAsync(user);
        /// </code>
        /// </example>
        public Task<WriteReturn> AddAsync<T>(T model, DataContext db = null) where T : class, new()
        {
            return FastWrite.AddAsy<T>(model, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 批量添加数据（使用事务）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="isTrans">是否使用事务（默认 true）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var users = new List&lt;User&gt;
        /// {
        ///     new User { Name = "张三", Age = 25 },
        ///     new User { Name = "李四", Age = 30 }
        /// };
        /// var result = client.AddList(users);
        /// </code>
        /// </example>
        public WriteReturn AddList<T>(List<T> list, bool isTrans = true) where T : class, new()
        {
            return FastWrite.AddList<T>(list, _key, isTrans, _enableSqlLog);
        }

        /// <summary>
        /// 批量添加数据。AddList 的常用别名。
        /// </summary>
        public WriteReturn AddRange<T>(List<T> list, bool isTrans = true) where T : class, new()
        {
            return AddList(list, isTrans);
        }

        /// <summary>
        /// 批量添加数据（异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="AddListAsync{T}"/> 代替。</para>
        /// </remarks>
        [Obsolete("请使用 AddListAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> AddListAsy<T>(List<T> list, bool isTrans = true) where T : class, new()
        {
            return AddListAsync(list, isTrans);
        }

        /// <summary>
        /// 批量添加数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="isTrans">是否使用事务（默认 true）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var users = new List&lt;User&gt; { ... };
        /// var result = await client.AddListAsync(users);
        /// </code>
        /// </example>
        public Task<WriteReturn> AddListAsync<T>(List<T> list, bool isTrans = true) where T : class, new()
        {
            return FastWrite.AddListAsy<T>(list, _key, isTrans, _enableSqlLog);
        }

        /// <summary>
        /// 批量添加数据（异步）。AddListAsync 的常用别名。
        /// </summary>
        public Task<WriteReturn> AddRangeAsync<T>(List<T> list, bool isTrans = true) where T : class, new()
        {
            return AddListAsync(list, isTrans);
        }

        /// <summary>
        /// 更新数据（根据主键）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象（主键字段必须有值）</param>
        /// <param name="field">更新的字段表达式（可选，默认更新非主键字段）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var user = new User { Id = 1, Name = "新名字", Age = 26 };
        /// var result = client.Update(user);
        /// 
        /// // 只更新指定字段
        /// var result = client.Update(user, u => new { u.Name });
        /// </code>
        /// </example>
        public WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.Update<T>(model, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 更新数据（异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="UpdateAsync{T}(T, Expression{Func{T, object}}, DataContext)"/> 代替。</para>
        /// </remarks>
        [Obsolete("请使用 UpdateAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return UpdateAsync(model, field, db);
        }

        /// <summary>
        /// 更新数据（异步，根据主键）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象</param>
        /// <param name="field">更新的字段表达式（可选）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public Task<WriteReturn> UpdateAsync<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.UpdateAsy<T>(model, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 更新数据（根据条件）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象（包含要更新的值）</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">更新的字段表达式（可选）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var user = new User { Name = "新名字" };
        /// var result = client.Update(user, u => u.Age > 18);
        /// </code>
        /// </example>
        public WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.Update<T>(model, predicate, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 更新数据（根据条件，异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="UpdateAsync{T}(T, Expression{Func{T, bool}}, Expression{Func{T, object}}, DataContext)"/> 代替。</para>
        /// </remarks>
        [Obsolete("请使用 UpdateAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return UpdateAsync(model, predicate, field, db);
        }

        /// <summary>
        /// 更新数据（异步，根据条件）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象（包含要更新的值）</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">更新的字段表达式（可选）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public Task<WriteReturn> UpdateAsync<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.UpdateAsy<T>(model, predicate, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="field">更新的字段表达式（可选）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.UpdateList<T>(list, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 批量更新数据（异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="UpdateListAsync{T}"/> 代替。</para>
        /// </remarks>
        [Obsolete("请使用 UpdateListAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return UpdateListAsync(list, field, db);
        }

        /// <summary>
        /// 批量更新数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="field">更新的字段表达式（可选）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public Task<WriteReturn> UpdateListAsync<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null) where T : class, new()
        {
            return FastWrite.UpdateListAsy<T>(list, field, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 删除数据（根据条件）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var result = client.Delete&lt;User&gt;(u => u.Age < 18);
        /// </code>
        /// </example>
        public WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.Delete<T>(predicate, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 删除数据（根据条件，异步）
        /// </summary>
        /// <remarks>
        /// <para><strong>过时方法：</strong>请使用 <see cref="DeleteAsync{T}(Expression{Func{T, bool}}, DataContext)"/> 代替。</para>
        /// </remarks>
        [Obsolete("请使用 DeleteAsync 代替，本方法将在未来版本移除")]
        public Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return DeleteAsync(predicate, db);
        }

        /// <summary>
        /// 删除数据（异步，根据条件）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public Task<WriteReturn> DeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.DeleteAsy<T>(predicate, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 删除数据（根据主键）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体对象（主键字段必须有值）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        public WriteReturn Delete<T>(T model, DataContext db = null) where T : class, new()
        {
            return FastWrite.Delete<T>(model, db, _key, false, _enableSqlLog);
        }

        #endregion

        #region 批量操作（Bulk）

        /// <summary>
        /// 高性能批量插入（使用 SqlBulkCopy/MySqlBulkLoader 等）
        /// 
        /// 注意：不触发 Aop 事件，不支持事务回滚
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var users = Enumerable.Range(1, 10000)
        ///     .Select(i =&gt; new User { Name = $"User{i}", Age = i % 100 })
        ///     .ToList();
        /// var result = client.BulkInsert(users);
        /// Console.WriteLine($"插入 {result.GetIdentity()} 条");
        /// </code>
        /// </example>
        public WriteReturn BulkInsert<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsert<T>(list, db, _key);
        }

        /// <summary>
        /// 高性能批量插入（异步）
        /// </summary>
        public Task<WriteReturn> BulkInsertAsync<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsertAsync<T>(list, db, _key);
        }

        /// <summary>
        /// 批量更新（使用 SQL UPDATE ... WHERE IN）
        /// 
        /// 通过 IN 条件批量更新符合条件的记录，比逐条 Update 性能更高
        /// 注意：不触发 Aop 事件，不支持事务回滚
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表（包含要更新的值）</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="db">数据上下文（可选，用于事务）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var users = new List&lt;User&gt; { new User { Name = "批量更新" } };
        /// var result = client.BulkUpdate(users, u => u.Age > 18);
        /// </code>
        /// </example>
        public WriteReturn BulkUpdate<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkUpdate<T>(list, predicate, db, _key);
        }

        /// <summary>
        /// 批量更新（异步）
        /// </summary>
        public Task<WriteReturn> BulkUpdateAsync<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkUpdateAsync<T>(list, predicate, db, _key);
        }

        /// <summary>
        /// 批量删除（使用 SQL DELETE FROM ... WHERE IN）
        /// 
        /// 通过 IN 条件批量删除符合条件的记录，比逐条 Delete 性能更高
        /// 注意：不触发 Aop 事件，不支持事务回滚
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var result = client.BulkDelete&lt;User&gt;(u => u.Age < 18);
        /// </code>
        /// </example>
        public WriteReturn BulkDelete<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkDelete<T>(predicate, db, _key);
        }

        /// <summary>
        /// 批量删除（异步）
        /// </summary>
        public Task<WriteReturn> BulkDeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkDeleteAsync<T>(predicate, db, _key);
        }

        #endregion

        #region 原生 SQL 写入

        /// <summary>
        /// 执行原生 SQL 写入（INSERT/UPDATE/DELETE/存储过程）
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// var sql = "UPDATE Users SET Age = @Age WHERE Id = @Id";
        /// var param = new[] 
        /// {
        ///     new SqlParameter("@Age", 26),
        ///     new SqlParameter("@Id", 1)
        /// };
        /// var result = client.ExecuteSqlWrite(sql, param);
        /// </code>
        /// </example>
        public WriteReturn ExecuteSqlWrite(string sql, DbParameter[] param, DataContext db = null)
        {
            return FastWrite.ExecuteSql(sql, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 原生 SQL 写入。ExecuteSqlWrite 的短别名。
        /// </summary>
        public WriteReturn Exec(string sql, DbParameter[] param = null, DataContext db = null)
        {
            return ExecuteSqlWrite(sql, param, db);
        }

        /// <summary>
        /// 执行原生 SQL 写入（异步）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <returns>写入返回对象任务</returns>
        public Task<WriteReturn> ExecuteSqlWriteAsync(string sql, DbParameter[] param, DataContext db = null)
        {
            return FastWrite.ExecuteSqlAsync(sql, param, db, _key, _enableSqlLog);
        }

        #endregion

        #region Map 映射写入

        /// <summary>
        /// 执行 XML 映射写入
        /// 
        /// SQL 定义在 XML 映射文件中，通过 name 引用
        /// </summary>
        /// <param name="name">XML 中定义的 SQL 名称</param>
        /// <param name="param">SQL 参数</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// // XML 定义：
        /// // &lt;sql id="UpdateUserAge"&gt;
        /// //   UPDATE Users SET Age = :Age WHERE Id = :Id
        /// // &lt;/sql&gt;
        /// 
        /// var result = client.MapWrite("UpdateUserAge", param);
        /// </code>
        /// </example>
        public WriteReturn MapWrite(string name, DbParameter[] param, DataContext db = null)
        {
            return FastMap.Write(name, param, db, _key, _enableSqlLog);
        }

        /// <summary>
        /// 执行 XML 映射写入（异步）
        /// </summary>
        /// <param name="name">Map名称</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <returns>写入返回对象任务</returns>
        public Task<WriteReturn> MapWriteAsync(string name, DbParameter[] param, DataContext db = null)
        {
            return FastMap.WriteAsync(name, param, db, _key, _enableSqlLog);
        }

        #endregion

        #region CodeFirst

        /// <summary>
        /// CodeFirst 建表
        /// 
        /// 根据实体类的特性（Table/Column/Primary）自动创建数据库表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="isDropExists">是否删除已存在的表（默认 false）</param>
        /// <returns>写入结果</returns>
        /// <example>
        /// <code>
        /// [Table("Users")]
        /// public class User
        /// {
        ///     [Primary]
        ///     [Column(IsIdentity = true)]
        ///     public int Id { get; set; }
        ///     
        ///     [Column(Length = 50)]
        ///     public string Name { get; set; }
        ///     
        ///     public int Age { get; set; }
        /// }
        /// 
        /// var result = client.CodeFirst&lt;User&gt;();
        /// var result = client.CodeFirst&lt;User&gt;(isDropExists: true);  // 重建表
        /// </code>
        /// </example>
        public WriteReturn CodeFirst<T>(bool isDropExists = false) where T : class, new()
        {
            return FastWrite.CodeFirst<T>(_key, isDropExists);
        }

        /// <summary>
        /// CodeFirst 建表（异步）
        /// </summary>
        public Task<WriteReturn> CodeFirstAsync<T>(bool isDropExists = false) where T : class, new()
        {
            return FastWrite.CodeFirstAsync<T>(_key, isDropExists);
        }

        #endregion

        #region 分片操作（Sharding）

        /// <summary>
        /// 分片查询
        /// 
        /// 根据分片策略自动路由到对应的数据库/表进行查询，
        /// 适用于大数据量场景（按时间、ID 范围等分片）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="queryParams">分表查询参数（用于确定查询哪些分片表）</param>
        /// <returns>分片查询结果列表</returns>
        /// <example>
        /// <code>
        /// // 查询最近 3 个月的数据（自动路由到对应的分片表）
        /// var results = client.ShardQuery&lt;Order&gt;(
        ///     predicate: o =&gt; o.CreateTime &gt; DateTime.Now.AddMonths(-3),
        ///     queryParams: new Dictionary&lt;string, object&gt; { { "CreateTime", DateTime.Now.AddMonths(-3) } }
        /// );
        /// </code>
        /// </example>
        public List<T> ShardQuery<T>(Expression<Func<T, bool>> predicate, Dictionary<string, object> queryParams) where T : class, new()
        {
            return ShardingReadHelper.Query<T>(predicate, queryParams, _key);
        }

        /// <summary>
        /// 分片分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="queryParams">分表查询参数</param>
        /// <param name="pageIndex">页码（从 1 开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        public PageResult<T> ShardQueryPage<T>(Expression<Func<T, bool>> predicate, Dictionary<string, object> queryParams, int pageIndex, int pageSize) where T : class, new()
        {
            return ShardingReadHelper.QueryPage<T>(predicate, queryParams, pageIndex, pageSize, _key);
        }

        /// <summary>
        /// 分片添加
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <returns>写入结果</returns>
        public WriteReturn ShardAdd<T>(T entity) where T : class, new()
        {
            return ShardingWriteHelper.Add<T>(entity, _key);
        }

        /// <summary>
        /// 分片批量添加
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <returns>写入结果</returns>
        public WriteReturn ShardAddList<T>(List<T> entities) where T : class, new()
        {
            return ShardingWriteHelper.AddList<T>(entities, _key);
        }

        /// <summary>
        /// 分片删除
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="queryParams">分表查询参数（用于确定删除哪些分片表的数据）</param>
        /// <returns>写入结果</returns>
        public WriteReturn ShardDelete<T>(Expression<Func<T, bool>> predicate, Dictionary<string, object> queryParams) where T : class, new()
        {
            return ShardingWriteHelper.Delete<T>(predicate, queryParams, _key);
        }

        /// <summary>
        /// 分片更新
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象（包含要更新的值）</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">更新字段（可选）</param>
        /// <returns>写入结果</returns>
        public WriteReturn ShardUpdate<T>(T entity, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null) where T : class, new()
        {
            return ShardingWriteHelper.Update<T>(entity, predicate, field, _key);
        }

        #endregion

#if !NETFRAMEWORK
        /// <summary>
        /// 创建链式读取构建器（带消息队列支持）
        /// </summary>
        public FastReadQueueBuilder<T> ReadQueue<T>() where T : class, new()
        {
            return new FastReadQueueBuilder<T>(_key);
        }

        /// <summary>
        /// 创建链式写入构建器（带消息队列支持）
        /// </summary>
        public FastWriteQueueBuilder WriteQueue()
        {
            return new FastWriteQueueBuilder(_key);
        }

        /// <summary>
        /// 配置表级别的消息队列（泛型版本）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="config">队列配置</param>
        public void ConfigureQueue<T>(WriteBehindConfig config) where T : class
        {
            FastWrite.ConfigureQueue<T>(config);
        }

        /// <summary>
        /// 配置表级别的消息队列（表名版本）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="config">队列配置</param>
        public void ConfigureQueue(string tableName, WriteBehindConfig config)
        {
            FastWrite.ConfigureQueue(tableName, config);
        }

        /// <summary>
        /// 检查表是否启用了消息队列（泛型版本）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>是否启用队列</returns>
        public bool IsQueueEnabled<T>() where T : class
        {
            return FastWrite.IsQueueEnabled<T>();
        }

        /// <summary>
        /// 检查表是否启用了消息队列（表名版本）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>是否启用队列</returns>
        public bool IsQueueEnabled(string tableName)
        {
            return FastWrite.IsQueueEnabled(tableName);
        }
#endif
    }
}
