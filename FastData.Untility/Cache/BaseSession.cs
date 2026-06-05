using System.Web;

namespace FastUntility.Cache
{
    /// <summary>
    /// Session 操作类
    /// 提供 Session 的增删查功能
    /// </summary>
    public static class BaseSession
    {
        /// <summary>
        /// 添加 Session 数据
        /// </summary>
        /// <param name="sessionName">Session 键名</param>
        /// <param name="value">Session 值</param>
        /// <param name="expiresMinutes">过期时间（分钟）</param>
        /// <returns>添加是否成功</returns>
        public static bool Add(string sessionName, object value, int expiresMinutes)
        {
            if (string.IsNullOrEmpty(sessionName))
                return false;

            var session = HttpContext.Current.Session;
            if (session == null)
                return false;

            session[sessionName] = value;
            session.Timeout = expiresMinutes;
            return true;
        }

        /// <summary>
        /// 添加 Session 数据（支持 HttpContextBase）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="sessionName">Session 键名</param>
        /// <param name="value">Session 值</param>
        /// <param name="expiresMinutes">过期时间（分钟）</param>
        /// <returns>添加是否成功</returns>
        public static bool AddAsync(HttpContextBase context, string sessionName, object value, int expiresMinutes)
        {
            if (context == null || string.IsNullOrEmpty(sessionName))
                return false;

            var session = context.Session;
            if (session == null)
                return false;

            session[sessionName] = value;
            session.Timeout = expiresMinutes;
            return true;
        }

        /// <summary>
        /// 添加 Session 数组数据
        /// </summary>
        /// <param name="sessionName">Session 键名</param>
        /// <param name="values">Session 值数组</param>
        /// <param name="expiresMinutes">过期时间（分钟）</param>
        /// <returns>添加是否成功</returns>
        public static bool Adds(string sessionName, object[] values, int expiresMinutes)
        {
            if (string.IsNullOrEmpty(sessionName) || values == null)
                return false;

            var session = HttpContext.Current.Session;
            if (session == null)
                return false;

            session[sessionName] = values;
            session.Timeout = expiresMinutes;
            return true;
        }

        /// <summary>
        /// 添加 Session 数组数据（支持 HttpContextBase）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="sessionName">Session 键名</param>
        /// <param name="values">Session 值数组</param>
        /// <param name="expiresMinutes">过期时间（分钟）</param>
        /// <returns>添加是否成功</returns>
        public static bool AddsAsync(HttpContextBase context, string sessionName, object[] values, int expiresMinutes)
        {
            if (context == null || string.IsNullOrEmpty(sessionName) || values == null)
                return false;

            var session = context.Session;
            if (session == null)
                return false;

            session[sessionName] = values;
            session.Timeout = expiresMinutes;
            return true;
        }

        /// <summary>
        /// 获取 Session 值
        /// </summary>
        /// <param name="sessionName">Session 键名</param>
        /// <returns>Session 值的字符串表示，不存在时返回 null</returns>
        public static string Get(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
                return null;

            var session = HttpContext.Current.Session;
            if (session == null)
                return null;

            var value = session[sessionName];
            return value == null ? null : value.ToString();
        }

        /// <summary>
        /// 获取 Session 值（支持 HttpContextBase）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="sessionName">Session 键名</param>
        /// <returns>Session 值的字符串表示，不存在时返回 null</returns>
        public static string GetAsync(HttpContextBase context, string sessionName)
        {
            if (context == null || string.IsNullOrEmpty(sessionName))
                return null;

            var session = context.Session;
            if (session == null)
                return null;

            var value = session[sessionName];
            return value == null ? null : value.ToString();
        }

        /// <summary>
        /// 获取 Session 数组值
        /// </summary>
        /// <param name="sessionName">Session 键名</param>
        /// <returns>Session 数组值，不存在或类型不匹配时返回 null</returns>
        public static string[] Gets(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
                return null;

            var session = HttpContext.Current.Session;
            if (session == null)
                return null;

            var value = session[sessionName];
            return value as string[];
        }

        /// <summary>
        /// 获取 Session 数组值（支持 HttpContext 参数）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="sessionName">Session 键名</param>
        /// <returns>Session 数组值，不存在或类型不匹配时返回 null</returns>
        public static string[] GetsAsync(HttpContext context, string sessionName)
        {
            if (context == null || string.IsNullOrEmpty(sessionName))
                return null;

            var session = context.Session;
            if (session == null)
                return null;

            var value = session[sessionName];
            return value as string[];
        }

        /// <summary>
        /// 删除指定 Session
        /// </summary>
        /// <param name="sessionName">Session 键名</param>
        public static void Del(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
                return;

            var session = HttpContext.Current.Session;
            if (session != null)
                session[sessionName] = null;
        }

        /// <summary>
        /// 删除指定 Session（支持 HttpContextBase）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="sessionName">Session 键名</param>
        public static void DelAsync(HttpContextBase context, string sessionName)
        {
            if (context == null || string.IsNullOrEmpty(sessionName))
                return;

            var session = context.Session;
            if (session != null)
                session[sessionName] = null;
        }
    }
}
