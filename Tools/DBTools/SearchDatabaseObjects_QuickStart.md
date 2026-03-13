# SearchDatabaseObjects - Quick Start Guide

## Overview

`SearchDatabaseObjects` is a powerful MCP tool for discovering and exploring database objects with flexible filtering and detail levels. It provides a unified interface for searching schemas, tables, columns, procedures, functions, and indexes.

## Features

✅ Search multiple object types: schemas, tables, columns, procedures, functions, indexes  
✅ Flexible pattern matching with SQL LIKE syntax (%, _)  
✅ Three detail levels: names, summary, full  
✅ Schema and table filtering  
✅ Configurable result limits (max 1000)  
✅ Automatic row count estimation (using SQL Server statistics)  
✅ Table and column descriptions support  

## Installation

The tool is automatically registered via the `[McpServerToolType]` attribute when the application starts. No manual registration is required.

## Dependencies

- `IConnector` - Database connector interface
- `ModelContextProtocol.Server` - MCP Server framework
- `System.Text.Json` - JSON serialization
- `System.Text.RegularExpressions` - Pattern matching

## Usage

### Basic Search

```csharp
var searchTool = new SearchDatabaseObjects(connector);

// Search for all tables in 'dbo' schema
var result = await searchTool.Execute(
    objectType: "table",
    schema: "dbo",
    detailLevel: "summary"
);
```

### Pattern Matching

```csharp
// Find all tables starting with "User"
var result = await searchTool.Execute(
    objectType: "table",
    pattern: "User%",
    detailLevel: "full"
);

// Find all columns ending with "_id"
var result = await searchTool.Execute(
    objectType: "column",
    pattern: "%_id",
    detailLevel: "summary"
);
```

### Advanced Filtering

```csharp
// Find all indexes on a specific table
var result = await searchTool.Execute(
    objectType: "index",
    schema: "dbo",
    table: "Users",
    detailLevel: "full"
);

// Search procedures with limit
var result = await searchTool.Execute(
    objectType: "procedure",
    schema: "dbo",
    pattern: "sp_%",
    limit: 50
);
```

## Response Format

The tool returns a JSON string with the following structure:

```json
{
  "object_type": "table",
  "pattern": "User%",
  "schema": "dbo",
  "detail_level": "summary",
  "count": 3,
  "results": [
    {
      "name": "Users",
      "schema": "dbo",
      "column_count": 8,
      "row_count": 1250,
      "comment": "User accounts table"
    },
    ...
  ],
  "truncated": false
}
```

## Error Handling

Errors are returned as JSON:

```json
{
  "error": "Schema 'xyz' does not exist. Available schemas: dbo, app, auth",
  "type": "InvalidOperationException"
}
```

## Performance Considerations

### Row Count Estimation

The tool uses SQL Server's system tables (`sys.partitions`) for fast row count estimation instead of `COUNT(*)`, which avoids full table scans.

```sql
SELECT SUM(p.rows) as row_count
FROM sys.partitions p
INNER JOIN sys.tables t ON p.object_id = t.object_id
WHERE p.index_id IN (0, 1)
```

### Pattern Matching

Pattern matching is done in-memory using compiled regex patterns converted from SQL LIKE patterns:
- `%` → `.*` (any characters)
- `_` → `.` (single character)

### Detail Levels

Choose the appropriate detail level to minimize token usage:
- **names**: Fastest, minimal data (just object names)
- **summary**: Moderate, includes metadata
- **full**: Slowest, complete details (columns, indexes, definitions)

## Integration Example

```csharp
// In Program.cs or startup configuration
builder.Services.AddSingleton<IConnector>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<LazyConnector>>();
    var mcpServer = provider.GetRequiredService<ModelContextProtocol.Server.McpServer>();
    var innerConnector = new SQLServerConnector();
    return new LazyConnector(logger, mcpServer, innerConnector);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(); // Automatically discovers SearchDatabaseObjects
```

## Common Workflows

### 1. Database Discovery
```
1. List schemas → 2. List tables in schema → 3. Get table details
```

### 2. Code Generation
```
1. Find table → 2. Get full schema → 3. Generate code from columns
```

### 3. Migration Planning
```
1. Compare table counts → 2. Find tables by pattern → 3. Analyze structures
```

### 4. Performance Analysis
```
1. Find large tables (summary) → 2. Review indexes (full) → 3. Optimize
```

## Validation Rules

| Rule | Description |
|------|-------------|
| Table requires schema | When `table` is specified, `schema` must also be provided |
| Table applies to column/index only | `table` parameter only valid for column and index searches |
| Schema must exist | If specified, schema is validated against available schemas |
| Limit maximum | Maximum limit is 1000, default is 100 |
| Valid object types | Must be one of: schema, table, column, procedure, function, index |
| Valid detail levels | Must be one of: names, summary, full |

## Troubleshooting

### Problem: "Schema 'xyz' does not exist"
**Solution**: First list available schemas without filter, then use a valid schema name.

### Problem: "The 'table' parameter requires 'schema' to be specified"
**Solution**: Always provide schema when filtering by table.

### Problem: Results are truncated
**Solution**: Check the `truncated` flag. If true, refine your pattern or increase limit.

### Problem: Slow performance
**Solution**: 
- Use more specific patterns (avoid leading wildcards)
- Filter by schema when possible
- Use lower detail levels for exploration
- Reduce the limit

## API Reference

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| objectType | string | Yes | - | Type of object to search |
| pattern | string | No | "%" | SQL LIKE pattern for filtering |
| schema | string | No | null | Filter to specific schema |
| table | string | No | null | Filter to specific table (column/index only) |
| detailLevel | string | No | "names" | Level of detail in results |
| limit | int | No | 100 | Maximum number of results (max 1000) |

### Object Types

- `schema` - Database schemas
- `table` - Tables
- `column` - Table columns
- `procedure` - Stored procedures
- `function` - User-defined functions
- `index` - Table indexes

### Detail Levels

- `names` - Just object names (minimal)
- `summary` - Names + metadata (row count, column count, etc.)
- `full` - Complete details (columns, indexes, definitions)

## See Also

- [Complete README](SearchDatabaseObjects_README.md) - Detailed documentation
- [Usage Examples](SearchDatabaseObjects_Examples.md) - Common use cases and patterns
