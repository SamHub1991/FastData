using FastData.Tooling.Sync;
using System;

namespace FastData.Tests.Sync
{
    public class TimeRangeCalculatorTests
    {
        public void GetSyncStartTime_FirstSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(DateTime.Now, 3, true);
            Assert.AreEqual(DateTime.MinValue, result);
        }

        public void GetSyncStartTime_SubsequentSync_CalculatesCorrectly()
        {
            var lastSync = DateTime.Now;
            var result = TimeRangeCalculator.GetSyncStartTime(lastSync, 3, false);
            Assert.AreEqual(lastSync.AddDays(-3), result);
        }

        public void GetSyncStartTime_NullLastSync_ReturnsMinValue()
        {
            var result = TimeRangeCalculator.GetSyncStartTime(null, 3, false);
            Assert.AreEqual(DateTime.MinValue, result);
        }

        public void GetSyncEndTime_ReturnsCurrentTime()
        {
            var result = TimeRangeCalculator.GetSyncEndTime();
            var now = DateTime.Now;
            Assert.IsTrue(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        public void GetPresetRange_Last7Days_ReturnsCorrectRange()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.Last7Days);
            var now = DateTime.Now;
            Assert.IsTrue(result.Item2 >= now.AddSeconds(-1) && result.Item2 <= now.AddSeconds(1));
            Assert.AreEqual(7, (now.Date - result.Item1.Date).Days);
        }

        public void GetPresetRange_ThisMonth_ReturnsFirstDayOfMonth()
        {
            var result = TimeRangeCalculator.GetPresetRange(PresetRangeType.ThisMonth);
            Assert.AreEqual(1, result.Item1.Day);
            Assert.AreEqual(DateTime.Now.Month, result.Item1.Month);
        }
    }
}
