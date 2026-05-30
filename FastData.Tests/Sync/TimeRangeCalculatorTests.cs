using FastData.Tooling.Sync;
using System;
using Xunit;

namespace FastData.Tests.Sync
{
    /// <summary>
    /// 时间范围计算器测试
    /// 
    /// 测试同步时间范围的计算逻辑。
    /// </summary>
    public class TimeRangeCalculatorTests
    {
        /// <summary>
        /// 测试首次同步返回最小时间
        /// </summary>
        [Fact]
        public void GetSyncStartTime_FirstSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(DateTime.Now, 3, true);
            Assert.Equal(DateTime.MinValue, result);
        }

        /// <summary>
        /// 测试后续同步正确计算开始时间
        /// </summary>
        [Fact]
        public void GetSyncStartTime_SubsequentSync_CalculatesCorrectly()
        {
            var lastSync = DateTime.Now;
            var result = TimeRangeCalculator.GetSyncStartTime(lastSync, 3, false);
            Assert.Equal(lastSync.AddDays(-3), result);
        }

        /// <summary>
        /// 测试空上次同步时间返回最小时间
        /// </summary>
        [Fact]
        public void GetSyncStartTime_NullLastSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(null, 3, false);
            Assert.Equal(DateTime.MinValue, result);
        }

        /// <summary>
        /// 测试获取同步结束时间
        /// </summary>
        [Fact]
        public void GetSyncEndTime_ReturnsCurrentTime()
        {
            var result = TimeRangeCalculator.GetSyncEndTime();
            var now = DateTime.Now;
            Assert.True(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        /// <summary>
        /// 测试预设范围 - 最近 7 天
        /// </summary>
        [Fact]
        public void GetPresetRange_Last7Days_ReturnsCorrectRange()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.Last7Days);
            var now = DateTime.Now;
            Assert.True(result.Item2 >= now.AddSeconds(-1) && result.Item2 <= now.AddSeconds(1));
            Assert.Equal(7, (now.Date - result.Item1.Date).Days);
        }

        /// <summary>
        /// 测试预设范围 - 本月
        /// </summary>
        [Fact]
        public void GetPresetRange_ThisMonth_ReturnsFirstDayOfMonth()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.ThisMonth);
            Assert.Equal(1, result.Item1.Day);
            Assert.Equal(DateTime.Now.Month, result.Item1.Month);
        }
    }
}
