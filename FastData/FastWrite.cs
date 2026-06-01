using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using FastData.Base;
using FastData.Model;
using FastData.Repository;
using FastData.Property;
using System.Diagnostics;
using FastData.Context;
using FastData.DbTypes;
using System.Reflection;
#if !NETFRAMEWORK
using FastData.Queue;
#endif

namespace FastData
{
    /// <summary>
    /// FastData 写入操作（静态方法）
    /// 
    /// 职责：
    /// 1. 单条/批量数据添加（Add / AddList）
    /// 2. 数据更新（Update / UpdateList）
    /// 3. 数据删除（Delete）
    /// 4. 高性能批量插入（BulkInsert，使用 SqlBulkCopy 等）
    /// 5. 原生 SQL 写入（ExecuteSql）
    /// 6. CodeFirst 建表（根据实体类特性自动创建表结构）
    /// 
    /// 使用示例：
    /// <code>
    /// // ========== 添加 ==========
    /// 
    /// // 单条添加
    /// var user = new User { Name = "张三", Age = 25 };
    /// var result = FastWrite.Add(user);
    /// if (result.IsSuccess)
    ///     Console.WriteLine($"新增成功，ID: {result.GetIdentity()}");
    /// 
    /// // 批量添加
    /// var users = new List&lt;User&gt; { new User { Name = "张三" }, new User { Name = "李四" } };
    /// var result = FastWrite.AddList(users);
    /// 
    /// // 高性能批量插入（适合大数据量）
    /// var result = FastWrite.BulkInsert(users);
    /// 
    /// // ========== 更新 ==========
    /// 
    /// // 根据主键更新
    /// var result = FastWrite.Update(user);
    /// 
    /// // 只更新指定字段
    /// var result = FastWrite.Update(user, u =&gt; new { u.Name });
    /// 
    /// // 根据条件更新
    /// var result = FastWrite.Update(new User { Name = "新名字" }, u =&gt; u.Age &gt; 18);
    /// 
    /// // 批量更新
    /// var result = FastWrite.UpdateList(userList);
    /// 
    /// // ========== 删除 ==========
    /// 
    /// // 根据条件删除
    /// var result = FastWrite.Delete&lt;User&gt;(u =&gt; u.Age &lt; 18);
    /// 
    /// // 根据主键删除
    /// var result = FastWrite.Delete(user);
    /// 
    /// // ========== 原生 SQL ==========
    /// 
    /// var result = FastWrite.ExecuteSql("UPDATE Users SET Age = @Age WHERE Id = @Id", param);
    /// 
    /// // ========== CodeFirst ==========
    /// 
    /// // 根据实体类创建表
    /// var result = FastWrite.CodeFirst&lt;User&gt;();
    /// 
    /// // 重建表（先删除再创建）
    /// var result = FastWrite.CodeFirst&lt;User&gt;(isDropExists: true);
    /// 
    /// // ========== 绑定 Key ==========
    /// 
    /// // 方式1：使用 Use 方法
    /// var db1 = FastWrite.Use("db1");
    /// var result = db1.Add(user);
    /// 
    /// // 方式2：使用 FastDataClient（推荐）
    /// var client = new FastDataClient("db1");
    /// var result = client.Add(user);
    /// </code>
    /// 
    /// 相关类：
    /// - FastWriteDb: 绑定 Key 的写入操作（实例方法）
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - FastRead: 读取操作
    /// - FastMap: XML 映射操作
    /// </summary>
    public static class FastWrite
    {
        public static FastWriteDb Use(string key)
        {
            return new FastWriteDb(key);
        }

#if !NETFRAMEWORK
        /// <summary>
        /// 创建链式写入构建器（带消息队列支持）
        /// 支持 Fluent API：FastWrite.QueueBuilder().Add(user).Add(user2).Execute()
        /// </summary>
        /// <param name="databaseKey">数据库 Key（可选）</param>
        /// <returns>链式构建器</returns>
        public static FastWriteQueueBuilder QueueBuilder(string databaseKey = null)
        {
            return new FastWriteQueueBuilder(databaseKey);
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

        #region 批量增加
        /// <summary>
        /// 批量增加
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="IsTrans">是否事务</param>
        /// <returns></returns>
        public static WriteReturn AddList<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            using (var tempDb = new DataContext(key, projectName))
            {
                config = tempDb.config;
                result = tempDb.AddList<T>(list, IsTrans, isLog);
            }

            stopwatch.Stop();

            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 批量增加 asy
        /// <summary>
        /// 批量增加 asy
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="IsTrans">是否事务</param>
        /// <returns></returns>
        public static Task<WriteReturn> AddListAsy<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => AddList<T>(list, key, IsTrans, isLog));
        }
        #endregion


        #region 增加
        /// <summary>
        /// 增加
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="IsTrans">是否事务</param>
        /// <param name="notAddField">不需要增加的字段</param>
        /// <returns></returns>
        public static WriteReturn Add<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            // 审计字段自动填充
            if (FastData.Config.FastDataOptions.Audit.Enabled)
            {
                AutoFillAuditFields(model, isUpdate: false);
            }

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.Add<T>(model, false);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.Add<T>(model, false);
                config = db.config;
            }

            stopwatch.Stop();

            if (config != null)
            {
                config.IsOutSql = config.IsOutSql || isOutSql;
                DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);
            }

            return result.WriteReturn;
        }
        #endregion

        #region 增加 asy
        /// <summary>
        /// 增加 asy
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="IsTrans">是否事务</param>
        /// <param name="notAddField">不需要增加的字段</param>
        /// <returns></returns>
        public static Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => Add<T>(model, db, key, isOutSql));
        }
        #endregion


        #region 删除(Lambda表达式)
        /// <summary>
        /// 删除(Lambda表达式)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <param name="IsTrans">是否事务</param>
        /// <returns></returns>
        public static WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            // 软删除支持
            if (Config.FastDataOptions.SoftDelete.Enabled)
            {
                return SoftDelete<T>(predicate, db, key, isOutSql);
            }

            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.Delete<T>(predicate);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.Delete<T>(predicate);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }

        /// <summary>
        /// 软删除实现
        /// </summary>
        private static WriteReturn SoftDelete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            try
            {
                var deleteProperty = FastData.Config.FastDataOptions.SoftDelete.PropertyName;
                var property = typeof(T).GetProperty(deleteProperty);
                if (property == null) throw new Exception($"字段{deleteProperty}不存在");

                var parameter = Expression.Parameter(typeof(T), "x");
                var updateField = Expression.Lambda<Func<T, object>>(
                    Expression.Convert(Expression.Property(parameter, property), typeof(object)), parameter);

                var example = new T();
                if (db == null)
                {
                    using (var tempDb = new DataContext(key, Assembly.GetCallingAssembly().GetName().Name))
                    {
                        var result = tempDb.Update(example, predicate, updateField);
                        return result.WriteReturn;
                    }
                }
                else
                {
                    var result = db.Update(example, predicate, updateField);
                    return result.WriteReturn;
                }
            }
            catch (Exception ex)
            {
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }
        #endregion

        #region 删除(Lambda表达式)asy
        /// <summary>
        /// 删除(Lambda表达式)asy
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <param name="IsTrans">是否事务</param>
        /// <returns></returns>
        public static Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => Delete<T>(predicate, db, key, isOutSql));
        }
        #endregion


        #region 删除
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriteReturn Delete<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.Delete(model, isTrans);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.Delete(model, isTrans);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 删除asy
        /// <summary>
        /// 删除asy
        /// </summary>
        /// <returns></returns>
        public static Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => Delete<T>(model, db, key, isTrans, isOutSql));
        }
        #endregion


        #region 修改(Lambda表达式)
        /// <summary>
        /// 修改(Lambda表达式)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="predicate">表达式</param>
        /// <param name="IsTrans">是否事务</param>
        /// <param name="field">需要修改的字段</param>
        /// <returns></returns>
        public static WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            // 审计字段自动填充
            if (FastData.Config.FastDataOptions.Audit.Enabled)
            {
                AutoFillAuditFields(model, isUpdate: true);
            }

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.Update<T>(model, predicate, field);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.Update<T>(model, predicate, field);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 修改(Lambda表达式)asy
        /// <summary>
        /// 修改(Lambda表达式)asy
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="predicate">表达式</param>
        /// <param name="IsTrans">是否事务</param>
        /// <param name="field">需要修改的字段</param>
        /// <returns></returns>
        public static Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => Update<T>(model, predicate, field, db, key, isOutSql));
        }
        #endregion


        #region 修改
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.Update(model, field);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.Update(model, field);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 修改asy
        /// <summary>
        /// 修改asy
        /// </summary>
        /// <returns></returns>
        public static Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => Update<T>(model, field, db, key, isOutSql));
        }
        #endregion


        #region 修改list
        /// <summary>
        /// 修改list
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn<T>();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    result = tempDb.UpdateList(list, field);
                    config = tempDb.config;
                }
            }
            else
            {
                result = db.UpdateList(list, field);
                config = db.config;
            }

            stopwatch.Stop();

            config.IsOutSql = config.IsOutSql || isOutSql;
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 修改list asy
        /// <summary>
        /// 修改list asy
        /// </summary>
        /// <returns></returns>
        public static Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => UpdateList<T>(list, field, db, key, isOutSql));
        }
        #endregion


        #region 执行sql
        /// <summary>
        /// 执行sql
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">配置键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>写入返回对象</returns>
        public static WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(key, projectName))
                {
                    config = tempDb.config;
                    config.IsOutSql = config.IsOutSql || isOutSql;
                    result = tempDb.ExecuteSql(sql, param, false,config.IsOutSql);
                }
            }
            else
            {
                config = db.config;
                config.IsOutSql = config.IsOutSql || isOutSql;
                result = db.ExecuteSql(sql, param, false, config.IsOutSql);
            }

            stopwatch.Stop();
            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }
        #endregion

        #region 执行sql asy
        /// <summary>
        /// 执行sql asy
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="param">数据库参数数组</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">配置键</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>写入返回对象任务</returns>
        public static Task<WriteReturn> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false)
        {
            return AsyncHelper.RunAsync(() => ExecuteSql(sql, param, db, key, isOutSql));
        }
        #endregion

        #region 批量插入
        /// <summary>
        /// 批量插入（高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <param name="key">数据库Key（可选）</param>
        /// <returns>插入结果</returns>
        public static WriteReturn BulkInsert<T>(List<T> list, DataContext db = null, string key = null) where T : class, new()
        {
            if (list == null || list.Count == 0)
                return new WriteReturn { IsSuccess = true };

            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            try
            {
                if (db == null)
                {
                    using (var tempDb = new DataContext(key, projectName))
                    {
                        config = tempDb.config;
                        result = tempDb.BulkInsert(list, config.IsOutSql);
                    }
                }
                else
                {
                    config = db.config;
                    result = db.BulkInsert(list, config.IsOutSql);
                }
            }
            catch (Exception ex)
            {
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            DbLog.LogSql(config?.IsOutSql ?? false, result.Sql, config?.DbType ?? DataDbType.SqlServer, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }

        /// <summary>
        /// 批量插入异步（高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <param name="key">数据库Key（可选）</param>
        /// <returns>插入结果</returns>
        public static Task<WriteReturn> BulkInsertAsync<T>(List<T> list, DataContext db = null, string key = null) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => BulkInsert(list, db, key));
        }
        #endregion

        #region Code First

        /// <summary>
        /// Code First：根据 Model 创建数据库表
        /// </summary>
        public static WriteReturn CodeFirst<T>(string key = null, bool isDropExists = false) where T : class, new()
        {
            try
            {
                var tableName = typeof(T).GetCustomAttributes(typeof(Property.TableAttribute), false)
                    .Cast<Property.TableAttribute>().FirstOrDefault()?.Name ?? typeof(T).Name;

                var db = new DataContext(key);
                var sql = BuildCreateTableSql<T>(tableName);

                if (isDropExists)
                {
                    try { db.ExecuteSql($"DROP TABLE [{tableName}]", null, false, false); }
                    catch (Exception dropEx) { DbLog.LogException(true, DataDbType.SqlServer, dropEx, "CodeFirst_DropTable", ""); }
                }

                db.ExecuteSql(sql, null, false, true);
                return new WriteReturn { IsSuccess = true, Message = $"Table {tableName} created" };
            }
            catch (Exception ex)
            {
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }

        private static string BuildCreateTableSql<T>(string tableName) where T : class, new()
        {
            var cols = GetModelColumns<T>();
            var defs = new List<string>();

            foreach (var c in cols)
            {
                var def = $"[{c.Name}] {c.Type}";
                if (c.IsPrimary && c.IsIdentity) def += " IDENTITY(1,1)";
                if (!c.IsNull || c.IsPrimary) def += " NOT NULL";
                defs.Add(def);
            }

            var pks = cols.Where(x => x.IsPrimary).Select(x => x.Name).ToList();
            if (pks.Count > 0) defs.Add($"PRIMARY KEY ({string.Join(", ", pks.Select(n => $"[{n}]"))})");

            return $"CREATE TABLE [{tableName}] ({string.Join(", ", defs)})";
        }

        private static List<(string Name, string Type, bool IsPrimary, bool IsIdentity, bool IsNull)> GetModelColumns<T>() where T : class, new()
        {
            var list = new List<(string, string, bool, bool, bool)>();
            foreach (var p in typeof(T).GetProperties())
            {
                var col = p.GetCustomAttribute<Property.ColumnAttribute>();
                var pri = p.GetCustomAttribute<Property.PrimaryAttribute>() != null;
                var type = GetClrSqlType(p.PropertyType, col?.Length ?? 0);
                list.Add((p.Name, type, pri, col?.IsIdentity ?? false, col?.IsNull ?? true));
            }
            return list;
        }

        private static string GetClrSqlType(Type t, int len)
        {
            if (t == typeof(string)) return len > 0 ? $"VARCHAR({len})" : "VARCHAR(MAX)";
            if (t == typeof(int)) return "INT";
            if (t == typeof(long)) return "BIGINT";
            if (t == typeof(decimal) || t == typeof(double)) return "DECIMAL(18,2)";
            if (t == typeof(bool)) return "BIT";
            if (t == typeof(DateTime)) return "DATETIME";
            if (t == typeof(Guid)) return "UNIQUEIDENTIFIER";
            return "NVARCHAR(MAX)";
        }

        public static Task<WriteReturn> CodeFirstAsync<T>(string key = null, bool isDropExists = false) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => CodeFirst<T>(key, isDropExists));
        }


        /// <summary>
        /// 自动填充审计字段
        /// </summary>
        private static void AutoFillAuditFields<T>(T model, bool isUpdate) where T : class, new()
        {
            var audit = FastData.Config.FastDataOptions.Audit;
            var now = DateTime.Now;
            var currentUser = audit.GetCurrentUser?.Invoke() ?? "System";

            // 创建时间
            if (!isUpdate)
            {
                var createdTimeProp = typeof(T).GetProperty(audit.CreatedTimeProperty);
                if (createdTimeProp != null && createdTimeProp.CanWrite)
                    createdTimeProp.SetValue(model, now);

                var createdByProp = typeof(T).GetProperty(audit.CreatedByProperty);
                if (createdByProp != null && createdByProp.CanWrite)
                    createdByProp.SetValue(model, currentUser);
            }

            // 更新时间
            var updatedTimeProp = typeof(T).GetProperty(audit.UpdatedTimeProperty);
            if (updatedTimeProp != null && updatedTimeProp.CanWrite)
                updatedTimeProp.SetValue(model, now);

            var updatedByProp = typeof(T).GetProperty(audit.UpdatedByProperty);
            if (updatedByProp != null && updatedByProp.CanWrite)
                updatedByProp.SetValue(model, currentUser);
        }
        #endregion

        #region SqlBulkCopy 辅助方法

        /// <summary>
        /// 创建用于 SqlBulkCopy 的 DataTable，自动包含实体所有属性对应的列
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="includeIdentity">是否包含自增列（默认为 true）</param>
        /// <returns>配置好的 DataTable</returns>
        public static DataTable CreateBulkCopyDataTable<T>(bool includeIdentity = true) where T : class, new()
        {
            var dataTable = new DataTable();
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            foreach (var prop in properties)
            {
                // 检查是否是自增列（通过 Column 特性）
                var columnAttr = prop.GetCustomAttributes(typeof(Property.ColumnAttribute), false)
                    .FirstOrDefault() as Property.ColumnAttribute;
                
                if (!includeIdentity && columnAttr?.IsIdentity == true)
                    continue;

                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            return dataTable;
        }

        /// <summary>
        /// 将实体列表填充到 DataTable（用于 SqlBulkCopy）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="dataTable">目标 DataTable</param>
        /// <param name="includeIdentity">是否包含自增列</param>
        public static void FillBulkCopyDataTable<T>(List<T> list, DataTable dataTable, bool includeIdentity = true) where T : class, new()
        {
            if (list == null || list.Count == 0)
                return;

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            foreach (var item in list)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    // 检查是否是自增列
                    var columnAttr = prop.GetCustomAttributes(typeof(Property.ColumnAttribute), false)
                        .FirstOrDefault() as Property.ColumnAttribute;
                    
                    if (!includeIdentity && columnAttr?.IsIdentity == true)
                        continue;

                    if (dataTable.Columns.Contains(prop.Name))
                    {
                        var value = prop.GetValue(item);
                        row[prop.Name] = value ?? DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }
        }

        #endregion

        #region 批量更新/删除

        /// <summary>
        /// 批量更新实体（使用 SQL UPDATE ... WHERE IN）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="predicate">更新条件（通常使用 ID）</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <param name="key">数据库Key（可选）</param>
        /// <returns>更新结果</returns>
        public static WriteReturn BulkUpdate<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null, string key = null) where T : class, new()
        {
            if (list == null || list.Count == 0)
                return new WriteReturn { IsSuccess = true };

            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            try
            {
                if (db == null)
                {
                    using (var tempDb = new DataContext(key, projectName))
                    {
                        config = tempDb.config;
                        result = tempDb.BulkUpdate(list, predicate, config.IsOutSql);
                    }
                }
                else
                {
                    config = db.config;
                    result = db.BulkUpdate(list, predicate, config.IsOutSql);
                }
            }
            catch (Exception ex)
            {
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            DbLog.LogSql(config?.IsOutSql ?? false, result.Sql, config?.DbType ?? DataDbType.SqlServer, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }

        /// <summary>
        /// 批量删除实体（使用 SQL DELETE FROM ... WHERE IN）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文（可选）</param>
        /// <param name="key">数据库Key（可选）</param>
        /// <returns>删除结果</returns>
        public static WriteReturn BulkDelete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null) where T : class, new()
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), "删除条件不能为空");

            key = key ?? FastDb.CurrentKey;
            var projectName = Assembly.GetCallingAssembly().GetName().Name;
            ConfigModel config = null;
            var result = new DataReturn();
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            try
            {
                if (db == null)
                {
                    using (var tempDb = new DataContext(key, projectName))
                    {
                        config = tempDb.config;
                        result = tempDb.BulkDelete(predicate, config.IsOutSql);
                    }
                }
                else
                {
                    config = db.config;
                    result = db.BulkDelete(predicate, config.IsOutSql);
                }
            }
            catch (Exception ex)
            {
                result.WriteReturn.IsSuccess = false;
                result.WriteReturn.Message = ex.Message;
            }

            stopwatch.Stop();
            DbLog.LogSql(config?.IsOutSql ?? false, result.Sql, config?.DbType ?? DataDbType.SqlServer, stopwatch.Elapsed.TotalMilliseconds);

            return result.WriteReturn;
        }

        /// <summary>
        /// 批量更新/删除异步
        /// </summary>
        public static Task<WriteReturn> BulkUpdateAsync<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null, string key = null) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => BulkUpdate(list, predicate, db, key));
        }

        public static Task<WriteReturn> BulkDeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null) where T : class, new()
        {
            return AsyncHelper.RunAsync(() => BulkDelete(predicate, db, key));
        }

        #endregion
    }
}
