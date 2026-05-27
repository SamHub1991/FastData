# FastData.Tooling

FastData.Tooling is a class library providing core tooling capabilities for the FastData ecosystem -- database metadata reading, C# model code generation, XML map SQL generation, and data synchronization abstractions.

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net45` | .NET Framework 4.5 |
| `net6.0` / `net8.0` / `net10.0` | Modern .NET |

## Installation

```bash
dotnet add package FastData.Tooling
```

## Key Features

### Database Metadata Reading

Read database schema information (tables, columns, types, primary keys):

```csharp
var reader = MetadataReaderFactory.Create("System.Data.SqlClient");
var tables = await reader.GetTablesAsync(connectionString);
var columns = await reader.GetColumnsAsync(connectionString, "Users");
var primaryKey = await reader.GetPrimaryKeyAsync(connectionString, "Users");
```

### C# Model Code Generation

Generate POCO classes from database schema:

```csharp
var generator = new ModelCodeGenerator();
var code = generator.GenerateCode(new CodeGenerationOptions
{
    TableName = "Users",
    Namespace = "MyApp.Models",
    Columns = columns
});
// Generates:
// [Table("Users")]
// public class User
// {
//     [Column("Id")]
//     public long Id { get; set; }
//     ...
// }
```

### XML Map SQL Generation

Generate XML-based SQL maps with CRUD operations:

```csharp
var generator = new XmlMapSqlGenerator();
var xml = generator.GenerateMap(new MapGenerationOptions
{
    TableName = "Users",
    Namespace = "MyApp.Maps",
    Columns = columns,
    PrimaryKey = "Id"
});
// Generates XML with SelectAll, SelectByPK, Insert, Update, Delete operations
```

### Data Sync Abstractions

```csharp
public interface IDataSyncService
{
    Task<SyncResult> SyncTableAsync(string sourceTable, string targetTable);
    Task<SyncResult> SyncWithTimeRangeAsync(string table, DateTime start, DateTime end);
    Task<ConnectionTestResult> TestConnectionAsync(string connectionString);
}
```

## Supported Databases

| Database | Provider Name |
|----------|---------------|
| SQL Server | `System.Data.SqlClient` |
| MySQL | `MySql.Data.MySqlClient` |
| SQLite | `System.Data.SQLite` |
| Oracle | `Oracle.ManagedDataAccess.Client` |
| DB2 | `IBM.Data.DB2.iSeries` |

## Type Mapping

The code generator maps database types to CLR types:

| Database Type | CLR Type |
|---------------|----------|
| `bigint` | `long` |
| `bit` | `bool` |
| `datetime` / `datetime2` | `DateTime` |
| `decimal` / `numeric` | `decimal` |
| `float` | `double` |
| `int` | `int` |
| `nvarchar` / `varchar` | `string` |
| `uniqueidentifier` | `Guid` |

## Dependencies

- FastUntility

## License

MIT License - see [LICENSE](../LICENSE) for details.
