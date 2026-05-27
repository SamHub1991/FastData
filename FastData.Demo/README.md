# FastData.Demo

FastData.Demo is an ASP.NET Core Web API application demonstrating the full FastData technology stack -- Repository pattern, Redis caching, data synchronization, message queues, pagination, and table sharding.

## Target Framework

`net10.0` (.NET 10)

## Features

- **Repository Pattern**: IFastRepository with read/write separation
- **Redis Caching**: Distributed cache with TTL support
- **Data Sync**: Background data synchronization
- **Message Queues**: ReliableQueue and Stream with FastWrite/FastRead chainable API
- **Pagination**: `PaginationResult<T>` with `ToPagination()` method
- **Table Sharding**: Time/Hash/List/Composite/QueryFrequency strategies
- **Swagger UI**: Interactive API documentation

## API Endpoints

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/users` | Get all users |
| `GET` | `/api/users/{id}` | Get user by ID (cached) |
| `GET` | `/api/users/active` | Get active users (cached) |
| `GET` | `/api/users/department/{dept}` | Get users by department |
| `GET` | `/api/users/paged` | Paginated user list |
| `GET` | `/api/users/search` | Search with dynamic Where<T> |
| `POST` | `/api/users` | Create user |
| `PUT` | `/api/users/{id}` | Update user |
| `DELETE` | `/api/users/{id}` | Delete user |

### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/orders` | Get all orders |
| `GET` | `/api/orders/{id}` | Get order by ID (cached) |
| `GET` | `/api/orders/user/{userId}` | Get orders by user |
| `POST` | `/api/orders` | Create order |
| `PUT` | `/api/orders/{id}/status` | Update order status |

### Sync

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/sync/all` | Sync all tables |
| `POST` | `/api/sync/users` | Sync users table |
| `POST` | `/api/sync/orders` | Sync orders table |

### Pagination

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/pagination/users` | Paginated users |
| `POST` | `/api/pagination/users/search` | Paginated search |
| `GET` | `/api/pagination/users/department/{dept}` | Department pagination |
| `GET` | `/api/pagination/users/async` | Async pagination |
| `GET` | `/api/pagination/users/dictionary` | Dictionary pagination |

### Sharding

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/sharding/init` | Initialize sharding tables |
| `POST` | `/api/sharding/insert-data` | Insert test data |
| `POST` | `/api/sharding/time/configure` | Configure time sharding |
| `GET` | `/api/sharding/time/query` | Query time-sharded data |
| `POST` | `/api/sharding/hash/configure` | Configure hash sharding |
| `GET` | `/api/sharding/hash/query` | Query hash-sharded data |
| `POST` | `/api/sharding/list/configure` | Configure list sharding |
| `GET` | `/api/sharding/list/query` | Query list-sharded data |
| `POST` | `/api/sharding/frequency/configure` | Configure frequency sharding |
| `POST` | `/api/sharding/frequency/record` | Record query frequency |
| `POST` | `/api/sharding/frequency/simulate` | Simulate queries |
| `GET` | `/api/sharding/frequency/hot` | Get hot data values |
| `POST` | `/api/sharding/sync` | Sync sharding data |
| `GET` | `/api/sharding/stats` | Get sharding statistics |

### Message Queue

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/mq/demo/reliable` | ReliableQueue demo |
| `POST` | `/api/mq/demo/stream` | Stream demo |
| `POST` | `/api/mq/demo/write-queue` | FastWrite queue demo |
| `POST` | `/api/mq/demo/read-queue` | FastRead queue demo |
| `GET` | `/api/mq/status/{topic}` | Get queue status |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check |
| `GET` | `/` | Service info |

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=FastDataDemo;Trusted_Connection=true;",
    "MySql": "Server=localhost;Database=FastDataDemo;Uid=root;Pwd=;",
    "Sqlite": "Data Source=FastDataDemo.db"
  },
  "Redis": {
    "Server": "localhost:6379",
    "Db": "0"
  },
  "Sharding": {
    "DefaultConnectionString": "Server=localhost;Database=FastDataDemo;Trusted_Connection=true;"
  }
}
```

## Running

```bash
# Run the demo
dotnet run --project FastData.Demo --urls "http://0.0.0.0:5000"

# Access Swagger UI
# http://localhost:5000/swagger
```

## Building

```bash
dotnet build FastData.Demo
```

## Dependencies

- FastData
- FastRedis
- FastUntility
- FastData.Tooling
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.Data.SqlClient 5.2.0

## License

MIT License - see [LICENSE](../LICENSE) for details.
