using System;
using System.Collections.Generic;
using System.Data.Common;
using FastData.Context;
using FastData.Property;
using FastData.Sharding;
using FastUntility.Page;
using Xunit;

namespace FastData.Tests.Integration
{
    [Collection("Sharding")]
    public class MultiDatabaseShardingCrudTests : IDisposable
    {
        private static readonly string[] Databases = { "SqlServer", "MySql", "PostgreSql" };

        static MultiDatabaseShardingCrudTests()
        {
            try { DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance); } catch { }
            try { DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance); } catch { }
        }

        public MultiDatabaseShardingCrudTests()
        {
            ShardingManager.Clear();
        }

        public void Dispose()
        {
            ShardingManager.Clear();
        }

        public static IEnumerable<object[]> GetDatabases()
        {
            foreach (var db in Databases)
                yield return new object[] { db };
        }

        [Theory]
        [MemberData(nameof(GetDatabases))]
        public void ShardedCrud_RoutesToPhysicalTablesAcrossDatabases(string dbName)
        {
            if (!ShouldRunDbIntegration()) return;

            var tag = NewTag();
            var janTable = $"shard_orders_{tag}_202601";
            var febTable = $"shard_orders_{tag}_202602";

            ConfigureSharding(dbName, $"shard_orders_{tag}");
            DropTable(dbName, janTable);
            DropTable(dbName, febTable);
            CreateTable(dbName, janTable);
            CreateTable(dbName, febTable);

            try
            {
                using var client = Db.Use(dbName);
                var janOrder = CreateOrder($"{tag}_jan", new DateTime(2026, 1, 15), "new");
                var febOrder = CreateOrder($"{tag}_feb", new DateTime(2026, 2, 10), "new");

                var addJan = client.ShardAdd(janOrder);
                var addFeb = client.ShardAdd(febOrder);
                Assert.True(addJan.IsSuccess, $"{dbName} shard add jan failed: {addJan.Message}");
                Assert.True(addFeb.IsSuccess, $"{dbName} shard add feb failed: {addFeb.Message}");

                Assert.Equal(1, CountPhysicalRows(dbName, janTable, janOrder.OrderNo));
                Assert.Equal(0, CountPhysicalRows(dbName, janTable, febOrder.OrderNo));
                Assert.Equal(1, CountPhysicalRows(dbName, febTable, febOrder.OrderNo));

                var janRows = client.ShardQuery<ShardOrder>(
                    o => o.OrderNo.Contains(tag),
                    new Dictionary<string, object>
                    {
                        { "CreatedAt_Start", new DateTime(2026, 1, 1) },
                        { "CreatedAt_End", new DateTime(2026, 1, 31) }
                    });
                Assert.Single(janRows);
                Assert.Equal(janOrder.OrderNo, janRows[0].OrderNo);

                var update = client.ShardUpdate(
                    new ShardOrder { OrderNo = febOrder.OrderNo, CreatedAt = febOrder.CreatedAt, Status = "paid", Amount = 99 },
                    o => o.OrderNo == febOrder.OrderNo,
                    o => new { o.Status, o.Amount });
                Assert.True(update.IsSuccess, $"{dbName} shard update failed: {update.Message}");

                var febRows = client.ShardQuery<ShardOrder>(
                    o => o.OrderNo == febOrder.OrderNo,
                    new Dictionary<string, object>
                    {
                        { "CreatedAt_Start", new DateTime(2026, 2, 1) },
                        { "CreatedAt_End", new DateTime(2026, 2, 28) }
                    });
                Assert.Single(febRows);
                Assert.Equal("paid", febRows[0].Status);
                Assert.Equal(99, febRows[0].Amount);

                var page = client.ShardQueryPage<ShardOrder>(
                    o => o.OrderNo.Contains(tag),
                    new Dictionary<string, object>
                    {
                        { "CreatedAt_Start", new DateTime(2026, 1, 1) },
                        { "CreatedAt_End", new DateTime(2026, 2, 28) }
                    },
                    pageIndex: 1,
                    pageSize: 10);
                Assert.Equal(2, page.pModel.TotalRecord);
                Assert.Equal(2, page.list.Count);

                var delete = client.ShardDelete<ShardOrder>(
                    o => o.OrderNo == janOrder.OrderNo,
                    new Dictionary<string, object>
                    {
                        { "CreatedAt_Start", new DateTime(2026, 1, 1) },
                        { "CreatedAt_End", new DateTime(2026, 1, 31) }
                    });
                Assert.True(delete.IsSuccess, $"{dbName} shard delete failed: {delete.Message}");
                Assert.Equal(0, CountPhysicalRows(dbName, janTable, janOrder.OrderNo));
                Assert.Equal(1, CountPhysicalRows(dbName, febTable, febOrder.OrderNo));
            }
            finally
            {
                DropTable(dbName, janTable);
                DropTable(dbName, febTable);
            }
        }

        private static void ConfigureSharding(string dbName, string baseTableName)
        {
            ShardingManager.Configure<ShardOrder>(new ShardingConfig
            {
                DatabaseKey = dbName,
                BaseTableName = baseTableName,
                ShardingType = ShardingType.Time,
                TimeConfig = new TimeShardingConfig
                {
                    TimeField = "CreatedAt",
                    Granularity = TimeGranularity.Month,
                    StartTime = new DateTime(2026, 1, 1),
                    EndTime = new DateTime(2026, 2, 28)
                }
            });
        }

        private static ShardOrder CreateOrder(string orderNo, DateTime createdAt, string status)
        {
            return new ShardOrder
            {
                OrderNo = orderNo,
                CreatedAt = createdAt,
                Status = status,
                Amount = 12
            };
        }

        private static int CountPhysicalRows(string dbName, string tableName, string orderNo)
        {
            using var db = new DataContext(dbName);
            var sql = $"select count(0) Count from {QuoteTable(dbName, tableName)} where OrderNo={db.config.Flag}OrderNo";
            var parameter = DbProviderFactories.GetFactory(db.config.ProviderName).CreateParameter();
            parameter.ParameterName = "OrderNo";
            parameter.Value = orderNo;
            var result = db.ExecuteSqlList(sql, new[] { parameter }, isAop: false);
            foreach (var value in result.DicList[0].Values)
                return Convert.ToInt32(value);

            return 0;
        }

        private static void CreateTable(string dbName, string tableName)
        {
            using var db = new DataContext(dbName);
            var table = QuoteTable(dbName, tableName);
            var sql = dbName == "PostgreSql"
                ? $"create table {table} (Id serial primary key, OrderNo varchar(100) not null, CreatedAt timestamp not null, Status varchar(50) null, Amount integer not null)"
                : $"create table {table} (Id int identity(1,1) primary key, OrderNo varchar(100) not null, CreatedAt datetime not null, Status varchar(50) null, Amount int not null)";

            if (dbName == "MySql")
                sql = $"create table {table} (Id int auto_increment primary key, OrderNo varchar(100) not null, CreatedAt datetime not null, Status varchar(50) null, Amount int not null)";

            db.ExecuteSql(sql, isAop: false);
        }

        private static void DropTable(string dbName, string tableName)
        {
            using var db = new DataContext(dbName);
            var table = QuoteTable(dbName, tableName);
            var sql = dbName == "SqlServer" ? $"if object_id('{tableName}', 'U') is not null drop table {table}" : $"drop table if exists {table}";
            db.ExecuteSql(sql, isAop: false);
        }

        private static string QuoteTable(string dbName, string tableName)
        {
            if (dbName == "MySql") return $"`{tableName}`";
            if (dbName == "PostgreSql") return tableName.ToLowerInvariant();
            return tableName;
        }

        private static bool ShouldRunDbIntegration()
        {
            return string.Equals(Environment.GetEnvironmentVariable("FASTDATA_RUN_DB_INTEGRATION"), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NewTag()
        {
            return $"s{Guid.NewGuid():N}".Substring(0, 12);
        }
    }

    [Table(Name = "shard_orders")]
    public class ShardOrder
    {
        [Column(IsKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public string OrderNo { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }

        public int Amount { get; set; }
    }
}
