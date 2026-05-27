# FastData

FastData is a lightweight, multi-database ORM (Object-Relational Mapping) library for .NET. It provides fluent API for CRUD operations, XML-based SQL mapping (similar to MyBatis), Code-First/Db-First patterns, table sharding, message queues, and AOP interception.

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net45` | .NET Framework 4.5 |
| `net6.0` / `net8.0` / `net10.0` | Modern .NET |

## Installation

```bash
dotnet add package FastData
```

## Supported Databases

| Database | Provider |
|----------|----------|
| SQL Server | `System.Data.SqlClient` |
| MySQL | `MySql.Data.MySqlClient` |
| Oracle | `Oracle.ManagedDataAccess.Client` |
| SQLite | `System.Data.SQLite` |
| DB2 | `IBM.Data.DB2.iSeries` |
| PostgreSQL | `Npgsql` |

## Quick Start

### Configuration

Create `db.config`:
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

### Basic CRUD
```csharp
// Query
var users = FastRead.Query<User>()
    .Where(u => u.IsActive)
    .ToList();

// Insert
FastWrite.Add(new User { Name = "John", Age = 30 });

// Update
FastWrite.Update<User>(u => u.Id == 1, u => new User { Name = "Updated" });

// Delete
FastWrite.Delete<User>(u => u.Id == 1);
```

### Lambda Query
```csharp
// Chainable query
var result = FastRead.Query<Order>()
    .Where(o => o.UserId == 1)
    .Where(o => o.Total > 100)
    .OrderBy(o => o.CreateTime)
    .Select(o => new { o.Id, o.Total })
    .ToPage(1, 20);

// Where<T> condition builder
var predicate = WhereBuilder.BuildWhere<Order>(o => o.Status == "Pending");
predicate = predicate.And(o => o.Amount > 50);
```

### XML Map SQL
```csharp
// Load XML map
FastMap.Init("Maps/UserMap.xml");

// Execute mapped query
var users = FastMap.Query<List<User>>("GetActiveUsers", new { DepartmentId = 1 });
```

### Pagination
```csharp
var page = FastRead.Query<User>()
    .Where(u => u.IsActive)
    .ToPagination(1, 20);

// page.Data, page.Total, page.Page, page.PageSize, page.TotalPages
```

### Table Sharding
```csharp
// Configure sharding
var config = new TimeShardingConfig
{
    TableName = "Logs",
    TimeField = "CreateTime",
    Granularity = TimeGranularity.Month
};
ShardingManager.ConfigureTimeSharding(config);

// Query with sharding
var logs = FastRead.Query<Log>()
    .UseSharding()
    .WithTimeRange(startTime, endTime)
    .ToList();
```

### Message Queue (.NET 6+)
```csharp
// Write-behind queue
FastWrite.Queue<Order>()
    .WithTableName("Orders")
    .WithBatchSize(100)
    .Add(order);

// Read from queue
var orders = FastRead.Queue<Order>()
    .WithTableName("Orders")
    .Take(10)
    .ToList();
```

### Repository Pattern
```csharp
// Register services
services.AddScoped<IFastRepository, FastRepository>();

// Use repository
public class UserService
{
    private readonly IFastRepository _repo;
    
    public UserService(IFastRepository repo) => _repo = repo;
    
    public async Task<User> GetUserAsync(int id)
        => await _repo.Query<User>().Where(u => u.Id == id).ToItemAsync();
}
```

### AOP Interception
```csharp
public class LoggingAop : IFastAop
{
    public void OnBefore(BeforeContext context)
        => Console.WriteLine($"Executing: {context.Method}");
    
    public void OnAfter(AfterContext context)
        => Console.WriteLine($"Completed: {context.Method}");
    
    public void OnException(ExceptionContext context)
        => Console.WriteLine($"Error: {context.Exception.Message}");
}
```

## Namespaces

| Namespace | Purpose |
|-----------|---------|
| `FastData` | Top-level entry points (FastRead, FastWrite, FastMap, FastDb) |
| FastData.Base | SQL building, expression visiting, caching |
| FastData.Model | Data models (ConfigModel, DataQuery, DataReturn) |
| FastData.Context | Database context for CRUD operations |
| FastData.Config | XML configuration loading |
| FastData.Adapter | SQL dialect implementations |
| FastData.Sharding | Table sharding strategies |
| FastData.Queue | Write-behind message queue |
| FastData.Repository | Repository pattern interfaces and implementations |
| FastData.Aop | AOP interception interfaces |

## Dependencies

- FastRedis
- FastUntility
- System.Configuration.ConfigurationManager 8.0.0

## License

MIT License - see [LICENSE](../LICENSE) for details.
