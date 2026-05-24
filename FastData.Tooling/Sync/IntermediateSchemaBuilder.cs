using System.Text;

namespace FastData.Tooling.Sync
{
    public class IntermediateSchemaBuilder
    {
        public string BuildSqlServerScript()
        {
            var builder = new StringBuilder();
            builder.AppendLine("create table fd_sync_task (task_id nvarchar(64) not null primary key, task_name nvarchar(128) not null, source_table nvarchar(128) not null, target_table nvarchar(128) not null, enabled bit not null default 1);");
            builder.AppendLine("create table fd_sync_batch (batch_id nvarchar(64) not null primary key, task_id nvarchar(64) not null, status nvarchar(32) not null, created_time datetime not null, finished_time datetime null);");
            builder.AppendLine("create table fd_sync_record (record_id nvarchar(64) not null primary key, batch_id nvarchar(64) not null, record_key nvarchar(256) null, payload nvarchar(max) not null, status nvarchar(32) not null, retry_count int not null default 0, error_message nvarchar(max) null);");
            builder.AppendLine("create table fd_sync_checkpoint (task_id nvarchar(64) not null primary key, checkpoint_value nvarchar(256) null, updated_time datetime not null);");
            builder.AppendLine("create table fd_sync_log (log_id nvarchar(64) not null primary key, task_id nvarchar(64) null, log_level nvarchar(32) not null, message nvarchar(max) not null, created_time datetime not null);");
            return builder.ToString();
        }
    }
}
