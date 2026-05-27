# FastUntility

FastUntility is a C# utility library for .NET development, providing commonly used helper classes for JSON handling, logging, caching, HTTP requests, encryption, Excel generation, data mapping, pagination, and more.

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net45` | Full feature set (WCF, WebAPI, MSMQ, Session, Cookie, Windows Service) |
| `net6.0` / `net8.0` / `net10.0` | Cross-platform subset |

## Installation

```bash
dotnet add package FastUntility
```

## Key Features

### JSON Operations
```csharp
// Serialize
var json = BaseJson.ModelToJson(myObject);

// Deserialize
var model = BaseJson.JsonToModel<MyClass>(jsonString);

// DbDataReader to JSON
var jsonArray = BaseJson.DataReaderToJson(reader);
```

### Logging
```csharp
// File-based logging
BaseLog.SaveLog("Operation completed", "app.log");

// Async logging
await BaseLog.SaveLogAsync("Async operation", "app.log");
```

### Caching
```csharp
// Set cache
BaseCache.Set("key", value, TimeSpan.FromMinutes(30));

// Get cache
var value = BaseCache.Get<T>("key");

// Remove cache
BaseCache.Remove("key");
```

### HTTP Client
```csharp
// GET request
var response = await BaseUrl.GetAsync("https://api.example.com/data");

// POST request
var result = await BaseUrl.PostAsync("https://api.example.com/submit", jsonData);
```

### Encryption
```csharp
// MD5 hash
var hash = BaseSymmetric.MD5("text");

// Rijndael encryption
var encrypted = BaseSymmetric.Encrypto("text", "key");
var decrypted = BaseSymmetric.Decrypto(encrypted, "key");
```

### Excel Generation
```csharp
// Create Excel workbook
var workbook = BaseExcel.Init("Sheet1", title, dataList);
var bytes = BaseExcel.ToBytes(workbook);
```

### Type Conversion Extensions
```csharp
// Safe type conversion
int num = "123".ToInt();
decimal price = "99.99".ToDecimal();
DateTime date = "2024-01-01".ToDate();
string text = obj.ToStr();
```

### Pagination
```csharp
var request = new PaginationRequest { PageIndex = 1, PageSize = 20 };
var result = new PaginationResult<T>
{
    Data = items,
    Total = totalCount,
    Page = request.PageIndex,
    PageSize = request.PageSize
};
```

### Lambda Expression Builder
```csharp
// Build dynamic WHERE conditions
var predicate = LambdaBuilder.Where<User>(u => u.IsActive);
predicate = predicate.And(u => u.Age > 18);
```

## Namespaces

| Namespace | Purpose |
|-----------|---------|
| `FastUntility.Base` | Core utilities: JSON, logging, HTTP, Excel, encryption, type conversion |
| `FastUntility.Cache` | Caching: in-memory, Session, Cookie |
| `FastUntility.Builder` | Lambda expression composition |
| `FastUntility.Page` | Pagination models |
| `FastUntility.Host` | WCF/WebAPI self-hosting (net45 only) |
| `FastUntility.WinService` | Windows Service management (net45 only) |

## Dependencies

### net45
- NPOI 2.5.6
- Newtonsoft.Json 13.0.3
- Microsoft.AspNet.WebApi.SelfHost 5.2.9

### net6.0 / net8.0 / net10.0
- NPOI 2.7.0
- Newtonsoft.Json 13.0.3
- Microsoft.Extensions.Caching.Memory 8.0.0

## License

MIT License - see [LICENSE](../LICENSE) for details.
