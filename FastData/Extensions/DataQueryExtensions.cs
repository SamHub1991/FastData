using System;
using System.Collections.Generic;
using System.Linq;
using FastData.Model;

namespace FastData
{
    /// <summary>
    /// DataQuery 扩展方法（支持 LINQ 操作）
    /// </summary>
    public static class DataQueryExtensions
    {
        /// <summary>
        /// 获取第一个元素或默认值
        /// </summary>
        public static T FirstOrDefault<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// 获取第一个元素或默认值（带条件）
        /// </summary>
        public static T FirstOrDefault<T>(this DataQuery<T> query, Func<T, bool> predicate) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.FirstOrDefault(predicate);
        }

        /// <summary>
        /// 获取第一个元素，不存在则抛出异常
        /// </summary>
        public static T First<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            if (list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");
            return list[0];
        }

        /// <summary>
        /// 获取单个元素，不存在或重复则抛出异常
        /// </summary>
        public static T Single<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            if (list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");
            if (list.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element");
            return list[0];
        }

        /// <summary>
        /// 获取单个元素或默认值
        /// </summary>
        public static T SingleOrDefault<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            if (list.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element");
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public static int Count<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.Count;
        }

        /// <summary>
        /// 获取元素数量（带条件）
        /// </summary>
        public static int Count<T>(this DataQuery<T> query, Func<T, bool> predicate) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.Count(predicate);
        }

        /// <summary>
        /// 是否包含元素
        /// </summary>
        public static bool Any<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.Count > 0;
        }

        /// <summary>
        /// 是否包含满足条件的元素
        /// </summary>
        public static bool Any<T>(this DataQuery<T> query, Func<T, bool> predicate) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.Any(predicate);
        }

        /// <summary>
        /// 是否所有元素都满足条件
        /// </summary>
        public static bool All<T>(this DataQuery<T> query, Func<T, bool> predicate) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.All(predicate);
        }

        /// <summary>
        /// 转换为 List
        /// </summary>
        public static List<T> ToList<T>(this DataQuery<T> query) where T : class, new()
        {
            return FastRead.ToList<T>(query);
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        public static T[] ToArray<T>(this DataQuery<T> query) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.ToArray();
        }

        /// <summary>
        /// 转换为 Dictionary
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(
            this DataQuery<T> query,
            Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.ToDictionary(keySelector, elementSelector);
        }

        /// <summary>
        /// 转换为 Dictionary
        /// </summary>
        public static Dictionary<TKey, T> ToDictionary<TKey, T>(
            this DataQuery<T> query,
            Func<T, TKey> keySelector) where T : class, new()
        {
            var list = FastRead.ToList<T>(query);
            return list.ToDictionary(keySelector);
        }

    }

    /// <summary>
    /// 空值处理扩展方法
    /// </summary>
    public static class NullSafetyExtensions
    {
        /// <summary>
        /// 如果为 null 则返回默认值
        /// </summary>
        public static T OrDefault<T>(this T value, T defaultValue = default) where T : class, new()
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// 如果为 null 或空字符串则返回默认值
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>处理后的字符串</returns>
        public static string OrEmpty(this string value, string defaultValue = "")
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// 如果为 null 则返回 0
        /// </summary>
        public static int OrZero(this int? value)
        {
            return value ?? 0;
        }

        /// <summary>
        /// 如果为 null 则返回 0
        /// </summary>
        public static decimal OrZero(this decimal? value)
        {
            return value ?? 0m;
        }

        /// <summary>
        /// 如果为 null 或空集合则返回空列表
        /// </summary>
        public static List<T> OrEmptyList<T>(this List<T> list)
        {
            return list ?? new List<T>();
        }

        /// <summary>
        /// 如果为 null 或空集合则返回空数组
        /// </summary>
        public static T[] OrEmptyArray<T>(this T[] array)
        {
            return array ?? new T[0];
        }

        /// <summary>
        /// 安全获取 List 元素
        /// </summary>
        public static T SafeGet<T>(this List<T> list, int index, T defaultValue = default) where T : class, new()
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }

        /// <summary>
        /// 安全获取数组元素
        /// </summary>
        public static T SafeGet<T>(this T[] array, int index, T defaultValue = default) where T : class, new()
        {
            if (array == null || index < 0 || index >= array.Length)
                return defaultValue;
            return array[index];
        }
    }

    /// <summary>
    /// 空值安全的结果包装类
    /// </summary>
    public class NullableResult<T> where T : class, new()
    {
        private readonly T _value;
        private readonly bool _hasValue;

        public NullableResult(T value)
        {
            _value = value;
            _hasValue = value != null;
        }

        public T Value => _value;

        public bool HasValue => _hasValue;

        public T OrDefault(T defaultValue = default)
        {
            return _hasValue ? _value : defaultValue;
        }

        public T OrThrow(Exception exception = null)
        {
            if (!_hasValue)
                throw exception ?? new InvalidOperationException("Value is null");
            return _value;
        }

        public static implicit operator T(NullableResult<T> result)
        {
            return result._value;
        }

        public static implicit operator NullableResult<T>(T value)
        {
            return new NullableResult<T>(value);
        }
    }

    /// <summary>
    /// NullableResult 扩展
    /// </summary>
    public static class NullableResultExtensions
    {
        /// <summary>
        /// 包装结果为 NullableResult
        /// </summary>
        public static NullableResult<T> ToNullable<T>(this T value) where T : class, new()
        {
            return new NullableResult<T>(value);
        }

        /// <summary>
        /// 安全转换为 NullableResult
        /// </summary>
        public static NullableResult<T> Safe<T>(this T value) where T : class, new()
        {
            return new NullableResult<T>(value);
        }
    }
}
