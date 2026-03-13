# SearchDatabaseObjects Tool

## Description
Unified tool for searching and listing database objects (schemas, tables, columns, procedures, functions, indexes) with flexible filtering and detail levels.

## Parameters

### objectType (required)
Type of database object to search:
- `schema` - Database schemas
- `table` - Tables
- `column` - Columns within tables
- `procedure` - Stored procedures
- `function` - User-defined functions
- `index` - Indexes on tables

### pattern (optional, default: "%")
SQL LIKE pattern for filtering results:
- `%` - Matches any sequence of characters
- `_` - Matches a single character
- Examples:
  - `User%` - Finds objects starting with "User"
  - `%_id` - Finds objects ending with "_id"
  - `tmp_%` - Finds temporary objects

### schema (optional)
Filter results to a specific schema name.

### table (optional)
Filter results to a specific table name.
- **Note:** Requires `schema` parameter
- Only applicable for `column` and `index` object types

### detailLevel (optional, default: "names")
Level of detail in results:
- `names` - Minimal: just object names (uses fewest tokens)
- `summary` - Metadata: names + brief information (row count, column count, etc.)
- `full` - Complete: full structure details (columns, indexes, definitions)

### limit (optional, default: 100, max: 1000)
Maximum number of results to return.

## Examples

### 1. List all schemas
```
objectType: "schema"
```

### 2. Find all tables starting with "User"
```
objectType: "table"
pattern: "User%"
detailLevel: "summary"
```

### 3. Find all columns named "id" in schema "dbo"
```
objectType: "column"
pattern: "id"
schema: "dbo"
detailLevel: "full"
```

### 4. Find all stored procedures in schema "app"
```
objectType: "procedure"
schema: "app"
detailLevel: "summary"
```

### 5. Find indexes on specific table
```
objectType: "index"
schema: "dbo"
table: "Users"
detailLevel: "full"
```

### 6. Search for functions with "calculate" in name
```
objectType: "function"
pattern: "%calculate%"
detailLevel: "summary"
```

## Response Structure

### Schema Results (names level)
```json
{
  "object_type": "schema",
  "pattern": "%",
  "count": 3,
  "results": [
    { "name": "dbo" },
    { "name": "app" },
    { "name": "auth" }
  ],
  "truncated": false
}
```

### Schema Results (summary/full level)
```json
{
  "object_type": "schema",
  "pattern": "%",
  "count": 3,
  "results": [
    { "name": "dbo", "table_count": 15 },
    { "name": "app", "table_count": 8 },
    { "name": "auth", "table_count": 3 }
  ],
  "truncated": false
}
```

### Table Results (names level)
```json
{
  "object_type": "table",
  "pattern": "User%",
  "count": 2,
  "results": [
    { "name": "Users", "schema": "dbo" },
    { "name": "UserRoles", "schema": "dbo" }
  ],
  "truncated": false
}
```

### Table Results (summary level)
```json
{
  "object_type": "table",
  "pattern": "User%",
  "count": 2,
  "results": [
    {
      "name": "Users",
      "schema": "dbo",
      "column_count": 8,
      "row_count": 1250,
      "comment": "User accounts table"
    },
    {
      "name": "UserRoles",
      "schema": "dbo",
      "column_count": 4,
      "row_count": 523
    }
  ],
  "truncated": false
}
```

### Table Results (full level)
```json
{
  "object_type": "table",
  "pattern": "Users",
  "count": 1,
  "results": [
    {
      "name": "Users",
      "schema": "dbo",
      "column_count": 8,
      "row_count": 1250,
      "comment": "User accounts table",
      "columns": [
        {
          "name": "Id",
          "type": "int",
          "nullable": false,
          "default": null
        },
        {
          "name": "UserName",
          "type": "nvarchar",
          "nullable": false,
          "default": null,
          "description": "Unique username"
        }
      ],
      "indexes": [
        {
          "name": "PK_Users",
          "columns": ["Id"],
          "unique": true,
          "primary": true
        },
        {
          "name": "IX_Users_UserName",
          "columns": ["UserName"],
          "unique": true,
          "primary": false
        }
      ]
    }
  ],
  "truncated": false
}
```

### Column Results (names level)
```json
{
  "object_type": "column",
  "pattern": "%_id",
  "schema": "dbo",
  "count": 5,
  "results": [
    { "name": "user_id", "table": "Orders", "schema": "dbo" },
    { "name": "product_id", "table": "Orders", "schema": "dbo" },
    { "name": "category_id", "table": "Products", "schema": "dbo" }
  ],
  "truncated": false
}
```

### Column Results (summary/full level)
```json
{
  "object_type": "column",
  "pattern": "%_id",
  "schema": "dbo",
  "count": 3,
  "results": [
    {
      "name": "user_id",
      "table": "Orders",
      "schema": "dbo",
      "type": "int",
      "nullable": false,
      "default": null
    },
    {
      "name": "product_id",
      "table": "Orders",
      "schema": "dbo",
      "type": "int",
      "nullable": false,
      "default": null
    }
  ],
  "truncated": false
}
```

### Procedure/Function Results (names level)
```json
{
  "object_type": "procedure",
  "pattern": "sp_%",
  "schema": "dbo",
  "count": 2,
  "results": [
    { "name": "sp_GetUserById", "schema": "dbo" },
    { "name": "sp_CreateUser", "schema": "dbo" }
  ],
  "truncated": false
}
```

### Procedure/Function Results (summary level)
```json
{
  "object_type": "procedure",
  "pattern": "sp_%",
  "schema": "dbo",
  "count": 2,
  "results": [
    {
      "name": "sp_GetUserById",
      "schema": "dbo",
      "type": "procedure",
      "language": "sql",
      "return_type": null
    }
  ],
  "truncated": false
}
```

### Procedure/Function Results (full level)
```json
{
  "object_type": "procedure",
  "pattern": "sp_%",
  "schema": "dbo",
  "count": 1,
  "results": [
    {
      "name": "sp_GetUserById",
      "schema": "dbo",
      "type": "procedure",
      "language": "sql",
      "parameters": "@userId IN int",
      "return_type": null,
      "definition": "CREATE PROCEDURE sp_GetUserById @userId INT AS BEGIN..."
    }
  ],
  "truncated": false
}
```

### Index Results (names level)
```json
{
  "object_type": "index",
  "pattern": "IX_%",
  "schema": "dbo",
  "count": 3,
  "results": [
    { "name": "IX_Users_Email", "table": "Users", "schema": "dbo" },
    { "name": "IX_Orders_Date", "table": "Orders", "schema": "dbo" }
  ],
  "truncated": false
}
```

### Index Results (summary/full level)
```json
{
  "object_type": "index",
  "pattern": "IX_%",
  "schema": "dbo",
  "count": 2,
  "results": [
    {
      "name": "IX_Users_Email",
      "table": "Users",
      "schema": "dbo",
      "columns": ["Email"],
      "unique": true,
      "primary": false
    },
    {
      "name": "IX_Orders_Date",
      "table": "Orders",
      "schema": "dbo",
      "columns": ["OrderDate", "UserId"],
      "unique": false,
      "primary": false
    }
  ],
  "truncated": false
}
```

## Error Handling

### Invalid object type
```json
{
  "error": "Invalid object type: xyz. Valid types: schema, table, column, procedure, function, index",
  "type": "ArgumentException"
}
```

### Table parameter without schema
```json
{
  "error": "The 'table' parameter requires 'schema' to be specified",
  "type": "ArgumentException"
}
```

### Invalid table filter
```json
{
  "error": "The 'table' parameter only applies to object_type 'column' or 'index', not 'procedure'",
  "type": "ArgumentException"
}
```

### Schema not found
```json
{
  "error": "Schema 'xyz' does not exist. Available schemas: dbo, app, auth",
  "type": "InvalidOperationException"
}
```

## Performance Tips

1. **Use appropriate detail levels:**
   - Use `names` for quick exploration (minimal token usage)
   - Use `summary` for metadata without full definitions
   - Use `full` only when you need complete details

2. **Filter by schema:**
   - Always specify `schema` when you know it to reduce search scope

3. **Use specific patterns:**
   - Specific patterns like `User%` are faster than `%User%`
   - Avoid patterns that start with `%` when possible

4. **Set reasonable limits:**
   - Use smaller limits (e.g., 10-50) for initial exploration
   - Increase limit only when needed

5. **Progressive refinement:**
   - Start with `names` level to see what's available
   - Then query specific objects with `full` level for details
