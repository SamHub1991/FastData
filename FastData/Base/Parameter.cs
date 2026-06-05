using System.Collections.Generic;
using System.Data.Common;

namespace FastData.Core.Base
{
    /// <summary>
    /// 数据库参数合并工具类
    /// </summary>
    internal static class Parameter
    {
        /// <summary>
        /// 合并两个 DbParameter 集合
        /// </summary>
        /// <param name="target">目标参数集合（结果将追加到此集合）</param>
        /// <param name="source">源参数集合</param>
        /// <returns>合并后的参数集合</returns>
        public static List<DbParameter> ParamMerge(List<DbParameter> target, List<DbParameter> source)
        {
            if (source != null && source.Count > 0)
                target.AddRange(source);

            return target;
        }
    }
}
