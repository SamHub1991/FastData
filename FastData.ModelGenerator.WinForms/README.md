# FastData.ModelGenerator.WinForms

FastData.ModelGenerator.WinForms is a Windows Forms desktop tool for generating C# model classes and XML SQL map files from database schema metadata (DbFirst code generation).

## Target Frameworks

| Framework | Notes |
|-----------|-------|
| `net6.0-windows` | .NET 6 Windows Desktop |
| `net8.0-windows` | .NET 8 Windows Desktop |
| `net10.0-windows` | .NET 10 Windows Desktop |

## Features

### Database Connection
- Provider selection dropdown (SQL Server, MySQL, SQLite, Oracle, DB2)
- Connection string input with test functionality
- Connection history management

### Table Discovery
- Load all tables and views from database
- Search/filter tables by name
- Include/exclude views checkbox
- Table count display

### C# Model Code Generation
Generate POCO classes with attributes:

```csharp
[Table("Users")]
public class User
{
    [Column("Id")]
    public long Id { get; set; }
    
    [Column("Name")]
    public string Name { get; set; }
    
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}
```

### XML Map SQL Generation
Generate XML-based SQL maps with CRUD operations:

```xml
<?xml version="1.0" encoding="utf-8"?>
<SqlMap namespace="UserMap">
  <Select id="SelectAll">
    SELECT * FROM [Users]
  </Select>
  
  <Select id="SelectByPrimaryKey">
    SELECT * FROM [Users] WHERE [Id] = @Id
  </Select>
  
  <Select id="SelectWithDynamicConditions">
    SELECT * FROM [Users]
    <dynamic prepend="WHERE">
      <isNotEmpty prepend="AND" property="Name">
        [Name] = @Name
      </isNotEmpty>
    </dynamic>
  </Select>
  
  <Insert id="Insert">
    INSERT INTO [Users] ([Name], [CreatedAt]) VALUES (@Name, @CreatedAt)
  </Insert>
  
  <Update id="Update">
    UPDATE [Users] SET [Name] = @Name WHERE [Id] = @Id
  </Update>
  
  <Delete id="Delete">
    DELETE FROM [Users] WHERE [Id] = @Id
  </Delete>
</SqlMap>
```

### Code Preview
- Live preview of generated code before writing to disk
- Syntax highlighting for C# and XML

### Batch Generation
- Multi-select tables from the table list
- Generate all selected files at once
- Configurable output directory

### Custom Namespaces
- Set global namespace for all generated code
- Override namespace per table

## Type Mapping

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

## Usage

1. **Select Provider**: Choose database type from dropdown
2. **Enter Connection String**: Input and test connection
3. **Load Tables**: Click "Load Tables" to discover schema
4. **Select Tables**: Check tables to generate code for
5. **Configure Options**: Set namespace, output directory
6. **Generate**: Click "Generate" to create files

## Building

```bash
# Build for .NET 10 (requires Windows)
dotnet build FastData.ModelGenerator.WinForms --framework net10.0-windows
```

## Dependencies

- FastData.Tooling
- FastUntility

## License

MIT License - see [LICENSE](../LICENSE) for details.
