using FastData.Abstractions;
using System;
using Xunit;

namespace FastData.Tests.Abstractions
{
    /// <summary>
    /// DateTimeProvider 单元测试
    /// 
    /// 覆盖默认时间提供器和可测试时间提供器的所有核心功能，
    /// 包括时间获取、固定时间设置、全局时间提供器切换、重置操作、
    /// 异常场景以及多属性同步更新等。
    /// </summary>
    public class DateTimeProviderTests
    {
        /// <summary>
        /// 验证 DefaultDateTimeProvider.Now 返回当前本地时间（误差 1 秒以内）
        /// </summary>
        [Fact]
        public void DefaultDateTimeProvider_Now_ReturnsCurrentTime()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.Now;
            
            var now = DateTime.Now;
            Assert.True(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        /// <summary>
        /// 验证 DefaultDateTimeProvider.UtcNow 返回当前 UTC 时间（误差 1 秒以内）
        /// </summary>
        [Fact]
        public void DefaultDateTimeProvider_UtcNow_ReturnsCurrentUtcTime()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.UtcNow;
            
            var utcNow = DateTime.UtcNow;
            Assert.True(result >= utcNow.AddSeconds(-1) && result <= utcNow.AddSeconds(1));
        }

        /// <summary>
        /// 验证 DefaultDateTimeProvider.Today 返回当前日期（不含时间部分）
        /// </summary>
        [Fact]
        public void DefaultDateTimeProvider_Today_ReturnsCurrentDate()
        {
            var provider = new DefaultDateTimeProvider();
            var result = provider.Today;
            
            var today = DateTime.Today;
            Assert.Equal(today.Date, result.Date);
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider 初始状态下（未设置固定时间）返回当前时间
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_InitiallyNotFixed_ReturnsCurrentTime()
        {
            var provider = new TestableDateTimeProvider();
            var result = provider.Now;
            
            var now = DateTime.Now;
            Assert.True(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider.SetNow 可正确固定 Now 为指定时间
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_SetNow_ReturnsFixedTime()
        {
            var fixedTime = new DateTime(2026, 5, 26, 12, 0, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            
            var result = provider.Now;
            Assert.Equal(fixedTime, result);
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider.SetUtcNow 可正确固定 UtcNow 为指定 UTC 时间
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_SetUtcNow_ReturnsFixedUtcTime()
        {
            var fixedTime = new DateTime(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
            var provider = new TestableDateTimeProvider();
            provider.SetUtcNow(fixedTime);
            
            var result = provider.UtcNow;
            Assert.Equal(fixedTime, result);
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider.SetToday 可正确固定 Today 为指定日期
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_SetToday_ReturnsFixedDate()
        {
            var fixedDate = new DateTime(2026, 12, 25);
            var provider = new TestableDateTimeProvider();
            provider.SetToday(fixedDate);
            
            var result = provider.Today;
            Assert.Equal(fixedDate.Date, result.Date);
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider 可同时设置 Now、UtcNow、Today 三个属性，并且值正确同步
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_SetMultipleTimes_AllPropertiesUpdated()
        {
            var fixedTime = new DateTime(2026, 6, 15, 10, 30, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            provider.SetUtcNow(fixedTime.ToUniversalTime());
            provider.SetToday(fixedTime.Date);
            
            Assert.Equal(fixedTime, provider.Now);
            Assert.Equal(fixedTime.ToUniversalTime(), provider.UtcNow);
            Assert.Equal(fixedTime.Date, provider.Today);
        }

        /// <summary>
        /// 验证 TestableDateTimeProvider.Reset 可将固定时间恢复为实时时间
        /// </summary>
        [Fact]
        public void TestableDateTimeProvider_Reset_RestoresCurrentTime()
        {
            var fixedTime = new DateTime(2026, 1, 1, 0, 0, 0);
            var provider = new TestableDateTimeProvider();
            provider.SetNow(fixedTime);
            
            Assert.Equal(fixedTime, provider.Now);
            
            provider.Reset();
            
            var afterReset = provider.Now;
            var now = DateTime.Now;
            Assert.True(afterReset >= now.AddSeconds(-1) && afterReset <= now.AddSeconds(1));
        }

        /// <summary>
        /// 验证 DateTimeProvider 静态属性初始使用默认时间提供器返回当前时间
        /// </summary>
        [Fact]
        public void DateTimeProvider_Global_InitiallyUsesDefault()
        {
            var result = DateTimeProvider.Now;
            var now = DateTime.Now;
            
            Assert.True(result >= now.AddSeconds(-1) && result <= now.AddSeconds(1));
        }

        /// <summary>
        /// 验证 DateTimeProvider.Current 设置为自定义提供器后，所有静态方法使用该提供器的时间
        /// </summary>
        [Fact]
        public void DateTimeProvider_SetProvider_UsesCustomProvider()
        {
            var fixedTime = new DateTime(2026, 7, 4, 15, 45, 0);
            var testProvider = new TestableDateTimeProvider();
            testProvider.SetNow(fixedTime);
            
            DateTimeProvider.Current = testProvider;
            
            try
            {
                var result = DateTimeProvider.Now;
                Assert.Equal(fixedTime, result);
            }
            finally
            {
                DateTimeProvider.ResetToDefault();
            }
        }

        /// <summary>
        /// 验证通过自定义提供器设置固定时间后，所有全局静态方法（Now、UtcNow、Today）都返回固定时间
        /// </summary>
        [Fact]
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
                Assert.Equal(fixedTime, DateTimeProvider.Now);
                Assert.Equal(fixedTime.ToUniversalTime(), DateTimeProvider.UtcNow);
                Assert.Equal(fixedTime.Date, DateTimeProvider.Today);
            }
            finally
            {
                DateTimeProvider.ResetToDefault();
            }
        }

        /// <summary>
        /// 验证 DateTimeProvider.ResetToDefault 可将全局提供器恢复为默认实现
        /// </summary>
        [Fact]
        public void DateTimeProvider_ResetToDefault_RestoresCurrentTime()
        {
            var fixedTime = new DateTime(2026, 9, 10, 9, 0, 0);
            var testProvider = new TestableDateTimeProvider();
            testProvider.SetNow(fixedTime);
            DateTimeProvider.Current = testProvider;
            
            Assert.Equal(fixedTime, DateTimeProvider.Now);
            
            DateTimeProvider.ResetToDefault();
            
            var afterReset = DateTimeProvider.Now;
            var now = DateTime.Now;
            Assert.True(afterReset >= now.AddSeconds(-1) && afterReset <= now.AddSeconds(1));
        }

        /// <summary>
        /// 验证将 DateTimeProvider.Current 设置为 null 时抛出 ArgumentNullException
        /// </summary>
        [Fact]
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
