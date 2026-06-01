using FastData.Migrations;

namespace FastData.Migrations.Examples
{
    /// <summary>
    /// 示例迁移：创建用户表
    /// </summary>
    public class CreateUsersTable_20260101_001 : Migration
    {
        public override string Version => "20260101_001";
        public override string Description => "创建用户表";

        public override void Up()
        {
            var builder = new SqlMigrationBuilder();
            
            builder.CreateTable("sys_user", table =>
            {
                table.Column("id", "INT IDENTITY(1,1)", nullable: false)
                     .PrimaryKey("id");
                table.Column("name", "NVARCHAR(50)", nullable: false);
                table.Column("email", "NVARCHAR(100)", nullable: false);
                table.Column("age", "INT", nullable: true);
                table.Column("is_active", "BIT", nullable: false, defaultValue: "1");
                table.Column("create_time", "DATETIME", nullable: false, defaultValue: "GETDATE()");
                table.Column("update_time", "DATETIME", nullable: true);
            });

            builder.CreateIndex("idx_user_email", "sys_user", "email");
            builder.CreateIndex("idx_user_name", "sys_user", "name");

            foreach (var sql in builder.GetSqlStatements())
            {
                ExecuteSql(sql);
            }
        }

        public override void Down()
        {
            var builder = new SqlMigrationBuilder();
            
            builder.DropIndex("idx_user_email");
            builder.DropIndex("idx_user_name");
            builder.DropTable("sys_user");

            foreach (var sql in builder.GetSqlStatements())
            {
                ExecuteSql(sql);
            }
        }

        private void ExecuteSql(string sql)
        {
            // 在实际实现中，这里应该执行 SQL
            System.Console.WriteLine($"Executing: {sql}");
        }
    }

    /// <summary>
    /// 示例迁移：添加用户头像字段
    /// </summary>
    public class AddUserAvatar_20260102_001 : Migration
    {
        public override string Version => "20260102_001";
        public override string Description => "添加用户头像字段";

        public override void Up()
        {
            var builder = new SqlMigrationBuilder();
            builder.AddColumn("sys_user", "avatar", "NVARCHAR(500)", nullable: true);
            
            foreach (var sql in builder.GetSqlStatements())
            {
                ExecuteSql(sql);
            }
        }

        public override void Down()
        {
            var builder = new SqlMigrationBuilder();
            builder.DropColumn("sys_user", "avatar");
            
            foreach (var sql in builder.GetSqlStatements())
            {
                ExecuteSql(sql);
            }
        }

        private void ExecuteSql(string sql)
        {
            System.Console.WriteLine($"Executing: {sql}");
        }
    }

    /// <summary>
    /// 变更跟踪使用示例
    /// </summary>
    public class ChangeTrackingExample
    {
        public void Example()
        {
            var tracker = new ChangeTracking.ChangeTracker();
            
            var user = new { Id = 1, Name = "张三", Email = "zhangsan@example.com" };
            
            // 开始跟踪
            tracker.Track(user);
            
            // 修改属性
            var userType = user.GetType();
            userType.GetProperty("Name")?.SetValue(user, "李四");
            userType.GetProperty("Email")?.SetValue(user, "lisi@example.com");
            
            // 获取变更
            var changes = tracker.GetChanges(user);
            foreach (var change in changes)
            {
                System.Console.WriteLine($"变更: {change.PropertyName}");
                System.Console.WriteLine($"  原始值: {change.OriginalValue}");
                System.Console.WriteLine($"  当前值: {change.CurrentValue}");
                System.Console.WriteLine($"  类型: {change.ChangeType}");
            }
            
            // 生成 UPDATE SQL
            var updateSql = tracker.GetUpdateSql(user, "sys_user");
            System.Console.WriteLine($"UPDATE SQL: {updateSql}");
            
            // 更新快照
            tracker.UpdateSnapshot(user);
            
            // 清除跟踪
            tracker.Untrack(user);
        }
    }

    /// <summary>
    /// 迁移使用示例
    /// </summary>
    public class MigrationExample
    {
        public void Example()
        {
            var connectionString = "Server=localhost;Database=MyDb;User Id=sa;Password=123;";
            var migrationManager = new MigrationManager(connectionString);
            
            // 添加迁移
            migrationManager.AddMigration(new CreateUsersTable_20260101_001());
            migrationManager.AddMigration(new AddUserAvatar_20260102_001());
            
            // 执行所有未应用的迁移
            migrationManager.Migrate();
            
            // 获取迁移历史
            var history = migrationManager.GetMigrationHistory();
            System.Console.WriteLine("迁移历史:");
            foreach (var info in history)
            {
                System.Console.WriteLine($"  {info.Version}: {info.Description} - {(info.AppliedAt.HasValue ? "已应用" : "未应用")}");
            }
            
            // 回滚到指定版本
            migrationManager.Rollback("20260101_001");
        }
    }
}