using System;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 同步时间范围计算器
    /// </summary>
    public static class TimeRangeCalculator
    {
        /// <summary>
        /// 获取同步开始时间
        /// </summary>
        /// <param name="lastSyncTime">上次同步时间</param>
        /// <param name="rangeDays">同步范围天数</param>
        /// <param name="isFirstSync">是否首次同步</param>
        /// <returns>同步开始时间，首次同步返回 DateTime.MinValue</returns>
        public static DateTime GetSyncStartTime(DateTime? lastSyncTime, int rangeDays, bool isFirstSync)
        {
            if (isFirstSync || !lastSyncTime.HasValue)
                return DateTime.MinValue;

            // 后续同步：从 (上次同步时间 - rangeDays) 开始
            return lastSyncTime.Value.AddDays(-rangeDays);
        }

        /// <summary>
        /// 获取同步结束时间
        /// </summary>
        /// <returns>当前时间</returns>
        public static DateTime GetSyncEndTime()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// 计算时间范围描述
        /// </summary>
        /// <param name="lastSyncTime">上次同步时间</param>
        /// <param name="rangeDays">同步范围天数</param>
        /// <param name="isFirstSync">是否首次同步</param>
        /// <returns>时间范围描述</returns>
        public static string GetTimeRangeDescription(DateTime? lastSyncTime, int rangeDays, bool isFirstSync)
        {
            if (isFirstSync || !lastSyncTime.HasValue)
                return "全量同步（所有历史数据）";

            var startTime = GetSyncStartTime(lastSyncTime, rangeDays, false);
            var endTime = GetSyncEndTime();
            return string.Format("增量同步（{0:yyyy-MM-dd HH:mm:ss} 至 {1:yyyy-MM-dd HH:mm:ss}）",
                startTime, endTime);
        }

        /// <summary>
        /// 快速获取预设时间范围
        /// </summary>
        /// <param name="preset">预设类型</param>
        /// <returns>(起始时间，结束时间)</returns>
        public static Tuple<DateTime, DateTime> GetPresetRange(PresetRangeType preset)
        {
            var endTime = DateTime.Now;
            DateTime startTime;

            switch (preset)
            {
                case PresetRangeType.Last1Day:
                    startTime = endTime.AddDays(-1);
                    break;
                case PresetRangeType.Last3Days:
                    startTime = endTime.AddDays(-3);
                    break;
                case PresetRangeType.Last7Days:
                    startTime = endTime.AddDays(-7);
                    break;
                case PresetRangeType.Last30Days:
                    startTime = endTime.AddDays(-30);
                    break;
                case PresetRangeType.ThisMonth:
                    startTime = new DateTime(endTime.Year, endTime.Month, 1);
                    break;
                case PresetRangeType.LastMonth:
                    startTime = new DateTime(endTime.Year, endTime.Month, 1).AddMonths(-1);
                    endTime = startTime.AddMonths(1).AddMilliseconds(-1);
                    break;
                default:
                    startTime = endTime.AddDays(-3);
                    break;
            }

            return Tuple.Create(startTime, endTime);
        }
    }

    /// <summary>
    /// 预设时间范围类型
    /// </summary>
    public enum PresetRangeType
    {
        Last1Day,
        Last3Days,
        Last7Days,
        Last30Days,
        ThisMonth,
        LastMonth
    }
}
