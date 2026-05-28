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

        [ConfigurationProperty("Active", IsRequired = false, DefaultValue = "")]
        public string Active
        {
            get
            {
                return this["Active"].ToString();
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

        #region Redis 节点
        /// <summary>
        /// Redis 配置
        /// </summary>
        [ConfigurationProperty("Redis", IsRequired = false)]
        public RedisElement Redis
        {
            get
            {
                return (RedisElement)this["Redis"];
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
                // Load db.config to get Active value
                var baseConfig = TryLoadConfig(dbFile);
                var activeEnv = ResolveActiveEnvironment(baseConfig);

                // Try to load environment-specific config (e.g., db.dev.config, db.pro.config)
                if (!string.IsNullOrEmpty(activeEnv))
                {
                    var envDbFile = $"db.{activeEnv}.config";
                    config = TryLoadConfig(envDbFile);
                }

                // Fall back to base db.config (if it has connections directly)
                if (config == null || config.Connections == null || config.Connections.Count == 0)
                    config = baseConfig;

                // If file-based approach failed, try embedded resource from calling assemblies
                if (config == null || (config.Connections == null || config.Connections.Count == 0))
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
                        
                        // Try environment-specific embedded resource first
                        if (!string.IsNullOrEmpty(activeEnv))
                        {
                            var envDbFile = $"db.{activeEnv}.config";
                            var tempConfig = LoadFromEmbeddedResource(name, envDbFile);
                            if (tempConfig != null && tempConfig.Connections != null && tempConfig.Connections.Count != 0)
                            {
                                config = tempConfig;
                                projectName = name;
                                break;
                            }
                        }
                        
                        // Fall back to base embedded resource
                        var baseTempConfig = LoadFromEmbeddedResource(name, dbFile);
                        if (baseTempConfig != null && baseTempConfig.Connections != null && baseTempConfig.Connections.Count != 0)
                        {
                            config = baseTempConfig;
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
                // Use file-based approach (same as projectName == null case)
                // This handles environment resolution correctly
                var baseConfig = TryLoadConfig(dbFile);
                var activeEnv = ResolveActiveEnvironment(baseConfig);

                // Try to load environment-specific config (e.g., db.dev.config)
                if (!string.IsNullOrEmpty(activeEnv))
                {
                    var envDbFile = $"db.{activeEnv}.config";
                    config = TryLoadConfig(envDbFile);
                }

                // Fall back to base db.config (if it has connections directly)
                if (config == null || config.Connections == null || config.Connections.Count == 0)
                    config = baseConfig;

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
                                item.Key = defaultKey;
                            if (element.IsDefault)
                                defaultKey = item.Key;
                            list.Add(item);
                        }
                    }
                }
            }

            var scopeKey = FastDb.CurrentKey;
            if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(scopeKey))
                key = scopeKey;

            // Apply environment variable overrides
            ApplyEnvironmentOverrides(list);

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

        /// <summary>
        /// Try to load config from file
        /// </summary>
        private static DataConfig TryLoadConfig(string dbFile)
        {
            try
            {
                if (dbFile.ToLower() == "web.config")
                    return (DataConfig)ConfigurationManager.GetSection("DataConfig");

                var configPath = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, dbFile);
                if (!File.Exists(configPath))
                    return null;

                var content = File.ReadAllText(configPath);
                return ParseXmlConfig(content);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Apply environment variable overrides to config list
        /// Environment variable format: FASTDATA_CONN_{KEY} (e.g., FASTDATA_CONN_SQLSERVER)
        /// </summary>
        private static void ApplyEnvironmentOverrides(List<ConfigModel> list)
        {
            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(item.Key)) continue;
                
                var envVarName = $"FASTDATA_CONN_{item.Key.ToUpper()}";
                var envConnStr = Environment.GetEnvironmentVariable(envVarName);
                if (!string.IsNullOrEmpty(envConnStr))
                {
                    item.ConnStr = envConnStr;
                }
            }
        }

        /// <summary>
        /// Resolve active environment from config or environment variable
        /// Priority: FASTDATA_ACTIVE env var > db.config Active attribute > default "dev"
        /// </summary>
        private static string ResolveActiveEnvironment(DataConfig baseConfig)
        {
            // 1. Check environment variable first
            var envActive = Environment.GetEnvironmentVariable("FASTDATA_ACTIVE");
            if (!string.IsNullOrEmpty(envActive))
                return NormalizeEnvironment(envActive);

            // 2. Check db.config Active attribute
            if (baseConfig != null && !string.IsNullOrEmpty(baseConfig.Active))
                return NormalizeEnvironment(baseConfig.Active);

            // 3. Default to "dev"
            return "dev";
        }

        /// <summary>
        /// Normalize environment name aliases
        /// dev/development/Development → dev
        /// pro/production/Production → pro
        /// staging/Staging → staging
        /// Others: lowercase
        /// </summary>
        private static string NormalizeEnvironment(string env)
        {
            if (string.IsNullOrEmpty(env)) return "dev";
            
            var lower = env.ToLower();
            switch (lower)
            {
                case "dev":
                case "development":
                    return "dev";
                case "pro":
                case "production":
                    return "pro";
                case "staging":
                    return "staging";
                default:
                    return lower;
            }
        }

        /// <summary>
        /// Get Redis config from current active environment config
        /// </summary>
        /// <returns>Redis config, or null if not configured</returns>
        public static RedisConfig GetRedisConfig(string dbFile = "db.config")
        {
            var cacheKey = "FastData.redis.config";
            
            if (DbCache.Exists(CacheType.Web, cacheKey))
                return DbCache.Get<RedisConfig>(CacheType.Web, cacheKey);

            DataConfig config = null;
            
            // Load base config to get Active value
            var baseConfig = TryLoadConfig(dbFile);
            var activeEnv = ResolveActiveEnvironment(baseConfig);

            // Try environment-specific config
            if (!string.IsNullOrEmpty(activeEnv))
            {
                var envDbFile = $"db.{activeEnv}.config";
                config = TryLoadConfig(envDbFile);
            }

            // Fall back to base config
            if (config == null)
                config = baseConfig;

            // Extract Redis config
            var redisConfig = new RedisConfig();
            if (config?.Redis != null)
            {
                redisConfig.Server = config.Redis.Server;
                redisConfig.Db = config.Redis.Db;
                redisConfig.Password = config.Redis.Password;
                redisConfig.ConnectTimeout = config.Redis.ConnectTimeout;
                redisConfig.SyncTimeout = config.Redis.SyncTimeout;
            }

            // Check environment variable overrides
            var envServer = Environment.GetEnvironmentVariable("FASTDATA_REDIS_SERVER");
            if (!string.IsNullOrEmpty(envServer))
                redisConfig.Server = envServer;

            var envDb = Environment.GetEnvironmentVariable("FASTDATA_REDIS_DB");
            if (!string.IsNullOrEmpty(envDb) && int.TryParse(envDb, out var dbIndex))
                redisConfig.Db = dbIndex;

            var envPassword = Environment.GetEnvironmentVariable("FASTDATA_REDIS_PASSWORD");
            if (!string.IsNullOrEmpty(envPassword))
                redisConfig.Password = envPassword;

            DbCache.Set<RedisConfig>(CacheType.Web, cacheKey, redisConfig);
            return redisConfig;
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
                Assembly assembly = null;
                
                // Try to find assembly from current AppDomain first
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == projectName && !asm.IsDynamic)
                    {
                        assembly = asm;
                        break;
                    }
                }
                
                // If not found in AppDomain, try Assembly.Load
                if (assembly == null)
                {
                    try
                    {
                        assembly = Assembly.Load(projectName);
                    }
                    catch
                    {
                        // Assembly not found in default probing paths
                        // Try to load from file system
                        var basePath = AppDomain.CurrentDomain.BaseDirectory;
                        var possiblePaths = new[]
                        {
                            Path.Combine(basePath, projectName + ".dll"),
                            Path.Combine(basePath, "bin", projectName + ".dll"),
                            Path.Combine(basePath, "publish", projectName + ".dll")
                        };
                        
                        foreach (var path in possiblePaths)
                        {
                            if (File.Exists(path))
                            {
                                try
                                {
                                    assembly = Assembly.LoadFrom(path);
                                    break;
                                }
                                catch
                                {
                                    // Failed to load from this path
                                }
                            }
                        }
                        
                        if (assembly == null)
                            return null;
                    }
                }
                
                var resourceName = string.Format("{0}.{1}", projectName, dbFile);
                using (var resource = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resource != null)
                    {
                        using (var reader = new StreamReader(resource))
                        {
                            var content = reader.ReadToEnd();
                            return ParseXmlConfig(content);
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

        /// <summary>
        /// Parse XML config content into DataConfig object
        /// </summary>
        private static DataConfig ParseXmlConfig(string xmlContent)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlContent);

                var dataConfigNode = doc.SelectSingleNode("//DataConfig");
                if (dataConfigNode == null)
                    return null;

                var config = new DataConfig();

                // Read Active attribute
                var activeAttr = dataConfigNode.Attributes?["Active"];
                if (activeAttr != null)
                    config["Active"] = activeAttr.Value;

                // Read Default attribute
                var defaultAttr = dataConfigNode.Attributes?["Default"];
                if (defaultAttr != null)
                    config["Default"] = defaultAttr.Value;

                // Read Connections
                var connectionsNode = dataConfigNode.SelectSingleNode("Connections");
                if (connectionsNode != null)
                {
                    var collection = new CollectionConfig();
                    foreach (System.Xml.XmlNode addNode in connectionsNode.SelectNodes("Add"))
                    {
                        var element = new ElementConfig();
                        element.Key = addNode.Attributes?["Key"]?.Value ?? "";
                        element.Provider = addNode.Attributes?["Provider"]?.Value ?? "";
                        element.ConnStr = addNode.Attributes?["ConnStr"]?.Value ?? "";
                        element.IsDefault = addNode.Attributes?["IsDefault"]?.Value?.ToLower() == "true";
                        collection.AddElement(element);
                    }
                    config["Connections"] = collection;
                }

                // Read Redis
                var redisNode = dataConfigNode.SelectSingleNode("Redis");
                if (redisNode != null)
                {
                    var redisElement = new RedisElement();
                    redisElement.Server = redisNode.Attributes?["Server"]?.Value ?? "";
                    redisElement.Db = int.TryParse(redisNode.Attributes?["Db"]?.Value, out var db) ? db : 0;
                    redisElement.Password = redisNode.Attributes?["Password"]?.Value ?? "";
                    redisElement.ConnectTimeout = int.TryParse(redisNode.Attributes?["ConnectTimeout"]?.Value, out var ct) ? ct : 5000;
                    redisElement.SyncTimeout = int.TryParse(redisNode.Attributes?["SyncTimeout"]?.Value, out var st) ? st : 5000;
                    config["Redis"] = redisElement;
                }

                return config;
            }
            catch
            {
                return null;
            }
        }

        #region 公共 API（供外部项目调用）
        /// <summary>
        /// 获取当前活跃环境名称
        /// </summary>
        public static string GetActiveEnvironment()
        {
            var baseConfig = TryLoadConfig("db.config");
            return ResolveActiveEnvironment(baseConfig);
        }

        /// <summary>
        /// 获取 Redis 配置（完整对象，供内部使用）
        /// </summary>
        public static RedisConfig GetRedisConfigPublic()
        {
            return GetRedisConfig();
        }

        /// <summary>
        /// 获取数据库连接列表（密码脱敏）
        /// </summary>
        public static List<Dictionary<string, string>> GetConnectionSummaries()
        {
            var result = new List<Dictionary<string, string>>();
            var cacheKey = "FastData.db.config";
            var cacheType = "web";
            
            // Trigger config loading if cache is empty
            if (!Base.DbCache.Exists(cacheType, cacheKey))
            {
                GetConfig();
            }
            
            if (!Base.DbCache.Exists(cacheType, cacheKey))
                return result;

            var list = Base.DbCache.Get<List<ConfigModel>>(cacheType, cacheKey);
            foreach (var c in list)
            {
                result.Add(new Dictionary<string, string>
                {
                    { "key", c.Key ?? "" },
                    { "dbType", c.DbType ?? "" },
                    { "provider", c.ProviderName ?? "" },
                    { "connStr", MaskConnStr(c.ConnStr) },
                    { "isOutSql", c.IsOutSql.ToString() },
                    { "isOutError", c.IsOutError.ToString() }
                });
            }
            return result;
        }

        /// <summary>
        /// 获取 Redis 配置摘要（密码脱敏）
        /// </summary>
        public static Dictionary<string, string> GetRedisSummary()
        {
            var redis = GetRedisConfig();
            if (redis == null)
                return new Dictionary<string, string> { { "configured", "false" } };

            return new Dictionary<string, string>
            {
                { "configured", "true" },
                { "server", redis.Server ?? "" },
                { "db", redis.Db.ToString() },
                { "password", string.IsNullOrEmpty(redis.Password) ? "" : "***" },
                { "connectTimeout", redis.ConnectTimeout.ToString() },
                { "syncTimeout", redis.SyncTimeout.ToString() }
            };
        }

        private static string MaskConnStr(string connStr)
        {
            if (string.IsNullOrEmpty(connStr)) return "";
            return System.Text.RegularExpressions.Regex.Replace(
                connStr, @"(?i)(pwd|password)=[^;]*", "$1=***");
        }
        #endregion
    }
}
