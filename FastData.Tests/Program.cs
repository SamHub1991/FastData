using System;
using System.IO;
using FastData.Tooling.Sync;

namespace FastData.Tests
{
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
                throw new Exception(string.Format("Assertion failed: {0}", message ?? "Expected true but was false"));
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
                throw new Exception(string.Format("Assertion failed: {0}", message ?? "Expected false but was true"));
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
                throw new Exception(string.Format("Assertion failed: {0}", message ?? "Expected not null but was null"));
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
                throw new Exception(string.Format("Assertion failed: {0}", message ?? "Expected null but was not null"));
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual))
                throw new Exception(string.Format("Assertion failed: {0}", message ?? string.Format("Expected {0} but was {1}", expected, actual)));
        }

        public static void Throws<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
                throw new Exception(string.Format("Assertion failed: {0}", message ?? string.Format("Expected exception {0} but no exception was thrown", typeof(T).Name)));
            }
            catch (T)
            {
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Assertion failed: {0}", message ?? string.Format("Expected exception {0} but got {1}", typeof(T).Name, ex.GetType().Name)));
            }
        }

        public static void Fail(string message = null)
        {
            throw new Exception(string.Format("Assertion failed: {0}", message ?? "Test failed"));
        }
    }

    public class TestRunner
    {
        private int _passed = 0;
        private int _failed = 0;

        public void RunTests()
        {
            Console.WriteLine("=== FastData Test Runner ===");
            Console.WriteLine();

            RunTimeRangeCalculatorTests();
            RunDatabaseAdapterFactoryTests();
            RunDataSyncOptionsTests();
            RunTableSyncConfigTests();
            RunPrimaryKeyConfigServiceTests();
            RunSyncConfigManagerTests();
            RunDataRowSerializerTests();
            RunDateTimeProviderTests();

            Console.WriteLine();
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine(string.Format("Passed: {0}", _passed));
            Console.WriteLine(string.Format("Failed: {0}", _failed));
            Console.WriteLine(string.Format("Total: {0}", _passed + _failed));

            if (_failed > 0)
                Environment.Exit(1);
        }

        private void RunTest(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine(string.Format("  [PASS] {0}", name));
                _passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("  [FAIL] {0}: {1}", name, ex.Message));
                _failed++;
            }
        }

        private void RunTimeRangeCalculatorTests()
        {
            Console.WriteLine("TimeRangeCalculatorTests:");
            var tests = new Sync.TimeRangeCalculatorTests();
            RunTest("GetSyncStartTime_FirstSync_ReturnsMinValue", tests.GetSyncStartTime_FirstSync_ReturnsMinValue);
            RunTest("GetSyncStartTime_SubsequentSync_CalculatesCorrectly", tests.GetSyncStartTime_SubsequentSync_CalculatesCorrectly);
            RunTest("GetSyncStartTime_NullLastSync_ReturnsMinValue", tests.GetSyncStartTime_NullLastSync_ReturnsMinValue);
            RunTest("GetSyncEndTime_ReturnsCurrentTime", tests.GetSyncEndTime_ReturnsCurrentTime);
            RunTest("GetPresetRange_Last7Days_ReturnsCorrectRange", tests.GetPresetRange_Last7Days_ReturnsCorrectRange);
            RunTest("GetPresetRange_ThisMonth_ReturnsFirstDayOfMonth", tests.GetPresetRange_ThisMonth_ReturnsFirstDayOfMonth);
            Console.WriteLine();
        }

        private void RunDatabaseAdapterFactoryTests()
        {
            Console.WriteLine("DatabaseAdapterFactoryTests:");
            var tests = new Adapter.DatabaseAdapterFactoryTests();
            RunTest("DbProviderFactories_GetFactory_ThrowsForInvalidProvider", tests.DbProviderFactories_GetFactory_ThrowsForInvalidProvider);
            RunTest("DataSyncResult_DefaultValues_AreZero", tests.DataSyncResult_DefaultValues_AreZero);
            RunTest("SyncDataType_Enum_HasStaticAndDynamic", tests.SyncDataType_Enum_HasStaticAndDynamic);
            Console.WriteLine();
        }

        private void RunDataSyncOptionsTests()
        {
            Console.WriteLine("DataSyncOptionsTests:");
            var tests = new Config.DataSyncOptionsTests();
            RunTest("DefaultValues_AreCorrect", tests.DefaultValues_AreCorrect);
            RunTest("EnableGlobalConfig_True_WithGlobalRangeDays_OverridesRangeDays", tests.EnableGlobalConfig_True_WithGlobalRangeDays_OverridesRangeDays);
            RunTest("EnableGlobalConfig_False_DoesNotOverrideRangeDays", tests.EnableGlobalConfig_False_DoesNotOverrideRangeDays);
            RunTest("EnableGlobalConfig_True_WithZeroGlobalRangeDays_DoesNotOverride", tests.EnableGlobalConfig_True_WithZeroGlobalRangeDays_DoesNotOverride);
            Console.WriteLine();
        }

        private void RunTableSyncConfigTests()
        {
            Console.WriteLine("TableSyncConfigTests:");
            var tests = new Config.TableSyncConfigTests();
            RunTest("DefaultValues_AreCorrect", tests.DefaultValues_AreCorrect);
            RunTest("TargetTableName_CanBeSetIndependently", tests.TargetTableName_CanBeSetIndependently);
            Console.WriteLine();
        }

        private void RunPrimaryKeyConfigServiceTests()
        {
            Console.WriteLine("PrimaryKeyConfigServiceTests:");
            var tests = new Sync.PrimaryKeyConfigServiceTests();
            RunTest("AddTableConfig_ValidConfig_AddsSuccessfully", tests.AddTableConfig_ValidConfig_AddsSuccessfully);
            RunTest("AddTableConfig_NullConfig_DoesNotAdd", tests.AddTableConfig_NullConfig_DoesNotAdd);
            RunTest("AddTableConfig_EmptyTableName_DoesNotAdd", tests.AddTableConfig_EmptyTableName_DoesNotAdd);
            RunTest("AddTableConfig_DuplicateTableName_Overwrites", tests.AddTableConfig_DuplicateTableName_Overwrites);
            RunTest("RemoveTableConfig_ExistingTable_RemovesSuccessfully", tests.RemoveTableConfig_ExistingTable_RemovesSuccessfully);
            RunTest("RemoveTableConfig_NonExistingTable_NoEffect", tests.RemoveTableConfig_NonExistingTable_NoEffect);
            RunTest("RemoveTableConfig_NullTableName_NoEffect", tests.RemoveTableConfig_NullTableName_NoEffect);
            RunTest("GetTableConfig_UnknownTable_ReturnsNull", tests.GetTableConfig_UnknownTable_ReturnsNull);
            RunTest("GetTableConfig_NullTableName_ReturnsNull", tests.GetTableConfig_NullTableName_ReturnsNull);
            RunTest("GetAllConfigs_ReturnsAllAddedConfigs", tests.GetAllConfigs_ReturnsAllAddedConfigs);
            RunTest("BuildPrimaryKeyWhereClause_SingleKey_ReturnsCorrectClause", tests.BuildPrimaryKeyWhereClause_SingleKey_ReturnsCorrectClause);
            RunTest("BuildPrimaryKeyWhereClause_CompositeKey_ReturnsAndClause", tests.BuildPrimaryKeyWhereClause_CompositeKey_ReturnsAndClause);
            RunTest("BuildPrimaryKeyWhereClause_CustomParamNames_ReturnsCustomClause", tests.BuildPrimaryKeyWhereClause_CustomParamNames_ReturnsCustomClause);
            RunTest("BuildPrimaryKeyWhereClause_NullConfig_ReturnsFallback", tests.BuildPrimaryKeyWhereClause_NullConfig_ReturnsFallback);
            RunTest("BuildPrimaryKeyWhereClause_EmptyColumns_ReturnsFallback", tests.BuildPrimaryKeyWhereClause_EmptyColumns_ReturnsFallback);
            RunTest("BuildIncrementalWhereClause_AutoIncrementSingleKey_ReturnsSimpleClause", tests.BuildIncrementalWhereClause_AutoIncrementSingleKey_ReturnsSimpleClause);
            RunTest("BuildIncrementalWhereClause_CompositeKey_ReturnsOrClause", tests.BuildIncrementalWhereClause_CompositeKey_ReturnsOrClause);
            RunTest("BuildIncrementalWhereClause_NullConfig_ReturnsFallback", tests.BuildIncrementalWhereClause_NullConfig_ReturnsFallback);
            RunTest("BuildIncrementalWhereClause_EmptyColumns_ReturnsFallback", tests.BuildIncrementalWhereClause_EmptyColumns_ReturnsFallback);
            RunTest("ExportToSql_GeneratesCreateTableAndInserts", tests.ExportToSql_GeneratesCreateTableAndInserts);
            Console.WriteLine();
        }

        private void RunSyncConfigManagerTests()
        {
            Console.WriteLine("SyncConfigManagerTests:");
            var tests = new Config.SyncConfigManagerTests();
            RunTest("SaveAndGetTaskConfig_WorksCorrectly", tests.SaveAndGetTaskConfig_WorksCorrectly);
            RunTest("GetTaskConfig_UnknownTask_ReturnsNull", tests.GetTaskConfig_UnknownTask_ReturnsNull);
            RunTest("GetTaskConfig_NullTaskId_ReturnsNull", tests.GetTaskConfig_NullTaskId_ReturnsNull);
            RunTest("DeleteTaskConfig_RemovesConfig", tests.DeleteTaskConfig_RemovesConfig);
            RunTest("DeleteTaskConfig_NullTaskId_NoEffect", tests.DeleteTaskConfig_NullTaskId_NoEffect);
            RunTest("GetAllTaskConfigs_ReturnsAllConfigs", tests.GetAllTaskConfigs_ReturnsAllConfigs);
            RunTest("UpdateLastSyncTime_UpdatesIsFirstSync", tests.UpdateLastSyncTime_UpdatesIsFirstSync);
            RunTest("UpdateTaskStatus_UpdatesStatusAndMessage", tests.UpdateTaskStatus_UpdatesStatusAndMessage);
            RunTest("PersistAndReload_ConfigsPersistAcrossInstances", tests.PersistAndReload_ConfigsPersistAcrossInstances);
            RunTest("NonExistentConfigFile_InitializesEmpty", tests.NonExistentConfigFile_InitializesEmpty);
            Console.WriteLine();
        }

        private void RunDataRowSerializerTests()
        {
            Console.WriteLine("DataRowSerializerTests:");
            var tests = new Sync.DataRowSerializerTests();
            RunTest("Serialize_EmptyRow_ReturnsJsonWithEmptyValues", tests.Serialize_EmptyRow_ReturnsJsonWithEmptyValues);
            RunTest("Serialize_RowWithData_ReturnsValidJson", tests.Serialize_RowWithData_ReturnsValidJson);
            RunTest("Serialize_RowWithNullValue_HandlesNullCorrectly", tests.Serialize_RowWithNullValue_HandlesNullCorrectly);
            RunTest("Serialize_RowWithDateTime_HandlesDateTimeCorrectly", tests.Serialize_RowWithDateTime_HandlesDateTimeCorrectly);
            RunTest("Deserialize_ValidJson_ReturnsDataTableWithSameSchema", tests.Deserialize_ValidJson_ReturnsDataTableWithSameSchema);
            RunTest("Deserialize_ValidJson_RestoresRowData", tests.Deserialize_ValidJson_RestoresRowData);
            RunTest("Deserialize_EmptyJson_ReturnsEmptyDataTable", tests.Deserialize_EmptyJson_ReturnsEmptyDataTable);
            RunTest("SerializeBatch_MultipleRows_ReturnsJsonArray", tests.SerializeBatch_MultipleRows_ReturnsJsonArray);
            RunTest("DeserializeBatch_ValidJsonArray_ReturnsDataTable", tests.DeserializeBatch_ValidJsonArray_ReturnsDataTable);
            RunTest("RoundTrip_SerializeThenDeserialize_PreservesData", tests.RoundTrip_SerializeThenDeserialize_PreservesData);
            Console.WriteLine();
        }

        private void RunDateTimeProviderTests()
        {
            Console.WriteLine("DateTimeProviderTests:");
            var tests = new Abstractions.DateTimeProviderTests();
            RunTest("DefaultDateTimeProvider_Now_ReturnsCurrentTime", tests.DefaultDateTimeProvider_Now_ReturnsCurrentTime);
            RunTest("DefaultDateTimeProvider_UtcNow_ReturnsCurrentUtcTime", tests.DefaultDateTimeProvider_UtcNow_ReturnsCurrentUtcTime);
            RunTest("DefaultDateTimeProvider_Today_ReturnsCurrentDate", tests.DefaultDateTimeProvider_Today_ReturnsCurrentDate);
            RunTest("TestableDateTimeProvider_InitiallyNotFixed_ReturnsCurrentTime", tests.TestableDateTimeProvider_InitiallyNotFixed_ReturnsCurrentTime);
            RunTest("TestableDateTimeProvider_SetNow_ReturnsFixedTime", tests.TestableDateTimeProvider_SetNow_ReturnsFixedTime);
            RunTest("TestableDateTimeProvider_SetUtcNow_ReturnsFixedUtcTime", tests.TestableDateTimeProvider_SetUtcNow_ReturnsFixedUtcTime);
            RunTest("TestableDateTimeProvider_SetToday_ReturnsFixedDate", tests.TestableDateTimeProvider_SetToday_ReturnsFixedDate);
            RunTest("TestableDateTimeProvider_SetMultipleTimes_AllPropertiesUpdated", tests.TestableDateTimeProvider_SetMultipleTimes_AllPropertiesUpdated);
            RunTest("TestableDateTimeProvider_Reset_RestoresCurrentTime", tests.TestableDateTimeProvider_Reset_RestoresCurrentTime);
            RunTest("DateTimeProvider_Global_InitiallyUsesDefault", tests.DateTimeProvider_Global_InitiallyUsesDefault);
            RunTest("DateTimeProvider_SetProvider_UsesCustomProvider", tests.DateTimeProvider_SetProvider_UsesCustomProvider);
            RunTest("DateTimeProvider_SetToFixedTime_AllGlobalMethodsUseFixedTime", tests.DateTimeProvider_SetToFixedTime_AllGlobalMethodsUseFixedTime);
            RunTest("DateTimeProvider_ResetToDefault_RestoresCurrentTime", tests.DateTimeProvider_ResetToDefault_RestoresCurrentTime);
            RunTest("DateTimeProvider_SetCurrentToNull_ThrowsArgumentNullException", tests.DateTimeProvider_SetCurrentToNull_ThrowsArgumentNullException);
            Console.WriteLine();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var runner = new TestRunner();
            runner.RunTests();
        }
    }
}