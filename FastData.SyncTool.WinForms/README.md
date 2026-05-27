# FastData.SyncTool.WinForms

FastData.SyncTool.WinForms is a Windows Forms desktop application for configuring and executing database-to-database data synchronization tasks. It supports scheduled sync, data replay, and table sharding operations.

## Target Framework

.NET Framework 4.5 (legacy WinForms project)

## Features

### Database Connection Management
- Save, test, and delete named database connections
- Provider selection (SQL Server, MySQL, SQLite, Oracle, DB2)
- Connection string configuration

### Sync Configuration
- Configure source/target/intermediate database connections
- Table selection with primary key mapping
- Time-range filtering for historical data replay
- Batch size and retry count configuration
- Deduplication options

### Task Management
- Create, edit, delete sync task configurations
- Batch enable/disable tasks
- Import/export configurations (JSON-based)
- Task execution history and logging

### Scheduled Sync
- Timer-based automatic synchronization
- Configurable intervals (5-3600 seconds)
- Start/stop scheduler controls

### Data Replay
- Re-sync historical data by time range
- Table-specific replay options
- Progress tracking with read/write/failed counts

### Sharding Operations
The SyncTool includes dedicated tabs for sharding operations:

**Sharding Configuration Tab**
- Visual configuration editor for 5 sharding strategies
- PropertyGrid-based detail editing
- Configuration import/export (JSON)
- Cross-environment configuration validation
- Color-coded configuration status

**Sharding Sync Tab**
- Time/Hash/List/Composite/QueryFrequency strategy configuration
- Strategy-specific settings panels
- Enable/disable individual strategies

**Sharding Data Import Tab**
- CSV/Excel file import
- Automatic sharding table routing
- Upsert logic (INSERT if not exists, UPDATE if exists)
- Background import with progress tracking
- Cancellation support

**Sharding Data Operations Tab**
- Table statistics (row count, size, type)
- Cross-table UNION ALL queries
- Batch INSERT/UPDATE/DELETE operations
- CSV export functionality

### Background Task Management
- Task state tracking (Running/Paused/Completed/Failed)
- Pause/Resume/Cancel controls
- Real-time progress monitoring
- Task history with color-coded status

## Architecture

### DI Container
```csharp
ServiceContainer.Register<ISyncService, SyncService>();
ServiceContainer.Register<ILogService, LogService>();
ServiceContainer.Register<ITaskSchedulerService, TaskSchedulerService>();
ServiceContainer.Register<IPrimaryKeyConfigService, PrimaryKeyConfigService>();
ServiceContainer.Register<ShardingTaskService>();
```

### Key Components
| Component | Purpose |
|-----------|---------|
| `MainForm` | Main application window with TabControl |
| `ShardingSyncControl` | Sharding strategy configuration |
| `ShardingTaskControl` | Background task management |
| `ShardingImportControl` | Data import with auto-routing |
| `ShardingDataControl` | Data operations and statistics |
| `ShardingCrudControl` | Sharding table CRUD operations |
| `ShardingMigrationControl` | Table migration management |

## Usage

1. **Configure Connections**: Add database connections in the Connections tab
2. **Create Sync Task**: Configure source/target tables and sync options
3. **Run Sync**: Execute sync manually or enable scheduled sync
4. **Monitor Progress**: View real-time logs and progress indicators
5. **Sharding Operations**: Use dedicated tabs for sharding configuration and data management

## Building

```bash
# Build for .NET Framework 4.5 (requires Windows)
dotnet build FastData.SyncTool.WinForms
```

## Dependencies

- FastData
- FastData.Tooling
- FastUntility

## License

MIT License - see [LICENSE](../LICENSE) for details.
