using System.Text;

namespace FastData.Tooling.Sync
{
    public class IntermediateSchemaBuilder
    {
        public string BuildScript(string provider)
        {
            var value = (provider ?? string.Empty).ToLower();
            if (value.Contains("mysql"))
                return BuildMySqlScript();

            if (value.Contains("oracle"))
                return BuildOracleScript();

            return BuildSqlServerScript();
        }

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

        public string BuildMySqlScript()
        {
            var builder = new StringBuilder();
            builder.AppendLine("create table fd_sync_task (task_id varchar(64) not null primary key, task_name varchar(128) not null, source_table varchar(128) not null, target_table varchar(128) not null, enabled bit not null default 1);");
            builder.AppendLine("create table fd_sync_batch (batch_id varchar(64) not null primary key, task_id varchar(64) not null, status varchar(32) not null, created_time datetime not null, finished_time datetime null);");
            builder.AppendLine("create table fd_sync_record (record_id varchar(64) not null primary key, batch_id varchar(64) not null, record_key varchar(256) null, payload longtext not null, status varchar(32) not null, retry_count int not null default 0, error_message longtext null);");
            builder.AppendLine("create table fd_sync_checkpoint (task_id varchar(64) not null primary key, checkpoint_value varchar(256) null, updated_time datetime not null);");
            builder.AppendLine("create table fd_sync_log (log_id varchar(64) not null primary key, task_id varchar(64) null, log_level varchar(32) not null, message longtext not null, created_time datetime not null);");
            return builder.ToString();
        }

        public string BuildOracleScript()
        {
            var builder = new StringBuilder();
            builder.AppendLine("create table fd_sync_task (task_id varchar2(64) not null primary key, task_name varchar2(128) not null, source_table varchar2(128) not null, target_table varchar2(128) not null, enabled number(1) default 1 not null);");
            builder.AppendLine("create table fd_sync_batch (batch_id varchar2(64) not null primary key, task_id varchar2(64) not null, status varchar2(32) not null, created_time date not null, finished_time date null);");
            builder.AppendLine("create table fd_sync_record (record_id varchar2(64) not null primary key, batch_id varchar2(64) not null, record_key varchar2(256) null, payload clob not null, status varchar2(32) not null, retry_count number(10) default 0 not null, error_message clob null);");
            builder.AppendLine("create table fd_sync_checkpoint (task_id varchar2(64) not null primary key, checkpoint_value varchar2(256) null, updated_time date not null);");
            builder.AppendLine("create table fd_sync_log (log_id varchar2(64) not null primary key, task_id varchar2(64) null, log_level varchar2(32) not null, message clob not null, created_time date not null);");
            return builder.ToString();
        }
    }
}
