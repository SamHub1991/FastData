using System;
using System.Collections.Generic;
using System.Timers;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.Services
{
    public enum SchedulerState { Stopped, Running, Paused }

    public class ScheduledTask
    {
        public string TaskId { get; set; }
        public bool Enabled { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public int RunCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string LastStatus { get; set; }
        public string LastError { get; set; }
        public SyncTaskConfig SyncConfig { get; set; }
        public DataSyncOptions Options { get; set; }
    }

    public class TaskSchedulerService : IDisposable
    {
        private readonly Dictionary<string, System.Timers.Timer> timers = new Dictionary<string, System.Timers.Timer>();
        private readonly Dictionary<string, ScheduledTask> tasks = new Dictionary<string, ScheduledTask>();
        private readonly SyncService syncService = new SyncService();
        private SchedulerState state = SchedulerState.Stopped;
        private bool disposed;

        public event EventHandler<ScheduledTask> TaskCompleted;
        public event EventHandler<ScheduledTask> TaskFailed;
        public event EventHandler<SchedulerState> StateChanged;

        public SchedulerState State { get { return state; } }
        public SyncService SyncService { get { return syncService; } }

        public void AddTask(SyncTaskConfig config, DataSyncOptions options, TimeSpan interval)
        {
            var task = new ScheduledTask
            {
                TaskId = config.TaskId,
                Enabled = config.IsEnabled,
                Interval = interval,
                SyncConfig = config,
                Options = options
            };
            tasks[config.TaskId] = task;

            var timer = new System.Timers.Timer(interval.TotalMilliseconds) { AutoReset = true };
            timer.Elapsed += (s, e) => ExecuteTask(task);
            timers[config.TaskId] = timer;

            if (config.IsEnabled && state == SchedulerState.Running) timer.Start();
        }

        public void RemoveTask(string taskId)
        {
            if (timers.ContainsKey(taskId)) { timers[taskId].Stop(); timers[taskId].Dispose(); timers.Remove(taskId); }
            tasks.Remove(taskId);
        }

        public void StartAll()
        {
            foreach (var kv in timers) { if (tasks.ContainsKey(kv.Key) && tasks[kv.Key].Enabled) kv.Value.Start(); }
            state = SchedulerState.Running;
            OnStateChanged();
        }

        public void StopAll()
        {
            foreach (var kv in timers) kv.Value.Stop();
            state = SchedulerState.Stopped;
            OnStateChanged();
        }

        public void PauseAll()
        {
            foreach (var kv in timers) kv.Value.Stop();
            state = SchedulerState.Paused;
            OnStateChanged();
        }

        public void ResumeAll()
        {
            foreach (var kv in timers) { if (tasks.ContainsKey(kv.Key) && tasks[kv.Key].Enabled) kv.Value.Start(); }
            state = SchedulerState.Running;
            OnStateChanged();
        }

        public void PauseTask(string taskId)
        {
            if (timers.ContainsKey(taskId)) { timers[taskId].Stop(); if (tasks.ContainsKey(taskId)) tasks[taskId].Enabled = false; }
        }

        public void ResumeTask(string taskId)
        {
            if (timers.ContainsKey(taskId)) { timers[taskId].Start(); if (tasks.ContainsKey(taskId)) tasks[taskId].Enabled = true; }
        }

        public void ExecuteTaskNow(string taskId)
        {
            if (tasks.ContainsKey(taskId)) ExecuteTask(tasks[taskId]);
        }

        private void ExecuteTask(ScheduledTask task)
        {
            try
            {
                task.LastRunTime = DateTime.Now;
                task.NextRunTime = DateTime.Now + task.Interval;
                task.LastStatus = "运行中";

                var result = syncService.ExecuteSync(task.Options);
                task.RunCount++;

                if (result.Success)
                {
                    task.SuccessCount++;
                    task.LastStatus = "成功";
                    task.LastError = null;
                    TaskCompleted?.Invoke(this, task);
                }
                else
                {
                    task.FailedCount++;
                    task.LastStatus = "失败";
                    task.LastError = result.ErrorMessage;
                    TaskFailed?.Invoke(this, task);
                }
            }
            catch (Exception ex)
            {
                task.FailedCount++;
                task.LastStatus = "异常";
                task.LastError = ex.Message;
                TaskFailed?.Invoke(this, task);
            }
        }

        public List<ScheduledTask> GetAllTasks() { return new List<ScheduledTask>(tasks.Values); }

        public ScheduledTask GetTask(string taskId)
        {
            return tasks.ContainsKey(taskId) ? tasks[taskId] : null;
        }

        private void OnStateChanged() { StateChanged?.Invoke(this, state); }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (var t in timers.Values) { t.Stop(); t.Dispose(); }
                timers.Clear();
                tasks.Clear();
                syncService.Dispose();
                disposed = true;
            }
        }
    }
}
