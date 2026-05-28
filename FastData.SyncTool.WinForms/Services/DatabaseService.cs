using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace FastData.SyncTool.WinForms.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "server=localhost;database=FastDataDemo;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";
        }

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<string> GetTableList()
        {
            var tables = new List<string>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return tables;
        }

        public async Task<int> GetTableCountAsync(string tableName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("SELECT COUNT(*) FROM [{0}]", tableName);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<DataTable> GetTableDataAsync(string tableName, int offset, int batchSize)
        {
            var dataTable = new DataTable();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        "SELECT * FROM [{0}] ORDER BY (SELECT NULL) OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY",
                        tableName, offset, batchSize);
                    using (var adapter = new SqlDataAdapter((SqlCommand)cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            return dataTable;
        }
    }
}
