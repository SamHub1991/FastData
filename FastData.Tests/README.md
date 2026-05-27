# FastData.Tests

FastData.Tests is the unit test project for the FastData ORM ecosystem. It covers core features including sharding strategies, query building, pagination, configuration, and synchronization components.

## Test Framework

- **Framework**: xUnit 2.6.2
- **Test SDK**: Microsoft.NET.Test.Sdk 17.8.0
- **Target Frameworks**: `net462`, `net6.0`, `net8.0`, `net10.0`

## Test Structure

### Root Level Tests

| File | Tests |
|------|-------|
| `ChainableWhereTests.cs` | `DataQuery.ChainedConditions` - AND/OR chaining, complex SQL expressions, clear/count |
| `WhereBuilderTests.cs` | `WhereBuilder.BuildWhereClause()` - initial + chained condition combination |
| `PaginationTests.cs` | `PaginationResult<T>` - total pages, HasPrevious/HasNext, FromPageResult conversion |
| `ShardingTests.cs` | All sharding strategies (Time/Hash/List/Composite/QueryFrequency), ShardingManager lifecycle |
| `ShardingCrudTests.cs` | Sharding CRUD - configure, enable/disable, GetTableName, chainable API |

### Subdirectory Tests

| Directory | File | Tests |
|-----------|------|-------|
| `Abstractions/` | `DateTimeProviderTests.cs` | DateTime provider abstraction |
| `Adapter/` | `DatabaseAdapterFactoryTests.cs` | Database adapter factory |
| `Config/` | `DataConfigTests.cs` | Configuration loading |
| `Config/` | `DataSyncOptionsTests.cs` | Sync options |
| `Config/` | `SyncConfigManagerTests.cs` | Sync config manager |
| `Sync/` | `DataRowSerializerTests.cs` | DataRow serialization |
| `Sync/` | `PrimaryKeyConfigServiceTests.cs` | Primary key configuration |
| `Sync/` | `TimeRangeCalculatorTests.cs` | Time range calculation |

## Running Tests

```bash
# Run all tests
dotnet test FastData.Tests

# Run specific test class
dotnet test FastData.Tests --filter "ShardingTests"

# Run with verbose output
dotnet test FastData.Tests --verbosity normal

# Run for specific framework
dotnet test FastData.Tests --framework net10.0
```

## Current Test Results

```
Passed!  - Failed: 0, Passed: 162, Skipped: 0, Total: 162
```

## Test Coverage

### Sharding Tests
- Time sharding with daily/weekly/monthly granularity
- Hash sharding with modulo/consistent/CRC32 algorithms
- List sharding with value mapping
- Composite sharding (time + hash combination)
- Query frequency sharding with hot data detection
- Cold data migration to separate tables
- ShardingManager lifecycle (configure/enable/disable/reset)

### Query Tests
- AND/OR condition chaining
- Complex SQL expression building
- Where<T> predicate composition
- Dynamic condition combination

### Pagination Tests
- Page calculation accuracy
- HasPrevious/HasNext flags
- Select projection with pagination
- Dictionary result pagination

## Dependencies

- xUnit 2.6.2
- Microsoft.NET.Test.Sdk 17.8.0
- FastData
- FastUntility

## License

MIT License - see [LICENSE](../LICENSE) for details.
