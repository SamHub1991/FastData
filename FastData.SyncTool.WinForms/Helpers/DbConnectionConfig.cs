using System;

namespace FastData.SyncTool.WinForms
{
    public class DbConnectionConfig
    {
        public string Name { get; set; }
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastTestTime { get; set; }
    }
}