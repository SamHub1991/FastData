using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using FastData.CacheModel;
using FastData.Model;

namespace FastData.Property
{
    /// <summary>
    /// 属性缓存类
    /// 统一使用 ConcurrentDictionary 进行反射元数据缓存，避免 MemoryCache 的锁竞争和内存压力驱逐
    /// </summary>
    internal static class PropertyCache
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> _propertyCache =
            new ConcurrentDictionary<string, PropertyInfo[]>();

        private static readonly ConcurrentDictionary<string, List<PropertyModel>> _propertyModelCache =
            new ConcurrentDictionary<string, List<PropertyModel>>();

        /// <summary>
        /// ColumnAttribute 解析结果缓存（按类型全名 -> 列模型列表）
        /// 避免每次 BaseTable.Check 时重复解析 CustomAttributes
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<ColumnModel>> _columnAttributeCache =
            new ConcurrentDictionary<string, List<ColumnModel>>();

        /// <summary>
        /// DynamicSet 委托缓存（替代 DbCache，减少 MemoryCache 的锁开销）
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _dynamicSetDelegateCache =
            new ConcurrentDictionary<string, object>();

        /// <summary>
        /// DynamicGet 委托缓存（替代 DbCache，统一使用 ConcurrentDictionary）
        /// </summary>
        private static readonly ConcurrentDictionary<string, Func<object, string, object>> _dynamicGetDelegateCache =
            new ConcurrentDictionary<string, Func<object, string, object>>();

        private static readonly BindingFlags _bindingFlags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// ColumnModel 属性名 → 索引 快速查找表（懒加载）
        /// </summary>
        private static Dictionary<string, int> _columnModelPropIndex;

        private static readonly object _columnModelPropLock = new object();

        /// <summary>
        /// 获取类型的所有可读写属性（带缓存）
        /// </summary>
        public static PropertyInfo[] GetPropertiesCached<T>() where T : class
        {
            return _propertyCache.GetOrAdd(typeof(T).FullName, _ =>
                typeof(T).GetProperties(_bindingFlags));
        }

        /// <summary>
        /// 获取类型的所有可读写属性（带缓存，非泛型版本）
        /// </summary>
        public static PropertyInfo[] GetPropertiesCached(Type type)
        {
            return _propertyCache.GetOrAdd(type.FullName, _ =>
                type.GetProperties(_bindingFlags));
        }

        #region 泛型缓存属性成员

        /// <summary>
        /// 泛型缓存属性成员（使用 ConcurrentDictionary 替代 DbCache）
        /// </summary>
        public static List<PropertyModel> GetPropertyInfo<T>(bool IsCache = true, string projectName = null)
        {
            var key = typeof(T).FullName;

            if (IsCache && _propertyModelCache.TryGetValue(key, out var cachedList))
                return cachedList;

            var list = BuildPropertyModelList(typeof(T));

            if (IsCache)
                _propertyModelCache[key] = list;

            return list;
        }
        #endregion

        #region 缓存属性成员（object 重载）

        /// <summary>
        /// 缓存属性成员（object 重载，委托到泛型版本）
        /// </summary>
        public static List<PropertyModel> GetPropertyInfo(object model, bool IsCache = true, string projectName = null)
        {
            var type = model.GetType();
            var key = type.FullName;

            if (IsCache && _propertyModelCache.TryGetValue(key, out var cachedList))
                return cachedList;

            var list = BuildPropertyModelList(type);

            if (IsCache)
                _propertyModelCache[key] = list;

            return list;
        }
        #endregion

        #region 泛型特性成员

        /// <summary>
        /// 泛型特性成员
        /// 从 PropertyInfo 列表中提取 ColumnAttribute 特性信息（带缓存）
        /// </summary>
        public static List<ColumnModel> GetAttributesColumnInfo(string tableName, List<PropertyInfo> ListInfo)
        {
            // 按类型全名缓存结果（ColumnAttribute 在运行时不变）
            var typeKey = ListInfo.Count > 0 ? ListInfo[0].DeclaringType?.FullName + "_ColAttr" : null;
            if (typeKey != null && _columnAttributeCache.TryGetValue(typeKey, out var cached))
                return cached;

            var list = new List<ColumnModel>(ListInfo.Count);
            var propIndex = GetColumnModelPropIndex();

            foreach (var p in ListInfo)
            {
                var temp = new ColumnModel { Name = p.Name };
                var dynSet = new DynamicSet<ColumnModel>();

                foreach (var attr in p.CustomAttributes)
                {
                    if (attr.AttributeType.Name != typeof(ColumnAttribute).Name)
                        continue;

                    foreach (var n in attr.NamedArguments)
                    {
                        // O(1) 字典查找替代 O(n) 循环
                        if (propIndex.TryGetValue(n.MemberName.ToLower(), out var idx))
                        {
                            dynSet.SetValue(temp, n.MemberName, n.TypedValue.Value, true);
                        }
                    }
                }

                if (temp.IsKey && temp.IsNull)
                    temp.IsNull = false;

                list.Add(temp);
            }

            if (typeKey != null)
                _columnAttributeCache[typeKey] = list;

            return list;
        }

        /// <summary>
        /// 构建 ColumnModel 属性名 → 索引的快速查找字典（懒加载，线程安全）
        /// </summary>
        private static Dictionary<string, int> GetColumnModelPropIndex()
        {
            if (_columnModelPropIndex != null)
                return _columnModelPropIndex;

            lock (_columnModelPropLock)
            {
                if (_columnModelPropIndex != null)
                    return _columnModelPropIndex;

                var paramList = GetPropertyInfo<ColumnModel>(true);
                var dict = new Dictionary<string, int>(paramList.Count, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < paramList.Count; i++)
                {
                    dict[paramList[i].Name] = i;
                }
                _columnModelPropIndex = dict;
                return dict;
            }
        }
        #endregion

        #region 泛型缓存特性成员

        /// <summary>
        /// 泛型缓存特性成员
        /// 从特性列表中提取 TableAttribute 的 Comments
        /// </summary>
        public static string GetAttributesTableInfo(List<Attribute> listAttribute)
        {
            foreach (var a in listAttribute)
            {
                if (a is TableAttribute tableAttr)
                    return tableAttr.Comments;
            }
            return "";
        }
        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 构建 PropertyModel 列表（无分配优化）
        /// </summary>
        private static List<PropertyModel> BuildPropertyModelList(Type type)
        {
            var properties = type.GetProperties(_bindingFlags);
            var list = new List<PropertyModel>(properties.Length);
            foreach (var p in properties)
            {
                list.Add(new PropertyModel { Name = p.Name, PropertyType = p.PropertyType });
            }
            return list;
        }
        #endregion

        #region DynamicSet 委托缓存

        /// <summary>
        /// 获取或添加 DynamicSet 委托（使用 ConcurrentDictionary 替代 DbCache/MemoryCache）
        /// </summary>
        internal static TDelegate GetOrAddDynamicSetDelegate<TDelegate>(string key, Func<TDelegate> factory) where TDelegate : class
        {
            if (_dynamicSetDelegateCache.TryGetValue(key, out var cached))
                return cached as TDelegate;

            var result = factory();
            _dynamicSetDelegateCache[key] = result;
            return result;
        }

        /// <summary>
        /// 获取或添加 DynamicGet 委托（使用 ConcurrentDictionary 替代 DbCache/MemoryCache）
        /// </summary>
        internal static Func<object, string, object> GetOrAddDynamicGetDelegate(string key, Func<Func<object, string, object>> factory)
        {
            return _dynamicGetDelegateCache.GetOrAdd(key, _ => factory());
        }
        #endregion
    }
}
