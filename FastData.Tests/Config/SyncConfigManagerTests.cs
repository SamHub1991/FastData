using FastData.Tooling.Sync;
using System;
using System.IO;
using Xunit;

namespace FastData.Tests.Config
{
    public class SyncConfigManagerTests
    {
        private string GetTempConfigPath()
        {
            return Path.Combine(Path.GetTempPath(), "sync_test_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [Fact]
        public void SaveAndGetTaskConfig_WorksCorrectly()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var config = new SyncTaskConfig
                {
                    TaskId = "task1",
                    TaskName = "Test Task",
                    SourceTable = "orders",
                    TargetTable = "orders_copy"
                };
                manager.SaveTaskConfig(config);

                var retrieved = manager.GetTaskConfig("task1");
                Assert.NotNull(retrieved);
                Assert.Equal("task1", retrieved.TaskId);
                Assert.Equal("Test Task", retrieved.TaskName);
                Assert.Equal("orders", retrieved.SourceTable);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void GetTaskConfig_UnknownTask_ReturnsNull()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var result = manager.GetTaskConfig("unknown");
                Assert.Null(result);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void GetTaskConfig_NullTaskId_ReturnsNull()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var result = manager.GetTaskConfig(null);
                Assert.Null(result);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void DeleteTaskConfig_RemovesConfig()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task2", TaskName = "Task 2" });

                manager.DeleteTaskConfig("task1");
                Assert.Null(manager.GetTaskConfig("task1"));
                Assert.NotNull(manager.GetTaskConfig("task2"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void DeleteTaskConfig_NullTaskId_NoEffect()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager.DeleteTaskConfig(null);
                Assert.NotNull(manager.GetTaskConfig("task1"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void GetAllTaskConfigs_ReturnsAllConfigs()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task2", TaskName = "Task 2" });
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task3", TaskName = "Task 3" });

                var all = manager.GetAllTaskConfigs();
                Assert.Equal(3, all.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void UpdateLastSyncTime_UpdatesIsFirstSync()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var config = new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" };
                manager.SaveTaskConfig(config);

                manager.UpdateLastSyncTime("task1", DateTime.Now);

                var updated = manager.GetTaskConfig("task1");
                Assert.False(updated.IsFirstSync);
                Assert.NotNull(updated.LastSyncTime);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void UpdateTaskStatus_UpdatesStatusAndMessage()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });

                manager.UpdateTaskStatus("task1", "Success", "100 rows synced");

                var updated = manager.GetTaskConfig("task1");
                Assert.Equal("Success", updated.LastSyncStatus);
                Assert.Equal("100 rows synced", updated.LastSyncMessage);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void PersistAndReload_ConfigsPersistAcrossInstances()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager1 = new SyncConfigManager(path);
                manager1.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager1.SaveTaskConfig(new SyncTaskConfig { TaskId = "task2", TaskName = "Task 2" });

                var manager2 = new SyncConfigManager(path);
                Assert.NotNull(manager2.GetTaskConfig("task1"));
                Assert.NotNull(manager2.GetTaskConfig("task2"));
                Assert.Equal(2, manager2.GetAllTaskConfigs().Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void NonExistentConfigFile_InitializesEmpty()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                Assert.Equal(0, manager.GetAllTaskConfigs().Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
