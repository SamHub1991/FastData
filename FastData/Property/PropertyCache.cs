using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastData.CacheModel;
using FastData.Base;
using FastData.Config;
using FastData.Model;

namespace FastData.Property
{
    /// <summary>
    /// 缓存类
    /// </summary>
    internal static class PropertyCache
    {
        #region 泛型缓存属性成员
        /// <summary>
        /// 泛型缓存属性成员
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="IsCache">是否使用缓存</param>
        /// <param name="projectName">项目名称</param>
        /// <returns>属性模型列表</returns>
        public static List<PropertyModel> GetPropertyInfo<T>(bool IsCache = true, string projectName = null)
        {
            var list = new List<PropertyModel>();
            var key = string.Format("{0}.{1}", typeof(T).Namespace, typeof(T).Name);
            var cacheType = "web";

            if (IsCache)
            {
                if (DbCache.Exists(cacheType, key))
                    return DbCache.Get<List<PropertyModel>>(cacheType, key);
                else
                {
                    typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().ForEach(a =>
                        {
                            var temp = new PropertyModel();
                            temp.Name = a.Name;
                            temp.PropertyType = a.PropertyType;
                            list.Add(temp);
                        });

                    DbCache.Set<List<PropertyModel>>(cacheType, key, list);
                }
            }
            else
            {
                DbCache.Remove(cacheType, key);
                typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().ForEach(a =>
                {
                    var temp = new PropertyModel();
                    temp.Name = a.Name;
                    temp.PropertyType = a.PropertyType;
                    list.Add(temp);
                });
            }

            return list;
        }
        #endregion

        #region 缓存发属性成员
        public static List<PropertyModel> GetPropertyInfo(object model, bool IsCache = true, string projectName = null)
        {
            var list = new List<PropertyModel>();
            var key = string.Format("{0}.{1}", model.GetType().Namespace, model.GetType().Name);
            var cacheType = "web";

            if (IsCache)
            {
                if (DbCache.Exists(cacheType, key))
                    return DbCache.Get<List<PropertyModel>>(cacheType, key);
                else
                {
                    model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().ForEach(a =>
                    {
                        var temp = new PropertyModel();
                        temp.Name = a.Name;
                        temp.PropertyType = a.PropertyType;
                        list.Add(temp);
                    });

                    DbCache.Set<List<PropertyModel>>(cacheType, key, list);
                }
            }
            else
            {
                model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().ForEach(a =>
                {
                    var temp = new PropertyModel();
                    temp.Name = a.Name;
                    temp.PropertyType = a.PropertyType;
                    list.Add(temp);
                });
            }

            return list;
        }
        #endregion

        #region 泛型特性成员
        /// <summary>
        /// 泛型特性成员
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="ListInfo">属性信息列表</param>
        /// <returns>列模型列表</returns>
        public static List<ColumnModel> GetAttributesColumnInfo(string tableName, List<PropertyInfo> ListInfo)
        {
            var list = new List<ColumnModel>();

            ListInfo.ForEach(p => {
                var temp = new ColumnModel();
                temp.Name = p.Name;
                var dynSet = new DynamicSet<ColumnModel>();
                var paramList = GetPropertyInfo<ColumnModel>(true);

                p.CustomAttributes.ToList().ForEach(c => {
                    if (c.AttributeType.Name == typeof(ColumnAttribute).Name)
                    {
                        c.NamedArguments.ToList().ForEach(n => {
                            if (paramList.Exists(b => b.Name.ToLower() == n.MemberName.ToLower()))
                                dynSet.SetValue(temp, n.MemberName, n.TypedValue.Value, true);
                        });
                    }
                });

                if (temp.IsKey && temp.IsNull)
                    temp.IsNull = false;

                list.Add(temp);
            });
            
            return list;
        }
        #endregion

        #region 泛型缓存特性成员
        /// <summary>
        /// 泛型缓存特性成员
        /// </summary>
        public static string GetAttributesTableInfo(List<Attribute> listAttribute)
        {
            var result = "";
            listAttribute.ForEach(a => {
                if (a is TableAttribute)
                    result = (a as TableAttribute).Comments;
            });

            return result;
        }
        #endregion
    }
}
