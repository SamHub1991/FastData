using System;
using System.Reflection;

namespace FastData.Base
{
    internal static class TableNameHelper
    {
        public static string GetTableName<T>()
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<Property.TableAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.Name))
                return attr.Name;
            return type.Name;
        }
    }
}