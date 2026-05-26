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
        /// <summary>
        /// 将同步操作转为异步执行（使用 Task.Run）
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">同步操作</param>
        /// <returns>异步任务</returns>
        public static Task<T> RunSyncAsAsync<T>(Func<T> func)
        {
            return Task.Run(func);
        }

        /// <summary>
        /// 将同步操作转为延迟执行
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">同步操作</param>
        /// <returns>延迟执行对象</returns>
        public static Lazy<T> ToLazy<T>(Func<T> func)
        {
            return new Lazy<T>(func);
        }

        /// <summary>
        /// 将同步操作转为异步延迟执行
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">同步操作</param>
        /// <returns>异步延迟执行对象</returns>
        public static Task<Lazy<T>> RunSyncAsLazyAsync<T>(Func<T> func)
        {
            return Task.Run(() => new Lazy<T>(func));
        }
    }
}