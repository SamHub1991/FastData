using FastData.Abstractions;
using System;

namespace FastData.Tests.Abstractions
{
    /// <summary>
    /// DateTimeProvider 单元测试
    /// </summary>
    public class DateTimeProviderTests
    {
        public void DefaultDateTimeProvider_Now_ReturnsCurrentTime()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.Now;
            
            var now = DateTime.Now;
            Assert.IsTrue(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        public void DefaultDateTimeProvider_UtcNow_ReturnsCurrentUtcTime()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.UtcNow;
            
            var utcNow = DateTime.UtcNow;
            Assert.IsTrue(result >= utcNow.AddSeconds(-1) && result <= utcNow.AddSeconds(1));
        }

        public void DefaultDateTimeProvider_Today_ReturnsCurrentDate()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.Today;
            
            var today = DateTime.Today;
            Assert.AreEqual(today.Date, result.Date);
        }

        public void TestableDateTimeProvider_InitiallyNotFixed_ReturnsCurrentTime()
        {
            var provider = new TestableDateTimeProvider();
            var result = provider.Now;
            
            var now = DateTime.Now;
            Assert.IsTrue(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        public void TestableDateTimeProvider_SetNow_ReturnsFixedTime()
        {
            var fixedTime = new DateTime(2026, 5, 26, 12, 0, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            
            var result = provider.Now;
            Assert.AreEqual(fixedTime, result);
        }

        public void TestableDateTimeProvider_SetUtcNow_ReturnsFixedUtcTime()
        {
            var fixedTime = new DateTime(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
            var provider = new TestableDateTimeProvider();
            provider.SetUtcNow(fixedTime);
            
            var result = provider.UtcNow;
            Assert.AreEqual(fixedTime, result);
        }

        public void TestableDateTimeProvider_SetToday_ReturnsFixedDate()
        {
            var fixedDate = new DateTime(2026, 12, 25);
            var provider = new TestableDateTimeProvider();
            provider.SetToday(fixedDate);
            
            var result = provider.Today;
            Assert.AreEqual(fixedDate.Date, result.Date);
        }

        public void TestableDateTimeProvider_SetMultipleTimes_AllPropertiesUpdated()
        {
            var fixedTime = new DateTime(2026, 6, 15, 10, 30, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            provider.SetUtcNow(fixedTime.ToUniversalTime());
            provider.SetToday(fixedTime.Date);
            
            Assert.AreEqual(fixedTime, provider.Now);
            Assert.AreEqual(fixedTime.ToUniversalTime(), provider.UtcNow);
            Assert.AreEqual(fixedTime.Date, provider.Today);
        }

        public void TestableDateTimeProvider_Reset_RestoresCurrentTime()
        {
            var fixedTime = new DateTime(2026, 1, 1, 0, 0, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            
            Assert.AreEqual(fixedTime, provider.Now);
            
            provider.Reset();
            
            var afterReset = provider.Now;
            var now = DateTime.Now;
            Assert.IsTrue(afterReset >= now.AddSeconds(-1) && afterReset <= now.AddSeconds(1));
        }

        public void DateTimeProvider_Global_InitiallyUsesDefault()
        {
            var result = DateTimeProvider.Now;
            var now = DateTime.Now;
            
            Assert.IsTrue(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        public void DateTimeProvider_SetProvider_UsesCustomProvider()
        {
            var fixedTime = new DateTime(2026, 7, 4, 15, 45, 0);
            var testProvider = new TestableDateTimeProvider();
            testProvider.SetNow(fixedTime);
            
            DateTimeProvider.Current = testProvider;
            
            try
            {
                var result = DateTimeProvider.Now;
                Assert.AreEqual(fixedTime, result);
            }
            finally
            {
                DateTimeProvider.ResetToDefault();
            }
        }

        public void DateTimeProvider_SetToFixedTime_AllGlobalMethodsUseFixedTime()
        {
            var fixedTime = new DateTime(2026, 8, 20, 8, 0, 0);
            var testProvider = new TestableDateTimeProvider();
            testProvider.SetNow(fixedTime);
            testProvider.SetUtcNow(fixedTime.ToUniversalTime());
            testProvider.SetToday(fixedTime.Date);
            
            DateTimeProvider.Current = testProvider;
            
            try
            {
                Assert.AreEqual(fixedTime, DateTimeProvider.Now);
                Assert.AreEqual(fixedTime.ToUniversalTime(), DateTimeProvider.UtcNow);
                Assert.AreEqual(fixedTime.Date, DateTimeProvider.Today);
            }
            finally
            {
                DateTimeProvider.ResetToDefault();
            }
        }

        public void DateTimeProvider_ResetToDefault_RestoresCurrentTime()
        {
            var fixedTime = new DateTime(2026, 9, 10, 9, 0, 0);
            var testProvider = new TestableDateTimeProvider();
            testProvider.SetNow(fixedTime);
            DateTimeProvider.Current = testProvider;
            
            Assert.AreEqual(fixedTime, DateTimeProvider.Now);
            
            DateTimeProvider.ResetToDefault();
            
            var afterReset = DateTimeProvider.Now;
            var now = DateTime.Now;
            Assert.IsTrue(afterReset >= now.AddSeconds(-1) && afterReset <= now.AddSeconds(1));
        }

        public void DateTimeProvider_SetCurrentToNull_ThrowsArgumentNullException()
        {
            try
            {
                DateTimeProvider.Current = null;
                Assert.Fail("Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }
        }
    }
}
