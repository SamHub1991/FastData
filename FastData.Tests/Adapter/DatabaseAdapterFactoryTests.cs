using System;
using FastData.Tooling.Sync;

namespace FastData.Tests.Adapter
{
    public class DatabaseAdapterFactoryTests
    {
        public void DbProviderFactories_GetFactory_ThrowsForInvalidProvider()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                System.Data.Common.DbProviderFactories.GetFactory("NonExistent.Provider");
            });
        }

        public void DataSyncResult_DefaultValues_AreZero()
        {
            var result = new DataSyncResult();
            Assert.AreEqual(0, result.ReadCount);
            Assert.AreEqual(0, result.WriteCount);
            Assert.AreEqual(0, result.FailedCount);
            Assert.AreEqual(0, result.RetryCount);
            Assert.AreEqual(0, result.RecoveredCount);
            Assert.IsNull(result.MaxPkValue);
            Assert.IsNull(result.LastSyncTime);
        }

        public void SyncDataType_Enum_HasStaticAndDynamic()
        {
            Assert.IsTrue(SyncDataType.Static == (SyncDataType)0);
            Assert.IsTrue(SyncDataType.Dynamic == (SyncDataType)1);
        }
    }
}