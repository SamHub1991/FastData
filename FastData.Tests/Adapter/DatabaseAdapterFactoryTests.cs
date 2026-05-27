using System;
using FastData.Tooling.Sync;
using Xunit;

namespace FastData.Tests.Adapter
{
    public class DatabaseAdapterFactoryTests
    {
        [Fact]
        public void DbProviderFactories_GetFactory_ThrowsForInvalidProvider()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                System.Data.Common.DbProviderFactories.GetFactory("NonExistent.Provider");
            });
        }

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

        [Fact]
        public void SyncDataType_Enum_HasStaticAndDynamic()
        {
            Assert.True(SyncDataType.Static == (SyncDataType)0);
            Assert.True(SyncDataType.Dynamic == (SyncDataType)1);
        }
    }
}
