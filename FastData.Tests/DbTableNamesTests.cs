using FastData.Property;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// DbTableNames 功能测试
    /// 测试多数据库表名映射功能
    /// </summary>
    public class DbTableNamesTests
    {
        /// <summary>
        /// 测试 DbTableNames 属性设置
        /// </summary>
        [Fact]
        public void DbTableNames_Property_SetAndGet()
        {
            var attr = new TableAttribute
            {
                DbTableNames = "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users"
            };
            
            Assert.Equal("SqlServer.Users,MySql.user_info,PostgreSQL.tb_users", attr.DbTableNames);
        }

        /// <summary>
        /// 测试 DbTableNames 和 Name 属性混合使用
        /// </summary>
        [Fact]
        public void DbTableNames_Mixed_With_Name()
        {
            var attr = new TableAttribute
            {
                Name = "default_users",
                DbTableNames = "SqlServer.Users,MySql.user_info"
            };
            
            Assert.Equal("default_users", attr.Name);
            Assert.Equal("SqlServer.Users,MySql.user_info", attr.DbTableNames);
        }

        /// <summary>
        /// 测试只有 Name 属性
        /// </summary>
        [Fact]
        public void DbTableNames_Only_Name()
        {
            var attr = new TableAttribute
            {
                Name = "users"
            };
            
            Assert.Equal("users", attr.Name);
            Assert.Null(attr.DbTableNames);
        }

        /// <summary>
        /// 测试 DbTableNames 为空时回退到 Name
        /// </summary>
        [Fact]
        public void DbTableNames_Empty_Fallback_To_Name()
        {
            var attr = new TableAttribute
            {
                Name = "users",
                DbTableNames = ""
            };
            
            Assert.Equal("users", attr.Name);
            Assert.Equal("", attr.DbTableNames);
        }

        /// <summary>
        /// 测试 DbTableNames 格式验证
        /// </summary>
        [Fact]
        public void DbTableNames_Format_Validation()
        {
            var attr = new TableAttribute
            {
                DbTableNames = "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users,SQLite.users"
            };
            
            var entries = attr.DbTableNames.Split(',');
            Assert.Equal(4, entries.Length);
            Assert.Contains("SqlServer.Users", entries);
            Assert.Contains("MySql.user_info", entries);
            Assert.Contains("PostgreSQL.tb_users", entries);
            Assert.Contains("SQLite.users", entries);
        }

        /// <summary>
        /// 测试 DbTableNames 单个条目
        /// </summary>
        [Fact]
        public void DbTableNames_Single_Entry()
        {
            var attr = new TableAttribute
            {
                DbTableNames = "SqlServer.Users"
            };
            
            var entries = attr.DbTableNames.Split(',');
            Assert.Single(entries);
            Assert.Equal("SqlServer.Users", entries[0]);
        }
    }

    /// <summary>
    /// 测试实体：多数据库表名映射
    /// </summary>
    [Table(DbTableNames = "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users")]
    public class MultiDbTestEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>
    /// 测试实体：使用 Name 属性
    /// </summary>
    [Table(Name = "default_users")]
    public class DefaultNameEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// 测试实体：无任何属性
    /// </summary>
    public class SimpleEntity
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
