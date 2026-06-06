using System;
using System.Collections.Generic;
using System.Data.Common;
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

            while (dr.Read())
            {
                var item = new T();

                if (field == null || field.Count == 0)
                {
                    foreach (var info in propertyList)
                    {
                        if (info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                            continue;

                        item = SetValue<T>(item, dynSet, dr, info, config);
                    }
                }
                else
                {
                    for (var i = 0; i < field.Count; i++)
                    {
                        var fieldName = field[i];
                        if (fieldName.Contains("."))
                            fieldName = fieldName.Substring(fieldName.IndexOf(".") + 1);

                        if (propertyList.Exists(a => a.Name.ToLower() == fieldName.ToLower()))
                        {
                            var info = propertyList.Find(a => a.Name.ToLower() == fieldName.ToLower());
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
                        dr.GetType().GetMethods().ToList().ForEach(m =>
                        {
                            if (m.Name == "GetOracleClob")
                            {
                                var param = new object[1];
                                param[0] = id;
                                var temp = m.Invoke(dr, param);
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "get_Value" && !dr.IsDBNull(id))
                                        value = v.Invoke(temp, null);
                                });
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "Close")
                                        v.Invoke(temp, null);
                                });
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "Dispose")
                                        v.Invoke(temp, null);
                                });
                            }
                        });
                    }
                    else if (typeName == "blob")
                    {
                        dr.GetType().GetMethods().ToList().ForEach(m =>
                        {
                            if (m.Name == "GetOracleBlob")
                            {
                                var param = new object[1];
                                param[0] = id;
                                var temp = m.Invoke(dr, param);
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "get_Value" && !dr.IsDBNull(id))
                                        value = v.Invoke(temp, null);
                                });
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "Close")
                                        v.Invoke(temp, null);
                                });
                                temp.GetType().GetMethods().ToList().ForEach(v =>
                                {
                                    if (v.Name == "Dispose")
                                        v.Invoke(temp, null);
                                });
                            }
                        });
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
