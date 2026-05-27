# FastRedis

FastRedis is a cross-platform .NET Redis library providing unified cache operations, message queues (ReliableQueue and Stream), and publish/subscribe capabilities. It abstracts the underlying Redis client, supporting both NServiceKit.Redis (.NET Framework 4.5) and NewLife.Redis (.NET 6+).

## Target Frameworks

| Framework | Redis Client |
|-----------|--------------|
| `net45` | NServiceKit.Redis 1.0.17 |
| `net6.0` / `net8.0` / `net10.0` | NewLife.Redis 6.0.2024.1006 |

## Installation

```bash
dotnet add package FastRedis
```

## Configuration

Create `db.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<db>
  <redis>
    <add name="default" 
         server="localhost:6379" 
         db="0" 
         password="" />
  </redis>
</db>
```

## Quick Start

### Cache Operations

```csharp
// Set value
RedisInfo.Set("user:1", new User { Name = "John" }, TimeSpan.FromHours(1));

// Get value
var user = RedisInfo.Get<User>("user:1");

// Delete
RedisInfo.Remove("user:1");

// Batch operations
var dic = new Dictionary<string, User>
{
    ["user:1"] = new User { Name = "John" },
    ["user:2"] = new User { Name = "Jane" }
};
RedisInfo.SetDic(dic);

var users = RedisInfo.GetDic<User>(new[] { "user:1", "user:2" });
```

### Async Operations

Every sync method has an async counterpart:

```csharp
await RedisInfo.SetAsy("user:1", user, TimeSpan.FromHours(1));
var user = await RedisInfo.GetAsy<User>("user:1");
await RedisInfo.RemoveAsy("user:1");
```

### Message Queue - ReliableQueue

Single consumer with acknowledgment and automatic rollback:

```csharp
// Producer
var producer = MessageQueueFactory.CreateReliableProducer(options);
await producer.PublishAsync("orders", order);

// Consumer
var consumer = MessageQueueFactory.CreateReliableConsumer(options);
var message = await consumer.ConsumeAsync("orders");
// Process message...
await consumer.AcknowledgeAsync(message.MessageId);
```

### Message Queue - Stream

Multiple consumer groups for broadcast/multi-party push:

```csharp
// Producer
var producer = MessageQueueFactory.CreateStreamProducer(options);
await producer.PublishAsync("events", eventData);

// Consumer Group A
var consumerA = MessageQueueFactory.CreateStreamConsumer(options, "group-a");
var message = await consumerA.ConsumeAsync("events");

// Consumer Group B
var consumerB = MessageQueueFactory.CreateStreamConsumer(options, "group-b");
var message = await consumerB.ConsumeAsync("events");
```

### Repository Pattern

```csharp
// Register services
services.AddScoped<IRedisRepository, RedisRepository>();

// Use repository
public class CacheService
{
    private readonly IRedisRepository _redis;
    
    public CacheService(IRedisRepository redis) => _redis = redis;
    
    public async Task<User> GetUserAsync(int id)
        => await _redis.GetAsync<User>($"user:{id}");
}
```

## Message Queue Integration

High-level integration service for DataTable publishing and multi-group consumption:

```csharp
var service = new MessageQueueIntegrationService();

// Publish DataTable
await service.PublishDataTableAsync("sync:users", dataTable);

// Consume with multiple groups
await service.ConsumeWithGroupsAsync("sync:users", 
    new[] { "group-a", "group-b" }, 
    async (message, group) => {
        // Process message per group
    });
```

## Namespaces

| Namespace | Purpose |
|-----------|---------|
| `FastRedis` | Core static API (`RedisInfo`) |
| `FastRedis.Config` | Configuration and caching |
| `FastRedis.Repository` | Repository pattern |
| `FastRedis.Messaging` | Message queue system |
| `FastRedis.Services` | High-level integration services |

## Features

- **Dual Backend**: Same API, NServiceKit.Redis on net45, NewLife.Redis on net6+
- **Read/Write Separation**: Independent read/write server configuration
- **Full Async**: Every sync method has an `*Asy` async version
- **Two Queue Modes**:
  - **ReliableQueue**: Single consumer, acknowledgment, automatic rollback
  - **Stream**: Multiple consumer groups, independent consumption
- **Generic Support**: Type-safe serialization/deserialization
- **Flexible Configuration**: `db.config`, `web.config`, or embedded resources
- **Built-in Error Logging**: All Redis exceptions logged to file

## Dependencies

### net45
- NServiceKit.Redis 1.0.17
- NServiceKit.Common 1.0.31

### net6.0 / net8.0 / net10.0
- NewLife.Redis 6.0.2024.1006
- Newtonsoft.Json 13.0.3

## License

MIT License - see [LICENSE](../LICENSE) for details.
