using System;

namespace FastData.Tests
{
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message ?? "Expected true but was false"}");
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
                throw new Exception($"Assertion failed: {message ?? "Expected false but was true"}");
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
                throw new Exception($"Assertion failed: {message ?? "Expected not null but was null"}");
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
                throw new Exception($"Assertion failed: {message ?? "Expected null but was not null"}");
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual))
                throw new Exception($"Assertion failed: {message ?? $"Expected {expected} but was {actual}"}");
        }

        public static void Throws<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
                throw new Exception($"Assertion failed: {message ?? $"Expected exception {typeof(T).Name} but no exception was thrown"}");
            }
            catch (T)
            {
                // Expected
            }
            catch (Exception ex)
            {
                throw new Exception($"Assertion failed: {message ?? $"Expected exception {typeof(T).Name} but got {ex.GetType().Name}"}");
            }
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

            // Run all test classes
            RunTimeRangeCalculatorTests();
            RunDatabaseAdapterFactoryTests();
            RunDataConfigTests();

            Console.WriteLine();
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Passed: {_passed}");
            Console.WriteLine($"Failed: {_failed}");
            Console.WriteLine($"Total: {_passed + _failed}");

            if (_failed > 0)
                Environment.Exit(1);
        }

        private void RunTest(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine($"  [PASS] {name}");
                _passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
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
            RunTest("GetAdapter_SqlServer_ReturnsValidAdapter", tests.GetAdapter_SqlServer_ReturnsValidAdapter);
            Console.WriteLine();
        }

        private void RunDataConfigTests()
        {
            Console.WriteLine("DataConfigTests:");
            var tests = new Config.DataConfigTests();
            RunTest("GetConnection_ExistingKey_ReturnsConnection", tests.GetConnection_ExistingKey_ReturnsConnection);
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
