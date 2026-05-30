using System;
using FastData.Tooling.Sync;
using Xunit;

namespace FastData.Tests.Adapter
{
    /// <summary>
    /// 数据库适配器工厂测试
    /// 
    /// 测试数据库提供程序工厂和同步数据类型的功能。
    /// </summary>
    public class DatabaseAdapterFactoryTests
    {
        /// <summary>
        /// 测试无效提供程序抛出异常
        /// </summary>
        [Fact]
        public void DbProviderFactories_GetFactory_ThrowsForInvalidProvider()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                System.Data.Common.DbProviderFactories.GetFactory("NonExistent.Provider");
            });
        }

        /// <summary>
        /// 测试 DataSyncResult 默认值为零
        /// </summary>
        [Fact]
        public void DataSyncResult_DefaultValues_AreZero()
        {
            var result = new DataSyncResult();
            Assert.Equal(0, result.ReadCount);
            Assert.Equal(0, result.WriteCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.RetryCount);
            Assert.Equal(0, result.RecoveredCount);
            Assert.Null(result.MaxPkValue);
            Assert.Null(result.LastSyncTime);
        }

        /// <summary>
        /// 测试 SyncDataType 枚举值
        /// </summary>
        [Fact]
        public void SyncDataType_Enum_HasStaticAndDynamic()
        {
            Assert.True(SyncDataType.Static == (SyncDataType)0);
            Assert.True(SyncDataType.Dynamic == (SyncDataType)1);
        }
    }
}
