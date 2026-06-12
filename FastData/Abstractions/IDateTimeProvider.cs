using System;

namespace FastData.Abstractions
{
    /// <summary>
    /// 日期时间抽象接口
    /// 用于支持单元测试中的时间模拟
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// 获取当前 UTC 时间
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// 获取当前本地时间
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// 获取当前日期（不含时间部分）
        /// </summary>
        DateTime Today { get; }
    }

    /// <summary>
    /// 默认日期时间提供程序
    /// 实际生产环境使用
    /// </summary>
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;
        /// <inheritdoc />
        public DateTime Today => DateTime.Today;
    }

    /// <summary>
    /// 可测试的日期时间提供程序
    /// 允许在单元测试中设置固定时间
    /// </summary>
    public class TestableDateTimeProvider : IDateTimeProvider
    {
        private DateTime? _fixedUtcNow;
        private DateTime? _fixedNow;
        private DateTime? _fixedToday;

        /// <inheritdoc />
        public DateTime UtcNow => _fixedUtcNow ?? DateTime.UtcNow;
        /// <inheritdoc />
        public DateTime Now => _fixedNow ?? DateTime.Now;
        /// <inheritdoc />
        public DateTime Today => _fixedToday ?? DateTime.Today;

        /// <summary>
        /// 设置固定的 UTC 时间
        /// </summary>
        /// <param name="dateTime">UTC 时间</param>
        public void SetUtcNow(DateTime dateTime)
        {
            _fixedUtcNow = dateTime;
        }

        /// <summary>
        /// 设置固定的本地时间
        /// </summary>
        /// <param name="dateTime">本地时间</param>
        public void SetNow(DateTime dateTime)
        {
            _fixedNow = dateTime;
        }

        /// <summary>
        /// 设置固定的日期
        /// </summary>
        /// <param name="dateTime">日期</param>
        public void SetToday(DateTime dateTime)
        {
            _fixedToday = dateTime.Date;
        }

        /// <summary>
        /// 重置所有固定时间
        /// </summary>
        public void Reset()
        {
            _fixedUtcNow = null;
            _fixedNow = null;
            _fixedToday = null;
        }
    }

    /// <summary>
    /// 全局日期时间提供程序访问点
    /// </summary>
    public static class DateTimeProvider
    {
        private static IDateTimeProvider _current = new DefaultDateTimeProvider();

        /// <summary>
        /// 获取或设置当前日期时间提供程序
        /// </summary>
        public static IDateTimeProvider Current
        {
            get => _current;
            set => _current = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 获取当前 UTC 时间
        /// </summary>
        public static DateTime UtcNow => _current.UtcNow;

        /// <summary>
        /// 获取当前本地时间
        /// </summary>
        public static DateTime Now => _current.Now;

        /// <summary>
        /// 获取当前日期
        /// </summary>
        public static DateTime Today => _current.Today;

        /// <summary>
        /// 重置为默认提供程序
        /// </summary>
        public static void ResetToDefault()
        {
            _current = new DefaultDateTimeProvider();
        }
    }
}
