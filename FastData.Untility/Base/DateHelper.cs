using System;

namespace FastUntility.Base
{
    /// <summary>
    /// 日期工具类
    /// </summary>
    public static class DateHelper
    {
        #region 时间戳
        /// <summary>
        /// 获取当前时间戳（秒）
        /// </summary>
        public static long GetTimestamp()
        {
            return FrameworkCompat.ToUnixTimeSeconds(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// 获取当前时间戳（毫秒）
        /// </summary>
        public static long GetTimestampMs()
        {
            return FrameworkCompat.ToUnixTimeMilliseconds(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// 时间转时间戳（秒）
        /// </summary>
        public static long ToTimestamp(DateTime dateTime)
        {
            var dto = dateTime.Kind == DateTimeKind.Utc
                ? new DateTimeOffset(dateTime, TimeSpan.Zero)
                : new DateTimeOffset(dateTime);
            return FrameworkCompat.ToUnixTimeSeconds(dto);
        }

        /// <summary>
        /// 时间戳转时间（秒）
        /// </summary>
        public static DateTime FromTimestamp(long timestamp)
        {
            return FrameworkCompat.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        /// <summary>
        /// 时间戳转时间（毫秒）
        /// </summary>
        public static DateTime FromTimestampMs(long timestampMs)
        {
            return FrameworkCompat.FromUnixTimeMilliseconds(timestampMs).LocalDateTime;
        }
        #endregion

        #region 相对时间
        /// <summary>
        /// 获取相对时间描述（如：3分钟前、2小时前）
        /// </summary>
        public static string GetRelativeTime(DateTime dateTime)
        {
            var ts = DateTime.Now - dateTime;

            if (ts.TotalSeconds < 0)
                return "刚刚";
            if (ts.TotalSeconds < 60)
                return $"{(int)ts.TotalSeconds}秒前";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes}分钟前";
            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours}小时前";
            if (ts.TotalDays < 30)
                return $"{(int)ts.TotalDays}天前";
            if (ts.TotalDays < 365)
                return $"{(int)(ts.TotalDays / 30)}个月前";

            return $"{(int)(ts.TotalDays / 365)}年前";
        }

        /// <summary>
        /// 获取相对时间描述（未来时间）
        /// </summary>
        public static string GetRelativeTimeFuture(DateTime dateTime)
        {
            var ts = dateTime - DateTime.Now;

            if (ts.TotalSeconds < 0)
                return "已过期";
            if (ts.TotalSeconds < 60)
                return $"{(int)ts.TotalSeconds}秒后";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes}分钟后";
            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours}小时后";
            if (ts.TotalDays < 30)
                return $"{(int)ts.TotalDays}天后";
            if (ts.TotalDays < 365)
                return $"{(int)(ts.TotalDays / 30)}个月后";

            return $"{(int)(ts.TotalDays / 365)}年后";
        }
        #endregion

        #region 日期计算
        /// <summary>
        /// 获取当天开始时间（00:00:00）
        /// </summary>
        public static DateTime GetDayStart(DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// 获取当天结束时间（23:59:59.999）
        /// </summary>
        public static DateTime GetDayEnd(DateTime dateTime)
        {
            return dateTime.Date.AddDays(1).AddMilliseconds(-1);
        }

        /// <summary>
        /// 获取本周开始时间（周一）
        /// </summary>
        public static DateTime GetWeekStart(DateTime dateTime)
        {
            var diff = (int)dateTime.DayOfWeek - 1;
            if (diff < 0) diff = 6;
            return dateTime.Date.AddDays(-diff);
        }

        /// <summary>
        /// 获取本周结束时间（周日 23:59:59）
        /// </summary>
        public static DateTime GetWeekEnd(DateTime dateTime)
        {
            return GetWeekStart(dateTime).AddDays(7).AddMilliseconds(-1);
        }

        /// <summary>
        /// 获取本月开始时间
        /// </summary>
        public static DateTime GetMonthStart(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// 获取本月结束时间
        /// </summary>
        public static DateTime GetMonthEnd(DateTime dateTime)
        {
            return GetMonthStart(dateTime).AddMonths(1).AddMilliseconds(-1);
        }

        /// <summary>
        /// 获取本季度开始时间
        /// </summary>
        public static DateTime GetQuarterStart(DateTime dateTime)
        {
            var quarter = (dateTime.Month - 1) / 3;
            return new DateTime(dateTime.Year, quarter * 3 + 1, 1);
        }

        /// <summary>
        /// 获取本季度结束时间
        /// </summary>
        public static DateTime GetQuarterEnd(DateTime dateTime)
        {
            return GetQuarterStart(dateTime).AddMonths(3).AddMilliseconds(-1);
        }

        /// <summary>
        /// 获取本年开始时间
        /// </summary>
        public static DateTime GetYearStart(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 1, 1);
        }

        /// <summary>
        /// 获取本年结束时间
        /// </summary>
        public static DateTime GetYearEnd(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 12, 31, 23, 59, 59, 999);
        }

        /// <summary>
        /// 计算两个日期之间的工作日天数
        /// </summary>
        public static int GetWorkDays(DateTime start, DateTime end)
        {
            var days = 0;
            var current = start.Date;
            while (current <= end.Date)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                    days++;
                current = current.AddDays(1);
            }
            return days;
        }

        /// <summary>
        /// 计算年龄
        /// </summary>
        public static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
                age--;
            return age;
        }
        #endregion

        #region 格式化
        /// <summary>
        /// 格式化为中文日期
        /// </summary>
        public static string ToChineseDate(DateTime dateTime)
        {
            return dateTime.ToString("yyyy年MM月dd日");
        }

        /// <summary>
        /// 格式化为中文日期时间
        /// </summary>
        public static string ToChineseDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy年MM月dd日 HH:mm:ss");
        }

        /// <summary>
        /// 格式化为友好的日期时间
        /// </summary>
        public static string ToFriendlyDateTime(DateTime dateTime)
        {
            var now = DateTime.Now;
            var ts = now - dateTime;

            if (ts.TotalDays == 0)
                return $"今天 {dateTime:HH:mm}";
            if (ts.TotalDays == 1)
                return $"昨天 {dateTime:HH:mm}";
            if (ts.TotalDays == 2)
                return $"前天 {dateTime:HH:mm}";
            if (dateTime.Year == now.Year)
                return dateTime.ToString("MM月dd日 HH:mm");

            return dateTime.ToString("yyyy年MM月dd日 HH:mm");
        }
        #endregion

        #region 判断
        /// <summary>
        /// 是否为今天
        /// </summary>
        public static bool IsToday(DateTime dateTime)
        {
            return dateTime.Date == DateTime.Today;
        }

        /// <summary>
        /// 是否为本周
        /// </summary>
        public static bool IsThisWeek(DateTime dateTime)
        {
            var weekStart = GetWeekStart(DateTime.Now);
            var weekEnd = GetWeekEnd(DateTime.Now);
            return dateTime >= weekStart && dateTime <= weekEnd;
        }

        /// <summary>
        /// 是否为本月
        /// </summary>
        public static bool IsThisMonth(DateTime dateTime)
        {
            var now = DateTime.Now;
            return dateTime.Year == now.Year && dateTime.Month == now.Month;
        }

        /// <summary>
        /// 是否为闰年
        /// </summary>
        public static bool IsLeapYear(int year)
        {
            return DateTime.IsLeapYear(year);
        }

        /// <summary>
        /// 是否为工作日
        /// </summary>
        public static bool IsWorkDay(DateTime dateTime)
        {
            return dateTime.DayOfWeek != DayOfWeek.Saturday && dateTime.DayOfWeek != DayOfWeek.Sunday;
        }
        #endregion
    }
}
