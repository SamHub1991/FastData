using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastData.Model;
using FastData.Property;

namespace FastData.Extensions
{
    /// <summary>
    /// FastData 实用扩展方法
    /// </summary>
    public static class FastDataExtensions
    {
        /// <summary>
        /// 检查实体是否成功
        /// </summary>
        public static bool IsSuccess(this WriteReturn result)
        {
            return result != null && result.IsSuccess;
        }

        /// <summary>
        /// 获取错误消息
        /// </summary>
        public static string GetErrorMessage(this WriteReturn result)
        {
            return result?.Message ?? "未知错误";
        }

        /// <summary>
        /// 获取操作成功的消息
        /// </summary>
        public static string GetSuccessMessage(this WriteReturn result)
        {
            return result?.Message ?? "操作成功";
        }

        /// <summary>
        /// 如果失败则抛出异常
        /// </summary>
        public static WriteReturn ThrowIfFailed(this WriteReturn result, string operation = "操作")
        {
            if (result == null || !result.IsSuccess)
            {
                throw new FastDataException(string.Format("{0}失败: {1}", operation, result?.GetErrorMessage()));
            }
            return result;
        }

        /// <summary>
        /// 异步版本：如果失败则抛出异常
        /// </summary>
        public static async Task<WriteReturn> ThrowIfFailedAsync(this Task<WriteReturn> resultTask, string operation = "操作")
        {
            var result = await resultTask;
            return result.ThrowIfFailed(operation);
        }

        /// <summary>
        /// 获取操作结果（成功返回 true，失败返回 false）
        /// </summary>
        public static bool GetResult(this WriteReturn result)
        {
            return result != null && result.IsSuccess;
        }

        /// <summary>
        /// 链式调用：添加 WHERE 条件（仅在条件为真时）
        /// </summary>
        public static T WhereIf<T>(
            this T source,
            bool condition,
            Func<T, T> action) where T : class
        {
            if (condition && action != null)
            {
                return action(source);
            }
            return source;
        }

        /// <summary>
        /// 链式调用：在值不为 null 时执行
        /// </summary>
        public static T WhenNotNull<T, TValue>(
            this T source,
            TValue value,
            Func<T, TValue, T> action) where T : class where TValue : class
        {
            if (value != null && action != null)
            {
                return action(source, value);
            }
            return source;
        }

        /// <summary>
        /// 安全获取实体属性（防止 NullReferenceException）
        /// </summary>
        public static TValue SafeGet<T, TValue>(this T entity, Expression<Func<T, TValue>> propertySelector, TValue defaultValue = default) where T : class
        {
            if (entity == null || propertySelector == null)
                return defaultValue;

            try
            {
                var property = propertySelector.Body as MemberExpression;
                if (property?.Member != null)
                {
                    var value = typeof(T).GetProperty(property.Member.Name)?.GetValue(entity);
                    return value != null ? (TValue)value : defaultValue;
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 批量分页处理
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null)
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
            {
                yield return batch;
            }
        }

        /// <summary>
        /// 异步批量分页处理
        /// </summary>
        public static async IAsyncEnumerable<IEnumerable<T>> BatchAsync<T>(
            this IAsyncEnumerable<T> source,
            int batchSize)
        {
            var batch = new List<T>(batchSize);
            await foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        /// <summary>
        /// 重试执行（指定重试次数）
        /// </summary>
        public static T Retry<T>(this Func<T> action, int maxRetries = 3, int delayMs = 1000)
        {
            Exception lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < maxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(delayMs);
                    }
                }
            }

            throw new FastDataException(string.Format("操作失败，已重试 {0} 次", maxRetries), lastException);
        }

        /// <summary>
        /// 异步重试执行
        /// </summary>
        public static async Task<T> RetryAsync<T>(
            this Func<Task<T>> action,
            int maxRetries = 3,
            int delayMs = 1000)
        {
            Exception lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < maxRetries - 1)
                    {
                        await System.Threading.Tasks.Task.Delay(delayMs);
                    }
                }
            }

            throw new FastDataException(string.Format("操作失败，已重试 {0} 次", maxRetries), lastException);
        }

        /// <summary>
        /// 执行超时保护
        /// </summary>
        public static async Task<T> WithTimeout<T>(
            this Func<Task<T>> action,
            TimeSpan timeout)
        {
            using var cts = new System.Threading.CancellationTokenSource(timeout);
            try
            {
                return await action();
            }
            catch (System.OperationCanceledException)
            {
                throw new TimeoutException(string.Format("操作超时：{0}秒", timeout.TotalSeconds));
            }
        }

        /// <summary>
        /// 条件执行
        /// </summary>
        public static T ExecuteIf<T>(this T source, bool condition, Action<T> action) where T : class
        {
            if (condition && action != null)
            {
                action(source);
            }
            return source;
        }

        /// <summary>
        /// 转换为字典（自动处理空值）
        /// </summary>
        public static Dictionary<string, object> ToDictionarySafe<T>(this T entity) where T : class
        {
            var dict = new Dictionary<string, object>();
            if (entity == null)
                return dict;

            foreach (var prop in PropertyCache.GetPropertiesCached<T>())
            {
                if (prop.CanRead)
                {
                    var value = prop.GetValue(entity);
                    dict[prop.Name] = value ?? DBNull.Value;
                }
            }

            return dict;
        }
    }

    /// <summary>
    /// FastData 异常类
    /// </summary>
    public class FastDataException : Exception
    {
        /// <summary>Gets the FastData error code.</summary>
        public FastDataErrorCode ErrorCode { get; }

        /// <summary>Initializes a FastData exception.</summary>
        /// <param name="message">Exception message.</param>
        public FastDataException(string message) : base(message)
        {
        }

        /// <summary>Initializes a FastData exception with an inner exception.</summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public FastDataException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>Initializes a FastData exception with an error code.</summary>
        /// <param name="errorCode">FastData error code.</param>
        /// <param name="message">Exception message.</param>
        public FastDataException(FastDataErrorCode errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>Initializes a FastData exception with an error code and inner exception.</summary>
        /// <param name="errorCode">FastData error code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public FastDataException(FastDataErrorCode errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
