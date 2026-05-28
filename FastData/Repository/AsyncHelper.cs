using System;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 异步操作辅助类
    /// 注意：当前使用 Task.Run 包装同步操作，这是一个反模式
    /// 理想情况下应该使用真正的异步数据库操作（async/await）
    /// </summary>
    internal static class AsyncHelper
    {
        public static Task<T> RunSyncAsAsync<T>(Func<T> func)
        {
            return Task.Run(func);
        }

        public static Task<T> RunAsync<T>(Func<T> func) => RunSyncAsAsync(func);

        public static Lazy<T> ToLazy<T>(Func<T> func)
        {
            return new Lazy<T>(func);
        }

        public static Task<Lazy<T>> RunSyncAsLazyAsync<T>(Func<T> func)
        {
            return Task.Run(() => new Lazy<T>(func));
        }

        public static Task<Lazy<T>> RunLazyAsync<T>(Func<T> func) => RunSyncAsLazyAsync(func);
    }
}