using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 异步操作辅助类
    ///
    /// 职责：在同步上下文（如 UI 线程）中安全执行 CPU 密集型操作。
    ///
    /// 重要说明：
    /// - 本类使用 TaskFactory.StartNew 包装同步操作，避免在同步上下文中阻塞
    /// - 适用于无法修改为真正 async/await 的遗留代码
    /// - 新代码应优先使用原生 async/await
    /// - 不应用于 I/O 密集型操作（应使用真正的异步 API）
    ///
    /// 实现细节：
    /// - 使用独立的 TaskFactory 配置，避免捕获同步上下文
    /// - 使用 CancellationToken.None，不支持取消
    /// - 使用 TaskScheduler.Default，强制在线程池中执行
    /// </summary>
    internal static class AsyncHelper
    {
        private static readonly TaskFactory _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        /// <summary>
        /// 在线程池中同步执行 Func 并返回 Task&lt;T&gt;
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的同步函数</param>
        /// <returns>表示异步操作的 Task</returns>
        /// <exception cref="ArgumentNullException">func 为 null 时抛出</exception>
        public static Task<T> RunSyncAsAsync<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            return _taskFactory.StartNew(func);
        }

        /// <summary>
        /// RunSyncAsAsync 的别名方法
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的同步函数</param>
        /// <returns>表示异步操作的 Task</returns>
        public static Task<T> RunAsync<T>(Func<T> func) => RunSyncAsAsync(func);

        /// <summary>
        /// 将 Func 包装为 Lazy&lt;T&gt;
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要延迟执行的函数</param>
        /// <returns>Lazy&lt;T&gt; 对象</returns>
        public static Lazy<T> ToLazy<T>(Func<T> func)
        {
            return new Lazy<T>(func);
        }

        /// <summary>
        /// 在线程池中异步创建 Lazy&lt;T&gt;
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要延迟执行的函数</param>
        /// <returns>表示 Lazy&lt;T&gt; 的 Task</returns>
        public static Task<Lazy<T>> RunSyncAsLazyAsync<T>(Func<T> func)
        {
            return _taskFactory.StartNew(() => new Lazy<T>(func));
        }

        /// <summary>
        /// RunSyncAsLazyAsync 的别名方法
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要延迟执行的函数</param>
        /// <returns>表示 Lazy&lt;T&gt; 的 Task</returns>
        public static Task<Lazy<T>> RunLazyAsync<T>(Func<T> func) => RunSyncAsLazyAsync(func);
    }
}
