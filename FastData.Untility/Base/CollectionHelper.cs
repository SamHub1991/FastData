using System;
using System.Collections.Generic;
using System.Linq;

namespace FastUntility.Base
{
    /// <summary>
    /// 集合工具类
    /// </summary>
    public static class CollectionHelper
    {
        #region 空值检查
        /// <summary>
        /// 是否为空或空集合
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        /// <summary>
        /// 是否不为空且有元素
        /// </summary>
        public static bool HasValue<T>(this IEnumerable<T> source)
        {
            return source != null && source.Any();
        }
        #endregion

        #region 安全操作
        /// <summary>
        /// 安全遍历（空集合不执行）
        /// </summary>
        public static void ForEachSafe<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null || action == null)
                return;

            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// 安全遍历（带索引）
        /// </summary>
        public static void ForEachSafe<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null || action == null)
                return;

            var index = 0;
            foreach (var item in source)
            {
                action(item, index);
                index++;
            }
        }

        /// <summary>
        /// 安全转换列表（null 返回空列表）
        /// </summary>
        public static List<T> ToListSafe<T>(this IEnumerable<T> source)
        {
            return source?.ToList() ?? new List<T>();
        }

        /// <summary>
        /// 安全转换数组（null 返回空数组）
        /// </summary>
        public static T[] ToArraySafe<T>(this IEnumerable<T> source)
        {
            return source?.ToArray() ?? FrameworkCompat.EmptyArray<T>();
        }
        #endregion

        #region 分页
        /// <summary>
        /// 分页
        /// </summary>
        public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            if (source == null)
                return Enumerable.Empty<T>();

            return source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// 分页（返回 Tuple: 数据 + 总数）
        /// </summary>
        public static (List<T> Data, int Total) PageWithTotal<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            if (source == null)
                return (new List<T>(), 0);

            var list = source.ToList();
            var data = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return (data, list.Count);
        }
        #endregion

        #region 去重与分组
        /// <summary>
        /// 按指定属性去重
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null)
                return Enumerable.Empty<T>();

            var seen = new HashSet<TKey>();
            return source.Where(item => seen.Add(keySelector(item)));
        }

        /// <summary>
        /// 按指定属性分组，返回字典
        /// </summary>
        public static Dictionary<TKey, List<T>> GroupToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null)
                return new Dictionary<TKey, List<T>>();

            return source.GroupBy(keySelector)
                         .ToDictionary(g => g.Key, g => g.ToList());
        }
        #endregion

        #region 集合运算
        /// <summary>
        /// 交集
        /// </summary>
        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            if (source == null || other == null)
                return Enumerable.Empty<T>();

            return source.Intersect(other);
        }

        /// <summary>
        /// 差集
        /// </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            if (source == null || other == null)
                return source ?? Enumerable.Empty<T>();

            return source.Except(other);
        }

        /// <summary>
        /// 并集（去重）
        /// </summary>
        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            if (source == null && other == null)
                return Enumerable.Empty<T>();

            return (source ?? Enumerable.Empty<T>()).Union(other ?? Enumerable.Empty<T>());
        }

        /// <summary>
        /// 合并（不去重）
        /// </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            if (source == null && other == null)
                return Enumerable.Empty<T>();

            return (source ?? Enumerable.Empty<T>()).Concat(other ?? Enumerable.Empty<T>());
        }
        #endregion

        #region 转换
        /// <summary>
        /// 转换为字典（安全）
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionarySafe<T, TKey, TValue>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector)
        {
            if (source == null)
                return new Dictionary<TKey, TValue>();

            return source.ToDictionary(keySelector, valueSelector);
        }

        /// <summary>
        /// 转换为 SortedList
        /// </summary>
        public static SortedList<TKey, TValue> ToSortedList<T, TKey, TValue>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector) where TKey : notnull
        {
            if (source == null)
                return new SortedList<TKey, TValue>();

            var list = new SortedList<TKey, TValue>();
            foreach (var item in source)
            {
                list[keySelector(item)] = valueSelector(item);
            }
            return list;
        }
        #endregion

        #region 批量操作
        /// <summary>
        /// 批量分割
        /// </summary>
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null || batchSize <= 0)
                yield break;

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
                yield return batch;
        }

        /// <summary>
        /// 批量执行
        /// </summary>
        public static void BatchExecute<T>(this IEnumerable<T> source, int batchSize, Action<List<T>> action)
        {
            if (source == null || action == null)
                return;

            foreach (var batch in source.Batch(batchSize))
            {
                action(batch);
            }
        }
        #endregion

        #region 统计
        /// <summary>
        /// 安全求和（null 返回 0）
        /// </summary>
        public static decimal SumSafe<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            if (source == null)
                return 0;

            return source.Sum(selector);
        }

        /// <summary>
        /// 安全最大值（null 返回默认值）
        /// </summary>
        public static T MaxSafe<T>(this IEnumerable<T> source, T defaultValue = default)
        {
            if (source == null || !source.Any())
                return defaultValue;

            return source.Max();
        }

        /// <summary>
        /// 安全最小值（null 返回默认值）
        /// </summary>
        public static T MinSafe<T>(this IEnumerable<T> source, T defaultValue = default)
        {
            if (source == null || !source.Any())
                return defaultValue;

            return source.Min();
        }

        /// <summary>
        /// 安全平均值（null 返回 0）
        /// </summary>
        public static decimal AverageSafe<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            if (source == null || !source.Any())
                return 0;

            return source.Average(selector);
        }
        #endregion

        #region 随机
        /// <summary>
        /// 随机打乱
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return Enumerable.Empty<T>();

            var random = new Random();
            return source.OrderBy(_ => random.Next());
        }

        /// <summary>
        /// 随机取一个元素
        /// </summary>
        public static T Random<T>(this IEnumerable<T> source)
        {
            if (source == null || !source.Any())
                throw new InvalidOperationException("集合为空");

            var list = source.ToList();
            var random = new Random();
            return list[random.Next(list.Count)];
        }

        /// <summary>
        /// 随机取 N 个元素
        /// </summary>
        public static List<T> Random<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
                return new List<T>();

            return source.Shuffle().Take(count).ToList();
        }
        #endregion

        #region 树形结构
        /// <summary>
        /// 转换为树形结构
        /// </summary>
        public static List<TreeNode<T>> ToTree<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> idSelector,
            Func<T, TKey> parentIdSelector,
            TKey rootParentId = default) where TKey : notnull
        {
            if (source == null)
                return new List<TreeNode<T>>();

            var list = source.ToList();
            var lookup = list.ToDictionary(idSelector, item => new TreeNode<T> { Data = item });

            foreach (var item in list)
            {
                var nodeId = idSelector(item);
                var parentId = parentIdSelector(item);

                if (lookup.ContainsKey(nodeId))
                {
                    var node = lookup[nodeId];
                    if (parentId.Equals(rootParentId))
                    {
                        // 根节点
                    }
                    else if (lookup.ContainsKey(parentId))
                    {
                        lookup[parentId].Children.Add(node);
                    }
                }
            }

            return lookup.Values.Where(n => parentIdSelector(n.Data).Equals(rootParentId)).ToList();
        }
        #endregion
    }

    /// <summary>
    /// 树节点
    /// </summary>
    public class TreeNode<T>
    {
        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public List<TreeNode<T>> Children { get; set; } = new List<TreeNode<T>>();
    }
}
