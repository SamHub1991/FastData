using System;

namespace FastData.Aop
{
    /// <summary>
    /// FastData AOP 拦截接口
    /// 
    /// 实现此接口可以在 SQL 执行前后进行拦截，用于日志记录、性能监控、异常处理等。
    /// 
    /// 使用示例：
    /// <code>
    /// public class LoggingAop : IFastAop
    /// {
    ///     public void Before(BeforeContext context)
    ///     {
    ///         Console.WriteLine($"执行 SQL: {context.Sql}");
    ///     }
    /// 
    ///     public void After(AfterContext context)
    ///     {
    ///         Console.WriteLine($"SQL 执行完成，耗时: {context.ElapsedMs}ms");
    ///     }
    /// 
    ///     public void Exception(ExceptionContext context)
    ///     {
    ///         Console.WriteLine($"SQL 执行异常: {context.Exception.Message}");
    ///     }
    /// 
    ///     public void MapBefore(MapBeforeContext context)
    ///     {
    ///         Console.WriteLine($"执行 Map SQL: {context.MapName}");
    ///     }
    /// 
    ///     public void MapAfter(MapAfterContext context)
    ///     {
    ///         Console.WriteLine($"Map SQL 执行完成");
    ///     }
    /// }
    /// 
    /// // 注册 AOP
    /// FastMap.InstanceMap(aop: new LoggingAop());
    /// </code>
    /// </summary>
    public interface IFastAop
    {
        /// <summary>
        /// Map SQL 执行前拦截
        /// </summary>
        /// <param name="context">Map SQL 上下文（包含 Map 名称、参数等）</param>
        void MapBefore(MapBeforeContext context);

        /// <summary>
        /// Map SQL 执行后拦截
        /// </summary>
        /// <param name="context">Map SQL 上下文（包含执行结果等）</param>
        void MapAfter(MapAfterContext context);

        /// <summary>
        /// 普通 SQL 执行前拦截
        /// </summary>
        /// <param name="context">SQL 上下文（包含 SQL 语句、参数等）</param>
        void Before(BeforeContext context);

        /// <summary>
        /// 普通 SQL 执行后拦截
        /// </summary>
        /// <param name="context">SQL 上下文（包含执行结果、耗时等）</param>
        void After(AfterContext context);

        /// <summary>
        /// SQL 执行异常拦截
        /// </summary>
        /// <param name="context">异常上下文（包含异常信息、SQL 语句等）</param>
        void Exception(ExceptionContext context);
    }
}
