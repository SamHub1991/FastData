using System;
using System.Text;
using System.Web;

namespace FastUntility.Cache
{
    /// <summary>
    /// Cookie 操作类
    /// 提供 Cookie 的读写删功能
    /// </summary>
    public static class BaseCookie
    {
        /// <summary>
        /// 写入 Cookie
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="name">Cookie 名称</param>
        /// <param name="value">Cookie 值</param>
        /// <param name="expireDays">过期天数，默认 1 天</param>
        /// <returns>写入是否成功</returns>
        public static bool WriteAsync(HttpContextBase context, string name, string value, int expireDays = 1)
        {
            if (context == null || string.IsNullOrEmpty(name))
                return false;

            var cookie = new HttpCookie(name)
            {
                HttpOnly = true,
                Value = HttpUtility.UrlEncode(value, Encoding.UTF8),
                Expires = DateTime.Now.AddDays(expireDays)
            };

            context.Response.Cookies.Add(cookie);
            return true;
        }

        /// <summary>
        /// 写入 Cookie
        /// </summary>
        /// <param name="name">Cookie 名称</param>
        /// <param name="value">Cookie 值</param>
        /// <param name="expireDays">过期天数，默认 1 天</param>
        /// <returns>写入是否成功</returns>
        public static bool Write(string name, string value, int expireDays = 1)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            var current = HttpContext.Current;
            if (current == null || current.Response == null)
                return false;

            var cookie = new HttpCookie(name)
            {
                HttpOnly = true,
                Value = HttpUtility.UrlEncode(value, Encoding.UTF8),
                Expires = DateTime.Now.AddDays(expireDays)
            };

            current.Response.Cookies.Add(cookie);
            return true;
        }

        /// <summary>
        /// 读取 Cookie 值
        /// </summary>
        /// <param name="name">Cookie 名称</param>
        /// <returns>Cookie 值，不存在时返回空字符串</returns>
        public static string Read(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var current = HttpContext.Current;
            if (current == null || current.Request == null || current.Request.Cookies == null)
                return string.Empty;

            var cookie = current.Request.Cookies[name];
            if (cookie == null)
                return string.Empty;

            return HttpUtility.UrlDecode(cookie.Value, Encoding.UTF8);
        }

        /// <summary>
        /// 读取 Cookie 值（支持 HttpContextBase）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="name">Cookie 名称</param>
        /// <returns>Cookie 值，不存在时返回空字符串</returns>
        public static string ReadAsync(HttpContextBase context, string name)
        {
            if (context == null || string.IsNullOrEmpty(name))
                return string.Empty;

            var cookies = context.Request.Cookies;
            if (cookies == null)
                return string.Empty;

            var cookie = cookies[name];
            if (cookie == null)
                return string.Empty;

            return HttpUtility.UrlDecode(cookie.Value, Encoding.UTF8);
        }

        /// <summary>
        /// 删除 Cookie
        /// 通过设置过期时间为过去时间来使浏览器删除 Cookie
        /// </summary>
        /// <param name="name">Cookie 名称</param>
        public static void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            var current = HttpContext.Current;
            if (current == null || current.Request == null || current.Response == null)
                return;

            var cookies = current.Request.Cookies;
            if (cookies == null || cookies[name] == null)
                return;

            var cookie = new HttpCookie(name)
            {
                Expires = DateTime.Now.AddMinutes(-1)
            };

            current.Response.Cookies.Add(cookie);
        }

        /// <summary>
        /// 删除 Cookie（支持 HttpContextBase）
        /// 通过设置过期时间为过去时间来使浏览器删除 Cookie
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="name">Cookie 名称</param>
        public static void RemoveAsync(HttpContextBase context, string name)
        {
            if (context == null || string.IsNullOrEmpty(name))
                return;

            var cookies = context.Request.Cookies;
            if (cookies == null || cookies[name] == null)
                return;

            var cookie = new HttpCookie(name)
            {
                Expires = DateTime.Now.AddMinutes(-1)
            };

            context.Response.Cookies.Add(cookie);
        }
    }
}
