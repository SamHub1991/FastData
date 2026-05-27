using FastData.Tooling.Sync;
using System;
using Xunit;

namespace FastData.Tests.Sync
{
    public class TimeRangeCalculatorTests
    {
        [Fact]
        public void GetSyncStartTime_FirstSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(DateTime.Now, 3, true);
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public void GetSyncStartTime_SubsequentSync_CalculatesCorrectly()
        {
            var lastSync = DateTime.Now;
            var result = TimeRangeCalculator.GetSyncStartTime(lastSync, 3, false);
            Assert.Equal(lastSync.AddDays(-3), result);
        }

        [Fact]
        public void GetSyncStartTime_NullLastSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(null, 3, false);
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public void GetSyncEndTime_ReturnsCurrentTime()
        {
            var result = TimeRangeCalculator.GetSyncEndTime();
            var now = DateTime.Now;
            Assert.True(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        [Fact]
        public void GetPresetRange_Last7Days_ReturnsCorrectRange()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.Last7Days);
            var now = DateTime.Now;
            Assert.True(result.Item2 >= now.AddSeconds(-1) && result.Item2 <= now.AddSeconds(1));
            Assert.Equal(7, (now.Date - result.Item1.Date).Days);
        }

        [Fact]
        public void GetPresetRange_ThisMonth_ReturnsFirstDayOfMonth()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.ThisMonth);
            Assert.Equal(1, result.Item1.Day);
            Assert.Equal(DateTime.Now.Month, result.Item1.Month);
        }
    }
}
