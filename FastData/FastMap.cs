using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;
using FastUntility.Page;
using FastData.Base;
using FastData.Config;
using FastData.Type;
using FastData.Model;
using System.Diagnostics;
using System.IO;
using FastUntility.Base;
using FastData.CacheModel;
using FastData.Check;
using System.Reflection;
using FastData.Context;

namespace FastData
{
    /// <summary>
    /// map
    /// </summary>
    public static class FastMap
    {
        #region 初始化model成员 1
        /// <summary>
        /// 初始化model成员 1
        /// </summary>
        /// <param name="list"></param>
        /// <param name="nameSpace">命名空间</param>
        /// <param name="dll">dll名称</param>
        public static void InstanceProperties(string nameSpace, string projectName)
        {
            var config = DataConfig.GetConfig();

            var assembly = AppDomain.CurrentDomain.GetAssemblies().ToList().Find(a => a.FullName.Split(',')[0] == projectName.Replace(".dll", ""));
            if (assembly == null)
                assembly = Assembly.Load(projectName.Replace(".dll", ""));

            if (assembly != null)
            {
                assembly.ExportedTypes.ToList().ForEach(t => {
                    var typeInfo = (t as TypeInfo);
                    if (typeInfo.Namespace != null && typeInfo.Namespace == nameSpace)
                    {
                        var key = string.Format("{0}.{1}", typeInfo.Namespace, typeInfo.Name);
                        var cacheList = new List<PropertyModel>();
                        typeInfo.DeclaredProperties.ToList().ForEach(a => {
                            var model = new PropertyModel();
                            model.Name = a.Name;
                            model.PropertyType = a.PropertyType;
                            cacheList.Add(model);
                        });

                        DbCache.Set<List<PropertyModel>>(config.CacheType, key, cacheList);
                    }
                });
            }
        }
        #endregion

        #region 初始化code first 2
        /// <summary>
        /// 初始化code first 2
        /// </summary>
        /// <param name="list"></param>
        /// <param name="nameSpace">命名空间</param>
        /// <param name="dll">dll名称</param>
        public static void InstanceTable(string nameSpace, string projectName, string dbKey = null)
        {
            var query = new DataQuery();
            query.Config = DataConfig.GetConfig(dbKey);
            query.Key = dbKey;

            MapXml.CreateLogTable(query);

            var assembly = AppDomain.CurrentDomain.GetAssemblies().ToList().Find(a => a.FullName.Split(',')[0] == projectName.Replace(".dll", ""));
            if (assembly == null)
                assembly = Assembly.Load(projectName.Replace(".dll", ""));

            if (assembly != null)
            {
                assembly.ExportedTypes.ToList().ForEach(a => {
                    var typeInfo = (a as TypeInfo);
                    if (typeInfo.Namespace != null && typeInfo.Namespace == nameSpace)
                        BaseTable.Check(query, a.Name, typeInfo.DeclaredProperties.ToList(), typeInfo.GetCustomAttributes().ToList());
                });
            }
        }
        #endregion

        #region 初始化map 3  by Resource
        public static void InstanceMapResource(string projectName, string dbKey = null)
        {
            var config = DataConfig.GetConfig(dbKey);
            var db = new DataContext(dbKey);
            var assembly = Assembly.Load(projectName);
            MapConfig.GetConfig().Path.ForEach(a =>
            {
                using (var resource = assembly.GetManifestResourceStream(string.Format("{0}.{1}", projectName, a.Replace("/", "."))))
                {
                    if (resource != null)
                    {
                        using (var reader = new StreamReader(resource))
                        {
                            var content = reader.ReadToEnd();
                            var info = new FileInfo(a);
                            var key = BaseSymmetric.Generate(info.FullName);
                            if (!DbCache.Exists(config.CacheType, key))
                            {
                                var temp = new MapXmlModel();
                                temp.LastWrite = info.LastWriteTime;
                                temp.FileKey = MapXml.ReadXml(info.FullName, config, info.Name.ToLower().Replace(".xml", ""));
                                temp.FileName = info.FullName;
                                if (MapXml.SaveXml(dbKey, key, info, config, db))
                                    DbCache.Set<MapXmlModel>(config.CacheType, key, temp);
                            }
                            else if ((DbCache.Get<MapXmlModel>(config.CacheType, key).LastWrite - info.LastWriteTime).Milliseconds != 0)
                            {
                                DbCache.Get<MapXmlModel>(config.CacheType, key).FileKey.ForEach(f => { DbCache.Remove(config.CacheType, f); });

                                var model = new MapXmlModel();
                                model.LastWrite = info.LastWriteTime;
                                model.FileKey = MapXml.ReadXml(info.FullName, config, info.Name.ToLower().Replace(".xml", ""));
                                model.FileName = info.FullName;
                                if (MapXml.SaveXml(dbKey, key, info, config, db))
                                    DbCache.Set<MapXmlModel>(config.CacheType, key, model);
                            }
                        }
                    }
                }
            });
        }
        #endregion

        #region 初始化map 3
        /// <summary>
        /// 初始化map 3
        /// </summary>
        /// <returns></returns>
        public static void InstanceMap(string dbKey = null)
        {
            var list = MapConfig.GetConfig();
            var config = DataConfig.GetConfig(dbKey);
            using (var db = new DataContext(dbKey)) {
                var query = new DataQuery { Config = config, Key = dbKey };

                if (config.IsMapSave)
                {
                    query.Config.DesignModel = FastData.Base.Config.CodeFirst;
                    if (query.Config.DbType == DataDbType.Oracle)
                    {
                        var listInfo = typeof(FastData.DataModel.Oracle.Data_MapFile).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
                        var listAttribute = typeof(FastData.DataModel.Oracle.Data_MapFile).GetTypeInfo().GetCustomAttributes().ToList();
                        BaseTable.Check(query, "Data_MapFile", listInfo, listAttribute);
                    }

                    if (query.Config.DbType == DataDbType.MySql)
                    {
                        var listInfo = typeof(FastData.DataModel.MySql.Data_MapFile).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
                        var listAttribute = typeof(FastData.DataModel.MySql.Data_MapFile).GetTypeInfo().GetCustomAttributes().ToList();
                        BaseTable.Check(query, "Data_MapFile", listInfo, listAttribute);
                    }

                    if (query.Config.DbType == DataDbType.SqlServer)
                    {
                        var listInfo = typeof(FastData.DataModel.SqlServer.Data_MapFile).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
                        var listAttribute = typeof(FastData.DataModel.SqlServer.Data_MapFile).GetTypeInfo().GetCustomAttributes().ToList();
                        BaseTable.Check(query, "Data_MapFile", listInfo, listAttribute);
                    }
                }

                list.Path.ForEach(p => {
                    var info = new FileInfo(p);
                    var key = BaseSymmetric.md5(32, info.FullName);

                    if (!DbCache.Exists(config.CacheType, key))
                    {
                        var temp = new MapXmlModel();
                        temp.LastWrite = info.LastWriteTime;
                        temp.FileKey = MapXml.ReadXml(p, config, info.Name.ToLower().Replace(".xml", ""));
                        temp.FileName = info.FullName;
                        if (MapXml.SaveXml(dbKey, key, info, config, db))
                            DbCache.Set<MapXmlModel>(config.CacheType, key, temp);
                    }
                    else if ((DbCache.Get<MapXmlModel>(config.CacheType, key).LastWrite - info.LastWriteTime).Milliseconds != 0)
                    {
                        DbCache.Get<MapXmlModel>(config.CacheType, key).FileKey.ForEach(a => { DbCache.Remove(config.CacheType, a); });

                        var model = new MapXmlModel();
                        model.LastWrite = info.LastWriteTime;
                        model.FileKey = MapXml.ReadXml(p, config, info.Name.ToLower().Replace(".xml", ""));
                        model.FileName = info.FullName;
                        if (MapXml.SaveXml(dbKey, key, info, config, db))
                            DbCache.Set<MapXmlModel>(config.CacheType, key, model);
                    }
                });
            }
        }
        #endregion

        #region maq 执行返回结果
        /// <summary>
        /// maq 执行返回结果
        /// </summary>
        public static List<T> Query<T>(string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            key = key == null ? MapDb(name) : key;
            var config = db == null ? DataConfig.GetConfig(key) : db.config;
            if(config.IsUpdateCache)
                InstanceMap(config.Key);

            if (DbCache.Exists(config.CacheType, name.ToLower()))
            {
                var sql = MapXml.GetMapSql(name, ref param,db,key);
                var result = FastRead.ExecuteSql<T>(sql, param,db,key);
                if (MapXml.MapIsForEach(name, config))
                {
                    if (db == null)
                    {
                        using (var tempDb = new DataContext(key))
                        {
                            for (var i = 1; i < MapXml.MapForEachCount(name, config); i++)
                            {
                                result = MapXml.MapForEach<T>(result, name, tempDb, config, i);
                            }
                        }
                    }
                    else
                        result = MapXml.MapForEach<T>(result, name, db, config);
                }
                return result;
            }
            else
                return new List<T>();
        }
        #endregion

        #region maq 执行返回结果 asy
        /// <summary>
        /// 执行sql asy
        /// </summary>
        public static async Task<List<T>> QueryAsy<T>(string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return await Task.Run(() =>
            {
                return Query<T>(name, param,db,key);
            }).ConfigureAwait(false);
        }
        #endregion

        #region maq 执行返回结果 lazy
        /// <summary>
        /// maq 执行返回结果 lazy
        /// </summary>
        public static Lazy<List<T>> QueryLazy<T>(string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return new Lazy<List<T>>(() => Query<T>( name, param,db,key));
        }
        #endregion

        #region maq 执行返回结果 lazy asy
        /// <summary>
        /// maq 执行返回结果 lazy asy
        /// </summary>
        public static async Task<Lazy<List<T>>> QueryLazyAsy<T>(string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return await Task.Run(() =>
            {
                return new Lazy<List<T>>(() => Query<T>(name, param,db,key));
            }).ConfigureAwait(false);
        }
        #endregion


        #region maq 执行写操作
        /// <summary>
        /// 执行写操作
        /// </summary>
        public static WriteReturn Write(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            key = key == null ? MapDb(name) : key;
            var config = db == null ? DataConfig.GetConfig(key) : db.config;
            if (config.IsUpdateCache)
                InstanceMap(config.Key);

            if (DbCache.Exists(config.CacheType, name.ToLower()))
            {
                var sql = MapXml.GetMapSql(name, ref param,db,key);
                
                return FastWrite.ExecuteSql(sql, param, db, key);
            }
            else
                return new WriteReturn();
        }
        #endregion

        #region maq 执行写操作 asy
        /// <summary>
        ///  maq 执行写操作 asy
        /// </summary>
        public static async Task<WriteReturn> WriteAsy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return Write(name, param, db, key);
            }).ConfigureAwait(false);
        }
        #endregion

        #region maq 执行写操作 asy lazy
        /// <summary>
        /// maq 执行写操作 asy lazy
        /// </summary>
        public static Lazy<WriteReturn> WriteLazy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return new Lazy<WriteReturn>(() => Write(name, param, db, key));
        }
        #endregion

        #region maq 执行写操作 asy lazy asy
        /// <summary>
        /// maq 执行写操作 asy lazy asy
        /// </summary>
        public static async Task<Lazy<WriteReturn>> WriteLazyAsy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return new Lazy<WriteReturn>(() => Write(name, param, db, key));
            }).ConfigureAwait(false);
        }
        #endregion


        #region maq 执行返回 List<Dictionary<string, object>>
        /// <summary>
        /// maq 执行返回 List<Dictionary<string, object>>
        /// </summary>
        public static List<Dictionary<string, object>> Query(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            key = key == null ? MapDb(name) : key;
            var config = db == null ? DataConfig.GetConfig(key) : db.config;
            if (config.IsUpdateCache)
                InstanceMap(config.Key);

            if (DbCache.Exists(config.CacheType, name.ToLower()))
            {
                var sql = MapXml.GetMapSql(name, ref param,db,key);
                
                var result = FastRead.ExecuteSql(sql, param, db, key);

                if (MapXml.MapIsForEach(name, config))
                {
                    if (db == null)
                    {
                        using(var tempDb =new DataContext(key)){
                        for (var i = 1; i < MapXml.MapForEachCount(name, config); i++)
                        {
                            result = MapXml.MapForEach(result, name, tempDb, key, config,i);
                        }
                        }
                    }
                    else
                        result = MapXml.MapForEach(result, name, db, key, config);
                }

                return result;
            }
            else
                return new List<Dictionary<string, object>>();
        }
        #endregion

        #region maq 执行返回 List<Dictionary<string, object>> asy
        /// <summary>
        /// 执行sql List<Dictionary<string, object>> asy
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> QueryAsy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return Query(name, param,db,key);
            }).ConfigureAwait(false);
        }
        #endregion

        #region maq 执行返回 List<Dictionary<string, object>> lazy
        /// <summary>
        /// maq 执行返回 List<Dictionary<string, object>> lazy
        /// </summary>
        public static Lazy<List<Dictionary<string, object>>> QueryLazy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return new Lazy<List<Dictionary<string, object>>>(() => Query(name, param,db,key));
        }
        #endregion

        #region maq 执行返回 List<Dictionary<string, object>> lazy asy
        /// <summary>
        /// maq 执行返回 List<Dictionary<string, object>> lazy asy
        /// </summary>
        public static async Task<Lazy<List<Dictionary<string, object>>>> ExecuteLazyMapAsy(string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return new Lazy<List<Dictionary<string, object>>>(() => Query(name, param,db,key));
            }).ConfigureAwait(false);
        }
        #endregion


        #region 执行分页
        /// <summary>
        /// 执行分页 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private static PageResult ExecuteSqlPage(PageModel pModel, string sql, DbParameter[] param, DataContext db = null, string key = null)
        {
            var result = new DataReturn();
            var config = DataConfig.GetConfig(key);
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using(var tempDb =new DataContext(key)){
                result = tempDb.GetPageSql(pModel, sql, param);
                }
            }
            else
                result = db.GetPageSql(pModel, sql, param);

            stopwatch.Stop();

            DbLog.LogSql(config.IsOutSql, result.Sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.PageResult;
        }
        #endregion

        #region maq 执行分页
        /// <summary>
        /// maq 执行分页
        /// </summary>
        public static PageResult QueryPage(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            key = key == null ? MapDb(name) : key;
            var config = db == null ? DataConfig.GetConfig(key) : db.config;
            if (config.IsUpdateCache)
                InstanceMap(config.Key);
            if (DbCache.Exists(config.CacheType, name.ToLower()))
            {
                var sql = MapXml.GetMapSql(name, ref param,db,key);
                
                var result = ExecuteSqlPage(pModel, sql, param, db, key);

                if (MapXml.MapIsForEach(name, config))
                {
                    if (db == null)
                    {
                        using(var tempDb =new DataContext(key)){
                        for (var i = 1; i < MapXml.MapForEachCount(name, config); i++)
                        {
                            result.list = MapXml.MapForEach(result.list, name, tempDb, key, config,i);
                        }
                        }
                    }
                    else
                        result.list = MapXml.MapForEach(result.list, name, db, key, config);
                }

                return result;
            }
            else
                return new PageResult();
        }
        #endregion

        #region maq 执行分页 asy
        /// <summary>
        /// 执行分页 asy
        /// </summary>
        public static async Task<PageResult> QueryPageAsy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return QueryPage(pModel, name, param,db,key);
            }).ConfigureAwait(false);
        }
        #endregion

        #region maq 执行分页 lazy
        /// <summary>
        /// maq 执行分页 lazy
        /// </summary>
        public static Lazy<PageResult> QueryPageLazy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return new Lazy<PageResult>(() => QueryPage(pModel, name, param,db,key));
        }
        #endregion

        #region maq 执行分页 lazy asy
        /// <summary>
        /// maq 执行分页lazy asy
        /// </summary>
        public static async Task<Lazy<PageResult>> QueryPageLazyAsy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null)
        {
            return await Task.Run(() =>
            {
                return new Lazy<PageResult>(() => QueryPage(pModel, name, param,db,key));
            }).ConfigureAwait(false);
        }
        #endregion
        

        #region 执行分页
        /// <summary>
        /// 执行分页 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private static PageResult<T> ExecuteSqlPage<T>(PageModel pModel, string sql, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            var result = new DataReturn<T>();
            var config = DataConfig.GetConfig(key);
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            if (db == null)
            {
                using(var tempDb =new DataContext(key)){
                result = tempDb.GetPageSql<T>(pModel, sql, param);
                }
            }
            else
                result = db.GetPageSql<T>(pModel, sql, param);

            stopwatch.Stop();

            DbLog.LogSql(config.IsOutSql, result.sql, config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.pageResult;
        }
        #endregion

        #region maq 执行分页
        /// <summary>
        /// maq 执行分页
        /// </summary>
        public static PageResult<T> QueryPage<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            key = key == null ? MapDb(name) : key;
            var config = db == null ? DataConfig.GetConfig(key) : db.config;
            if (config.IsUpdateCache)
                InstanceMap(config.Key);
            if (DbCache.Exists(config.CacheType, name.ToLower()))
            {
                var sql = MapXml.GetMapSql(name, ref param,db,key);

                var result = ExecuteSqlPage<T>(pModel, sql, param,db,key);

                if (MapXml.MapIsForEach(name, config))
                {
                    if (db == null)
                    {
                        using(var tempDb =new DataContext(key)){
                        for (var i = 1; i < MapXml.MapForEachCount(name, config); i++)
                        {
                            result.list = MapXml.MapForEach<T>(result.list, name, tempDb, config,i);
                        }
                        }
                    }
                    else
                        result.list = MapXml.MapForEach<T>(result.list, name, db, config);
                }
                return result;
            }
            else
                return new PageResult<T>();
        }
        #endregion

        #region maq 执行分页 asy
        /// <summary>
        /// 执行分页 asy
        /// </summary>
        public static async Task<PageResult<T>> QueryPageAsy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return await Task.Run(() =>
            {
                return QueryPage<T>(pModel, name, param,db,key);
            }).ConfigureAwait(false);
        }
        #endregion

        #region maq 执行分页 lazy
        /// <summary>
        /// maq 执行分页 lazy
        /// </summary>
        public static Lazy<PageResult<T>> QueryPageLazy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return new Lazy<PageResult<T>>(() => QueryPage<T>(pModel, name, param,db,key));
        }
        #endregion

        #region maq 执行分页 lazy asy
        /// <summary>
        /// maq 执行分页lazy asy
        /// </summary>
        public static async Task<Lazy<PageResult<T>>> QueryPageLazyAsy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null) where T : class, new()
        {
            return await Task.Run(() =>
            {
                return new Lazy<PageResult<T>>(() => QueryPage<T>(pModel, name, param,db,key));
            }).ConfigureAwait(false);
        }
        #endregion

        #region 验证xml
        /// <summary>
        /// 验证xml
        /// </summary>
        /// <returns></returns>
        public static bool CheckMap(string xml, string dbKey = null)
        {
            var config = DataConfig.GetConfig(dbKey);
            var info = new FileInfo(xml);

            var key = new List<string>();
            var sql = new List<string>();
            var db = new Dictionary<string, object>();
            var type = new Dictionary<string, object>();
            var param = new Dictionary<string, object>();
            var check = new Dictionary<string, object>();
            var name = new Dictionary<string, object>();
            var parameName = new Dictionary<string, object>();

            return MapXml.GetXmlList(info.FullName, "sqlMap", ref key, ref sql, ref db, ref type, ref check, ref param, ref name, ref parameName, config);
        }
        #endregion
        
        #region map 参数列表
        /// <summary>
        /// map 参数列表
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<string> MapParam(string name,ConfigModel config=null)
        {
            var cacheType = config == null ? DataConfig.GetConfig().CacheType : config.CacheType;
            return DbCache.Get<List<string>>(cacheType, string.Format("{0}.param", name.ToLower()));
        }
        #endregion

        #region map db
        /// <summary>
        /// map db
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string MapDb(string name)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.db", name.ToLower()));
        }
        #endregion
        
        #region map type
        /// <summary>
        /// map db
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string MapType(string name)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.type", name.ToLower()));
        }
        #endregion

        #region 是否存在map id
        /// <summary>
        /// 是否存在map id
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsExists(string name)
        {
            return DbCache.Exists(DataConfig.GetConfig().CacheType, name.ToLower());
        }
        #endregion

        #region 获取map备注
        public static string MapRemark(string name)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.remark", name.ToLower()));
        }
        #endregion

        #region 获取map参数备注
        public static string MapParamRemark(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.remark", name.ToLower(), param.ToLower()));
        }
        #endregion

        #region  获取api接口key
        /// <summary>
        /// 获取api接口key
        /// </summary>
        public static Dictionary<string, object> Api
        {
            get
            {
                return DbCache.Get<Dictionary<string, object>>(DataConfig.GetConfig().CacheType, "FastMap.Api") ?? new Dictionary<string, object>();
            }
        }
        #endregion

        #region 获取map验证必填
        /// <summary>
        /// 获取map验证必填
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string MapRequired(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.required", name.ToLower(), param.ToLower()));
        }
        #endregion

        #region 获取map验证长度
        /// <summary>
        /// 获取map验证长度
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string MapMaxlength(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.maxlength", name.ToLower(), param.ToLower()));
        }
        #endregion

        #region 获取map验证日期 
        /// <summary>
        /// 获取map验证日期
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string MapDate(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.date", name.ToLower(), param.ToLower()));
        }
        #endregion

        #region 获取map验证map
        /// <summary>
        /// 获取map验证map
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string MapCheckMap(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.checkmap", name.ToLower(), param.ToLower()));
        }
        #endregion

        #region 获取map验证map
        /// <summary>
        /// 获取map验证map
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string MapExistsMap(string name, string param)
        {
            return DbCache.Get(DataConfig.GetConfig().CacheType, string.Format("{0}.{1}.existsmap", name.ToLower(), param.ToLower()));
        }
        #endregion
    }
}

