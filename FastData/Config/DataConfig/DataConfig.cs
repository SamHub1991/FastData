using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using FastData.Type;
using FastData.Model;
using FastData.Base;
using FastUntility.Base;
using System;
using System.Reflection;
using System.IO;
using System.Xml;

namespace FastData.Config
{
    /// <summary>
    /// 数据库配置类
    /// </summary>
    internal class DataConfig : ConfigurationSection
    {
        [ConfigurationProperty("Default", IsRequired = false, DefaultValue = "")]
        public string Default
        {
            get
            {
                return this["Default"].ToString();
            }
        }

        [ConfigurationProperty("Connections", IsRequired = false)]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig Connections
        {
            get
            {
                return (CollectionConfig)this["Connections"];
            }
        }

        #region oralce 节点
        /// <summary>
        /// oralce 节点
        /// </summary>
        [ConfigurationProperty("Oracle")]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig Oracle
        {
            get
            {
                return (CollectionConfig)this["Oracle"];
            }
        }
        #endregion
        
        #region sqlserver 节点
         ///<summary>
         ///sqlserver 节点
         ///</summary>
        [ConfigurationProperty("SqlServer")]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig SqlServer
        {
            get
            {
                return (CollectionConfig)this["SqlServer"];
            }
        }
        #endregion

        #region mysql 节点
        /// <summary>
        /// mysql 节点
        /// </summary>
        [ConfigurationProperty("MySql")]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig MySql
        {
            get
            {
                return (CollectionConfig)this["MySql"];
            }
        }
        #endregion
        
        #region db2 节点
        /// <summary>
        /// db2 节点
        /// </summary>
        [ConfigurationProperty("DB2")]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig DB2
        {
            get
            {
                return (CollectionConfig)this["DB2"];
            }
        }
        #endregion

        #region SQLite 节点
        /// <summary>
        /// SQLite 节点
        /// </summary>
        [ConfigurationProperty("SQLite", IsRequired = false)]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig SQLite
        {
            get
            {
                return (CollectionConfig)this["SQLite"];
            }
        }
        #endregion

        #region PostgreSql 节点
        /// <summary>
        /// PostgreSql 节点
        /// </summary>
        [ConfigurationProperty("PostgreSql", IsRequired = false)]
        [ConfigurationCollection(typeof(CollectionConfig), AddItemName = "Add")]
        public CollectionConfig PostgreSql
        {
            get
            {
                return (CollectionConfig)this["PostgreSql"];
            }
        }
        #endregion

        #region 获取配置节点
        /// <summary>
        /// 获取配置节点
        /// </summary>
        /// <returns></returns>
        public static ConfigModel GetConfig(string key = null, string projectName = null, string dbFile = "db.config")
        {
            var cacheKey = "FastData.db.config";
            var result = new ConfigModel();
            var list = new List<ConfigModel>();
            var config = new DataConfig();
            var defaultKey = string.Empty;

            if (DbCache.Exists(CacheType.Web, cacheKey))
                list = DbCache.Get<List<ConfigModel>>(CacheType.Web, cacheKey);
            else if (projectName == null)
            {
                // Try file-based approach first
                try
                {
                    if (dbFile.ToLower() == "web.config")
                        config = (DataConfig)ConfigurationManager.GetSection("DataConfig");
                    else
                    {
                        var exeConfig = new ExeConfigurationFileMap();
                        exeConfig.ExeConfigFilename = string.Format("{0}bin\\{1}", AppDomain.CurrentDomain.BaseDirectory, dbFile);
                        config = (DataConfig)ConfigurationManager.OpenMappedExeConfiguration(exeConfig, ConfigurationUserLevel.None).GetSection("DataConfig");
                    }
                }
                catch
                {
                    // File-based approach failed, try embedded resource
                    config = null;
                }

                // If file-based approach failed, try embedded resource from calling assemblies
                if (config == null || config.Default == null)
                {
                    var assemblies = new[] {
                        Assembly.GetCallingAssembly(),
                        Assembly.GetEntryAssembly(),
                        Assembly.GetExecutingAssembly()
                    };
                    
                    foreach (var assembly in assemblies)
                    {
                        if (assembly == null) continue;
                        var name = assembly.GetName().Name;
                        var tempConfig = LoadFromEmbeddedResource(name, dbFile);
                        if (tempConfig != null && tempConfig.Connections != null && tempConfig.Connections.Count != 0)
                        {
                            config = tempConfig;
                            projectName = name;
                            break;
                        }
                    }
                }

                if (config == null)
                    config = new DataConfig();

                defaultKey = config.Default;

                if (config.Connections != null && config.Connections.Count != 0)
                {
                    foreach (var temp in config.Connections)
                    {
                        var element = temp as ElementConfig;
                        var item = CreateConfigModel(element, element.Provider);
                        if (item != null)
                        {
                            if (string.IsNullOrEmpty(item.Key) && element.IsDefault)
                                item.Key = config.Default;
                            if (element.IsDefault)
                                defaultKey = item.Key;
                            list.Add(item);
                        }
                    }
                }

                #region Db2
                if (config.DB2 != null && config.DB2.Count != 0)
                {
                    foreach (var temp in config.DB2)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.DB2);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion

                #region oracle
                if (config.Oracle != null && config.Oracle.Count != 0)
                {
                    foreach (var temp in config.Oracle)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.Oracle);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion

                #region mysql
                if (config.MySql != null && config.MySql.Count != 0)
                {
                    foreach (var temp in config.MySql)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.MySql);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion

                #region sqlserver
                if (config.SqlServer != null && config.SqlServer.Count != 0)
                {
                    foreach (var temp in config.SqlServer)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.SqlServer);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion

                #region sqlite
                if (config.SQLite != null && config.SQLite.Count != 0)
                {
                    foreach (var temp in config.SQLite)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.SQLite);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion

                #region PostgreSql
                if (config.PostgreSql != null && config.PostgreSql.Count != 0)
                {
                    foreach (var temp in config.PostgreSql)
                    {
                        var item = CreateConfigModel(temp as ElementConfig, DataDbType.PostgreSql);
                        if (item != null)
                            list.Add(item);
                    }
                }
                #endregion
            }
            else
            {
                var assembly = Assembly.Load(projectName);
                using (var resource = assembly.GetManifestResourceStream(string.Format("{0}.{1}", projectName, dbFile)))
                {
                    if (resource != null)
                    {
                        using (var reader = new StreamReader(resource))
                        {
                            var content = reader.ReadToEnd();
                            var xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(content);
                            var nodelList = xmlDoc.SelectNodes("configuration/DataConfig");
                            foreach (XmlNode node in nodelList)
                            {
                                defaultKey = GetAttr(node, "Default");
                                foreach (XmlNode leaf in node.ChildNodes)
                                {
                                    if (leaf.Name.ToLower() == "connections")
                                    {
                                        foreach (XmlNode db in leaf.ChildNodes)
                                        {
                                            var item = CreateConfigModel(db, GetAttr(db, "Provider"));
                                            if (item != null)
                                            {
                                                if (string.IsNullOrEmpty(item.Key) && IsTrue(GetAttr(db, "IsDefault")))
                                                    item.Key = defaultKey;
                                                if (IsTrue(GetAttr(db, "IsDefault")))
                                                    defaultKey = item.Key;
                                                list.Add(item);
                                            }
                                        }
                                        continue;
                                    }

                                    foreach (XmlNode db in leaf.ChildNodes)
                                    {
                                        var item = CreateConfigModel(db, leaf.Name);
                                        if (item != null)
                                            list.Add(item);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var scopeKey = FastDb.CurrentKey;
            if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(scopeKey))
                key = scopeKey;

            list = SetDefaultFirst(list, defaultKey);
            DbCache.Set<List<ConfigModel>>(CacheType.Web, cacheKey, list);

            if (string.IsNullOrEmpty(key))
                result = GetDefaultConfig(list, defaultKey);
            else
                result = list.Find(a => a.Key == key);

            if (result == null)
                throw new Exception(string.Format("数据库配置Key不存在:{0}; 可用Key:{1}", key, string.Join(",", list.Select(a => a.Key))));

            return result;
        }
        #endregion

        private static List<ConfigModel> SetDefaultFirst(List<ConfigModel> list, string defaultKey)
        {
            if (string.IsNullOrEmpty(defaultKey))
                return list;

            var item = list.Find(a => a.Key == defaultKey);
            if (item == null || list.IndexOf(item) == 0)
                return list;

            list.Remove(item);
            list.Insert(0, item);
            return list;
        }

        private static ConfigModel GetDefaultConfig(List<ConfigModel> list, string defaultKey)
        {
            if (!string.IsNullOrEmpty(defaultKey))
            {
                var item = list.Find(a => a.Key == defaultKey);
                if (item != null)
                    return item;
            }

            if (list.Count == 0)
                return null;

            return list.First();
        }

        private static ConfigModel CreateConfigModel(ElementConfig element, string dbType)
        {
            var item = CreateProviderConfig(dbType);
            if (item == null)
                return null;

            item.ConnStr = element.ConnStr;
            item.IsOutError = element.IsOutError;
            item.IsOutSql = element.IsOutSql;
            item.IsPropertyCache = element.IsPropertyCache;
            item.Key = element.Key;
            item.DbLinkName = element.DbLinkName;
            item.DesignModel = element.DesignModel;
            item.IsEncrypt = element.IsEncrypt;
            item.IsMapSave = element.IsMapSave;
            item.SqlErrorType = element.SqlErrorType;
            item.CacheType = element.CacheType;
            item.IsUpdateCache = element.IsUpdateCache;
            return item;
        }

        private static ConfigModel CreateConfigModel(XmlNode db, string dbType)
        {
            var item = CreateProviderConfig(dbType);
            if (item == null)
                return null;

            item.ConnStr = GetAttr(db, "ConnStr");
            item.IsOutError = GetBool(db, "IsOutError", true);
            item.IsOutSql = GetBool(db, "IsOutSql", true);
            item.IsPropertyCache = GetBool(db, "IsPropertyCache", true);
            item.Key = GetAttr(db, "Key");
            item.DbLinkName = GetAttr(db, "DbLinkName");
            item.DesignModel = GetAttr(db, "DesignModel");
            item.IsEncrypt = GetBool(db, "IsEncrypt", false);
            item.IsMapSave = GetBool(db, "IsMapSave", false);
            item.SqlErrorType = GetAttr(db, "SqlErrorType");
            item.CacheType = GetAttr(db, "CacheType");
            item.IsUpdateCache = GetBool(db, "IsUpdateCache", false);
            item.DesignModel = item.DesignModel == null ? "DbFirst" : item.DesignModel;
            item.CacheType = item.CacheType == null ? "web" : item.CacheType;
            item.SqlErrorType = item.SqlErrorType == null ? "db" : item.SqlErrorType;
            return item;
        }

        private static string GetAttr(XmlNode node, string name)
        {
            var attr = node.Attributes == null ? null : node.Attributes[name];
            return attr == null ? null : attr.Value;
        }

        private static bool GetBool(XmlNode node, string name, bool defaultValue)
        {
            var value = GetAttr(node, name);
            if (value == null)
                return defaultValue;

            return value.ToStr().ToLower() == "true";
        }

        private static bool IsTrue(string value)
        {
            return value.ToStr().ToLower() == "true";
        }

        private static ConfigModel CreateProviderConfig(string dbType)
        {
            if (string.IsNullOrEmpty(dbType))
                return null;

            var originalProviderName = dbType;

            if (dbType == Provider.DB2)
                dbType = DataDbType.DB2;
            else if (dbType == Provider.Oracle)
                dbType = DataDbType.Oracle;
            else if (dbType == Provider.MySql)
                dbType = DataDbType.MySql;
            else if (dbType == Provider.SqlServer || dbType == "Microsoft.Data.SqlClient")
                dbType = DataDbType.SqlServer;
            else if (dbType == Provider.SQLite)
                dbType = DataDbType.SQLite;
            else if (dbType == Provider.PostgreSql)
                dbType = DataDbType.PostgreSql;

            var item = new ConfigModel();
            if (dbType.ToLower() == DataDbType.DB2.ToLower())
            {
                item.DbType = DataDbType.DB2;
                item.Flag = "@";
                item.ProviderName = Provider.DB2;
            }
            else if (dbType.ToLower() == DataDbType.Oracle.ToLower())
            {
                item.DbType = DataDbType.Oracle;
                item.Flag = ":";
                item.ProviderName = Provider.Oracle;
            }
            else if (dbType.ToLower() == DataDbType.MySql.ToLower())
            {
                item.DbType = DataDbType.MySql;
                item.Flag = "?";
                item.ProviderName = Provider.MySql;
            }
            else if (dbType.ToLower() == DataDbType.SqlServer.ToLower())
            {
                item.DbType = DataDbType.SqlServer;
                item.Flag = "@";
                item.ProviderName = originalProviderName;
            }
            else if (dbType.ToLower() == DataDbType.SQLite.ToLower())
            {
                item.DbType = DataDbType.SQLite;
                item.Flag = "@";
                item.ProviderName = Provider.SQLite;
            }
            else if (dbType.ToLower() == DataDbType.PostgreSql.ToLower())
            {
                item.DbType = DataDbType.PostgreSql;
                item.Flag = ":";
                item.ProviderName = Provider.PostgreSql;
            }
            else
                return null;

            return item;
        }

        public static bool DataType(string key = null, string projectName = null, string dbFile = "db.config")
        {
            var result = new List<bool>();
            var cacheKey = "FastData.db.config";

            if (!DbCache.Exists(CacheType.Web, cacheKey))
                DataConfig.GetConfig(key, projectName, dbFile);

            var list = DbCache.Get<List<ConfigModel>>(CacheType.Web, cacheKey);

            result.Add(list.Count(a => a.DbType == DataDbType.Oracle) > 0);
            result.Add(list.Count(a => a.DbType == DataDbType.DB2) > 0);
            result.Add(list.Count(a => a.DbType == DataDbType.SQLite) > 0);
            result.Add(list.Count(a => a.DbType == DataDbType.SqlServer) > 0);
            result.Add(list.Count(a => a.DbType == DataDbType.PostgreSql) > 0);
            result.Add(list.Count(a => a.DbType == DataDbType.MySql) > 0);

            return result.Count(a => a == true) > 1;
        }

        /// <summary>
        /// 从嵌入式资源加载配置
        /// </summary>
        private static DataConfig LoadFromEmbeddedResource(string projectName, string dbFile)
        {
            try
            {
                var assembly = Assembly.Load(projectName);
                using (var resource = assembly.GetManifestResourceStream(string.Format("{0}.{1}", projectName, dbFile)))
                {
                    if (resource != null)
                    {
                        // Just verify the resource exists and has DataConfig section
                        using (var reader = new StreamReader(resource))
                        {
                            var content = reader.ReadToEnd();
                            if (content.Contains("DataConfig"))
                                return new DataConfig();
                        }
                    }
                }
            }
            catch
            {
                // Failed to load from embedded resource
            }
            return null;
        }
    }
}
