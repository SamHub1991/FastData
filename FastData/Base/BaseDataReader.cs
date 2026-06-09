using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using FastData.Property;
using FastData.DbTypes;
using FastData.Model;
using FastData.CacheModel;
using System.Linq;

namespace FastData.Base
{
    /// <summary>
    /// datareader操作类
    /// </summary>
    internal static class BaseDataReader
    {
        /// <summary>
        /// Oracle CLOB/BLOB 反射方法缓存（避免每次调用反射遍历）
        /// </summary>
        private static class OracleMethodCache
        {
            /// <summary>OracleDataReader.GetOracleClob(int) 方法</summary>
            public static readonly MethodInfo GetOracleClob;
            /// <summary>OracleDataReader.GetOracleBlob(int) 方法</summary>
            public static readonly MethodInfo GetOracleBlob;
            /// <summary>OracleClob.get_Value 方法</summary>
            public static readonly MethodInfo GetValue;
            /// <summary>OracleClob.Dispose 方法</summary>
            public static readonly MethodInfo Dispose;
            /// <summary>OracleClob.Close 方法</summary>
            public static readonly MethodInfo Close;

            static OracleMethodCache()
            {
                try
                {
                    var oracleReaderType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDataReader, Oracle.ManagedDataAccess");
                    if (oracleReaderType != null)
                    {
                        GetOracleClob = oracleReaderType.GetMethod("GetOracleClob");
                        GetOracleBlob = oracleReaderType.GetMethod("GetOracleBlob");
                        var clobType = Type.GetType("Oracle.ManagedDataAccess.Types.OracleClob, Oracle.ManagedDataAccess");
                        if (clobType != null)
                        {
                            GetValue = clobType.GetMethod("get_Value");
                            Dispose = clobType.GetMethod("Dispose");
                            Close = clobType.GetMethod("Close");
                        }
                    }
                }
                catch
                {
                    // 忽略 Oracle 类型加载失败（非 Oracle 环境运行时）
                }
            }
        }
        #region to list
        /// <summary>
        /// 将 DataReader 转换为实体对象列表
        /// </summary>
        /// <typeparam name="T">实体类型（需有无参构造函数）</typeparam>
        /// <param name="dr">数据库读取器</param>
        /// <param name="config">数据库配置模型（用于获取数据库类型以处理列名大小写）</param>
        /// <param name="field">要读取的字段名列表，为 null 或空时读取全部字段</param>
        /// <returns>实体对象列表</returns>
        public static List<T> ToList<T>(DbDataReader dr, ConfigModel config, List<string> field = null) where T : class, new()
        {
            var list = new List<T>();
            var dynSet = new Property.DynamicSet<T>();

            if (dr == null)
                return list;

            var propertyList = PropertyCache.GetPropertyInfo<T>(config.IsPropertyCache);

            // 构建字段名 → PropertyModel 的 O(1) 查找字典（忽略大小写）
            Dictionary<string, PropertyModel> propertyDict = null;
            if (field != null && field.Count > 0)
            {
                propertyDict = new Dictionary<string, PropertyModel>(propertyList.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var info in propertyList)
                {
                    propertyDict[info.Name] = info;
                }
            }

            while (dr.Read())
            {
                var item = new T();

                if (propertyDict == null)
                {
                    // 读取全部字段
                    foreach (var info in propertyList)
                    {
                        if (info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                            continue;

                        item = SetValue<T>(item, dynSet, dr, info, config);
                    }
                }
                else
                {
                    // 按指定字段读取（O(1) 字典查找替代 O(n) List.Find）
                    for (var i = 0; i < field.Count; i++)
                    {
                        var fieldName = field[i];
                        if (fieldName.Contains("."))
                            fieldName = fieldName.Substring(fieldName.IndexOf(".") + 1);

                        if (propertyDict.TryGetValue(fieldName, out var info))
                        {
                            item = SetValue<T>(item, dynSet, dr, info, config);
                        }
                    }
                }

                list.Add(item);
            }
                        
            return list;
        }
        #endregion

        /// <summary>
        /// 从 DataReader 读取字段值并设置到实体属性
        /// 针对 Oracle 的 CLOB/BLOB 类型做特殊处理
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="item">实体对象</param>
        /// <param name="dynSet">动态属性设置器</param>
        /// <param name="dr">数据库读取器</param>
        /// <param name="info">属性元数据模型</param>
        /// <param name="config">数据库配置模型</param>
        /// <returns>设置属性后的实体对象</returns>
        private static T SetValue<T>(T item, Property.DynamicSet<T> dynSet, DbDataReader dr, PropertyModel info, ConfigModel config)
        {
            try
            {
                var ordinalName = info.Name;
                if (config.DbType == DataDbType.Oracle)
                    ordinalName = info.Name.ToUpper();
                else if (config.DbType == DataDbType.PostgreSql)
                    ordinalName = info.Name.ToLower();
                var id = dr.GetOrdinal(ordinalName);
                if (DataDbType.Oracle == config.DbType)
                {
                    object value = null;
                    var typeName = dr.GetDataTypeName(id).ToLower();
                    if (typeName == "clob" || typeName == "nclob")
                    {
                        var getOracleClob = OracleMethodCache.GetOracleClob;
                        if (getOracleClob != null)
                        {
                            var temp = getOracleClob.Invoke(dr, new object[] { id });
                            if (temp != null)
                            {
                                var getValue = OracleMethodCache.GetValue;
                                if (getValue != null && !dr.IsDBNull(id))
                                    value = getValue.Invoke(temp, null);
                                OracleMethodCache.Close?.Invoke(temp, null);
                                OracleMethodCache.Dispose?.Invoke(temp, null);
                            }
                        }
                    }
                    else if (typeName == "blob")
                    {
                        var getOracleBlob = OracleMethodCache.GetOracleBlob;
                        if (getOracleBlob != null)
                        {
                            var temp = getOracleBlob.Invoke(dr, new object[] { id });
                            if (temp != null)
                            {
                                var getValue = OracleMethodCache.GetValue;
                                if (getValue != null && !dr.IsDBNull(id))
                                    value = getValue.Invoke(temp, null);
                                OracleMethodCache.Close?.Invoke(temp, null);
                                OracleMethodCache.Dispose?.Invoke(temp, null);
                            }
                        }
                    }
                    else
                        value = dr.GetValue(id);

                    if (!dr.IsDBNull(id))
                    {
                        if (info.PropertyType.Name == "Nullable`1" && info.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            dynSet.SetValue(item, info.Name, Convert.ChangeType(value, Nullable.GetUnderlyingType(info.PropertyType)), config.IsPropertyCache);
                        else
                            dynSet.SetValue(item, info.Name, Convert.ChangeType(value, info.PropertyType), config.IsPropertyCache);
                    }
                }
                else
                {
                    if (!dr.IsDBNull(id))
                    {
                        if (info.PropertyType.Name == "Nullable`1" && info.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            dynSet.SetValue(item, info.Name, Convert.ChangeType(dr.GetValue(id), Nullable.GetUnderlyingType(info.PropertyType)), config.IsPropertyCache);
                        else
                            dynSet.SetValue(item, info.Name, Convert.ChangeType(dr.GetValue(id), info.PropertyType), config.IsPropertyCache);
                    }
                }

                return item;
            }
            catch (Exception ex) { DbLog.LogException(config.IsOutError, config.DbType, ex, "ToModel", ""); return item; }
        }

    }
}
