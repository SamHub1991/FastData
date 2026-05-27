# FastData.Example

FastData.Example is a console application providing interactive, runnable examples covering all major FastData ORM features. It serves as a learning reference and quick-start guide.

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net45` | .NET Framework 4.5 |
| `net6.0` / `net8.0` / `net10.0` | Modern .NET |

## Examples

| # | File | Description |
|---|------|-------------|
| 1 | `BasicCrudExample.cs` | Basic CRUD operations (Create, Read, Update, Delete) |
| 2 | `LambdaQueryExample.cs` | `DataQuery<T>` chainable queries, `Where<T>` condition builder |
| 3 | `RawSqlExample.cs` | Raw SQL query execution |
| 4 | `MapSqlExample.cs` | XML-mapped SQL usage |
| 5 | `TransactionExample.cs` | Database transaction handling |
| 6 | `MultiDbExample.cs` | Multiple database connection switching |
| 7 | `DataSyncExample.cs` | Data synchronization tool usage |
| 8 | `MessageQueueExample.cs` | Message queue (RTU peak-shaving / multi-party push) |
| 9 | `PaginationExample.cs` | Pagination API with `PaginationResult<T>` |
| 10 | `ShardingExample.cs` | Basic sharding (data partitioning) |
| 11 | `ShardingFullExample.cs` | Complete sharding example with SQL Server |

## Running Examples

```bash
# Interactive mode
dotnet run --project FastData.Example --framework net10.0

# Direct example selection
echo "1" | dotnet run --project FastData.Example --framework net10.0
```

## Example Details

### 1. Basic CRUD
Demonstrates Create, Read, Update, Delete operations using `FastWrite` and `FastRead`.

### 2. Lambda Query
Shows `DataQuery<T>` chainable API with `Where<T>` condition builder:
```csharp
var users = FastRead.Query<User>()
    .Where(u => u.IsActive)
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Select(u => new { u.Id, u.Name })
    .ToList();
```

### 3. Raw SQL
Execute raw SQL queries with parameterized inputs.

### 4. XML Map SQL
Use XML-mapped SQL statements (similar to MyBatis):
```csharp
FastMap.Init("Maps/UserMap.xml");
var users = FastMap.Query<List<User>>("GetActiveUsers", new { DepartmentId = 1 });
```

### 5. Transaction
Database transaction handling with commit/rollback.

### 6. Multi-Database
Switch between multiple database connections using `FastDb.Use(key)`.

### 7. Data Sync
Demonstrate data synchronization between databases.

### 8. Message Queue (.NET 6+ only)
- **ReliableQueue**: Single consumer with acknowledgment
- **Stream**: Multiple consumer groups
- **FastWrite Queue**: Write-behind caching with `FastWrite.Queue<T>()`
- **FastRead Queue**: Read from queue with `FastRead.Queue<T>()`

### 9. Pagination
Pagination API with `PaginationResult<T>`:
```csharp
var page = FastRead.Query<User>()
    .Where(u => u.IsActive)
    .ToPagination(1, 20);
// page.Data, page.Total, page.TotalPages
```

### 10. Basic Sharding
Demonstrate table sharding with different strategies:
- Time-based sharding
- Hash-based sharding
- List-based sharding

### 11. Full Sharding Example
Complete sharding example with SQL Server:
- 10000 log records with time-based sharding
- 5000 order records with hash-based sharding
- Query frequency sharding with hot data detection
- Chainable API with `UseSharding()` and `WithShardingParam()`

## Configuration

### appsettings.json (.NET 6+)
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=FastDataDemo;Trusted_Connection=true;",
    "MySql": "Server=localhost;Database=FastDataDemo;Uid=root;Pwd=;",
    "Sqlite": "Data Source=FastDataDemo.db"
  },
  "Sharding": {
    "DefaultConnectionString": "Server=localhost;Database=FastDataDemo;Trusted_Connection=true;"
  }
}
```

### db.config (.NET Framework 4.5)
```xml
<?xml version="1.0" encoding="utf-8"?>
<db>
  <config>
    <add name="SqlServer" 
         providerName="System.Data.SqlClient" 
         connectionString="Server=.;Database=TestDb;Trusted_Connection=true;" />
  </config>
</db>
```

## Building

```bash
# Build for all targets
dotnet build FastData.Example

# Build for specific target
dotnet build FastData.Example --framework net10.0
```

## Dependencies

- FastData
- FastRedis
- FastUntility
- Microsoft.Extensions.Configuration.FileExtensions (net6+)

## License

MIT License - see [LICENSE](../LICENSE) for details.
