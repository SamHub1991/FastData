using FastData.Tooling.Sync;
using System;
using System.IO;
using Xunit;

namespace FastData.Tests.Config
{
    /// <summary>
    /// 同步配置管理器测试
    /// 
    /// 测试同步任务配置的保存、获取和删除功能。
    /// </summary>
    public class SyncConfigManagerTests
    {
        /// <summary>
        /// 获取临时配置文件路径
        /// </summary>
        /// <returns>临时文件路径</returns>
        private string GetTempConfigPath()
        {
            return Path.Combine(Path.GetTempPath(), "sync_test_" + Guid.NewGuid().ToString("N") + ".json");
        }

        /// <summary>
        /// 测试保存和获取任务配置
        /// </summary>
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

        /// <summary>
        /// 测试获取未知任务配置返回空
        /// </summary>
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

        /// <summary>
        /// 测试获取空任务 ID 返回空
        /// </summary>
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

        /// <summary>
        /// 测试删除任务配置
        /// </summary>
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

        /// <summary>
        /// 测试删除空任务 ID 无影响
        /// </summary>
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

        /// <summary>
        /// 测试删除不存在的任务配置无影响
        /// </summary>
        [Fact]
        public void DeleteTaskConfig_NonExistingTask_NoEffect()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                manager.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });

                manager.DeleteTaskConfig("nonexistent");
                Assert.NotNull(manager.GetTaskConfig("task1"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        /// <summary>
        /// 测试获取所有任务配置
        /// </summary>
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

        /// <summary>
        /// 测试保存多个任务配置
        /// </summary>
        [Fact]
        public void SaveMultipleTaskConfigs_WorksCorrectly()
        {
            var path = GetTempConfigPath();
            try
            {
                var manager = new SyncConfigManager(path);
                for (int i = 1; i <= 10; i++)
                {
                    manager.SaveTaskConfig(new SyncTaskConfig
                    {
                        TaskId = string.Format("task{0}", i),
                        TaskName = string.Format("Task {0}", i),
                        SourceTable = string.Format("source_{0}", i),
                        TargetTable = string.Format("target_{0}", i)
                    });
                }

                var all = manager.GetAllTaskConfigs();
                Assert.Equal(10, all.Count);

                var task5 = manager.GetTaskConfig("task5");
                Assert.NotNull(task5);
                Assert.Equal("Task 5", task5.TaskName);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        /// <summary>
        /// 测试配置文件持久化
        /// </summary>
        [Fact]
        public void ConfigFile_Persists_AcrossManagerInstances()
        {
            var path = GetTempConfigPath();
            try
            {
                // 第一个管理器保存配置
                var manager1 = new SyncConfigManager(path);
                manager1.SaveTaskConfig(new SyncTaskConfig { TaskId = "task1", TaskName = "Task 1" });

                // 第二个管理器读取配置
                var manager2 = new SyncConfigManager(path);
                var result = manager2.GetTaskConfig("task1");

                Assert.NotNull(result);
                Assert.Equal("Task 1", result.TaskName);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
