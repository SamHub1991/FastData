using FastData.Tooling.Sync;
using System;
using System.IO;

namespace FastData.Tests.Config
{
    public class SyncConfigManagerTests
    {
        private string GetTempConfigPath()
        {
            return Path.Combine(Path.GetTempPath(), "sync_test_" + Guid.NewGuid().ToString("N") + ".json");
        }

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
                Assert.IsNotNull(retrieved);
                Assert.AreEqual("task1", retrieved.TaskId);
                Assert.AreEqual("Test Task", retrieved.TaskName);
                Assert.AreEqual("orders", retrieved.SourceTable);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void GetTaskConfig_UnknownTask_ReturnsNull()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var result = manager.GetTaskConfig("unknown");
                Assert.IsNull(result);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void GetTaskConfig_NullTaskId_ReturnsNull()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                var result = manager.GetTaskConfig(null);
                Assert.IsNull(result);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void DeleteTaskConfig_RemovesConfig()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task2", TaskName = "Task 2" });

                manager.DeleteTaskConfig("task1");
                Assert.IsNull(manager.GetTaskConfig("task1"));
                Assert.IsNotNull(manager.GetTaskConfig("task2"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void DeleteTaskConfig_NullTaskId_NoEffect()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager.DeleteTaskConfig(null);
                Assert.IsNotNull(manager.GetTaskConfig("task1"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

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
                Assert.AreEqual(3, all.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

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
                Assert.IsFalse(updated.IsFirstSync);
                Assert.IsNotNull(updated.LastSyncTime);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void UpdateTaskStatus_UpdatesStatusAndMessage()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });

                manager.UpdateTaskStatus("task1", "Success", "100 rows synced");

                var updated = manager.GetTaskConfig("task1");
                Assert.AreEqual("Success", updated.LastSyncStatus);
                Assert.AreEqual("100 rows synced", updated.LastSyncMessage);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void PersistAndReload_ConfigsPersistAcrossInstances()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager1 = new SyncConfigManager(path);
                manager1.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });
                manager1.SaveTaskConfig(new SyncTaskConfig { TaskId = "task2", TaskName = "Task 2" });

                var manager2 = new SyncConfigManager(path);
                Assert.IsNotNull(manager2.GetTaskConfig("task1"));
                Assert.IsNotNull(manager2.GetTaskConfig("task2"));
                Assert.AreEqual(2, manager2.GetAllTaskConfigs().Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void NonExistentConfigFile_InitializesEmpty()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                Assert.AreEqual(0, manager.GetAllTaskConfigs().Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}