# SearchDatabaseObjects - Usage Examples

## Common Use Cases

### 1. Database Discovery Workflow

#### Step 1: List all schemas
```csharp
// Find all available schemas in the database
objectType: "schema"
detailLevel: "summary"
```

#### Step 2: Explore tables in a schema
```csharp
// List all tables in 'dbo' schema with metadata
objectType: "table"
schema: "dbo"
detailLevel: "summary"
```

#### Step 3: Examine specific table structure
```csharp
// Get full details for a specific table
objectType: "table"
pattern: "Users"
schema: "dbo"
detailLevel: "full"
```

### 2. Finding Foreign Key Columns

```csharp
// Find all columns that likely contain foreign keys
objectType: "column"
pattern: "%_id"
detailLevel: "summary"
```

### 3. Exploring Stored Procedures

#### List all procedures
```csharp
objectType: "procedure"
detailLevel: "names"
limit: 50
```

#### Find procedures by naming convention
```csharp
// Find all procedures starting with 'sp_Get'
objectType: "procedure"
pattern: "sp_Get%"
detailLevel: "summary"
```

#### Get procedure definition
```csharp
// Get full definition of a specific procedure
objectType: "procedure"
pattern: "sp_GetUserById"
detailLevel: "full"
```

### 4. Index Analysis

#### Find all indexes on a table
```csharp
objectType: "index"
schema: "dbo"
table: "Users"
detailLevel: "full"
```

#### Find all unique indexes
```csharp
// Find all indexes (filter unique in code)
objectType: "index"
schema: "dbo"
detailLevel: "summary"
```

### 5. Schema Migration Planning

#### Compare table counts across schemas
```csharp
objectType: "schema"
detailLevel: "summary"
```

#### Find all tables needing migration
```csharp
objectType: "table"
pattern: "temp_%"
detailLevel: "summary"
```

### 6. Database Documentation

#### Document all tables in a schema
```csharp
objectType: "table"
schema: "dbo"
detailLevel: "full"
```

#### Document all procedures
```csharp
objectType: "procedure"
schema: "dbo"
detailLevel: "full"
```

### 7. Code Refactoring Support

#### Find all columns with specific name
```csharp
// Find all 'Status' columns across database
objectType: "column"
pattern: "Status"
detailLevel: "summary"
```

#### Find all functions for calculation
```csharp
objectType: "function"
pattern: "%calculate%"
detailLevel: "summary"
```

### 8. Performance Optimization

#### Find large tables
```csharp
// Get row counts for all tables
objectType: "table"
schema: "dbo"
detailLevel: "summary"
// Filter in code for tables with row_count > threshold
```

#### Review indexes on large tables
```csharp
objectType: "index"
schema: "dbo"
table: "LargeTable"
detailLevel: "full"
```

### 9. Security Audit

#### List all schemas (check access)
```csharp
objectType: "schema"
detailLevel: "summary"
```

#### Find sensitive tables
```csharp
objectType: "table"
pattern: "%password%"
detailLevel: "summary"
```

### 10. API Development

#### Find all tables for API endpoints
```csharp
objectType: "table"
schema: "api"
detailLevel: "full"
```

#### Find related procedures
```csharp
objectType: "procedure"
schema: "api"
pattern: "%User%"
detailLevel: "full"
```

## Pattern Matching Examples

### Exact Match
```csharp
pattern: "Users"  // Matches only "Users"
```

### Prefix Match
```csharp
pattern: "User%"  // Matches "Users", "UserRoles", "UserSettings"
```

### Suffix Match
```csharp
pattern: "%_log"  // Matches "error_log", "audit_log", "access_log"
```

### Contains Match
```csharp
pattern: "%order%"  // Matches "Orders", "CustomerOrders", "order_details"
```

### Single Character Wildcard
```csharp
pattern: "User_"  // Matches "Users", "User1", but not "UserRoles"
```

### Complex Patterns
```csharp
pattern: "tmp_%_2024"  // Matches "tmp_users_2024", "tmp_orders_2024"
```

## Progressive Detail Levels

### Workflow: Names → Summary → Full

#### 1. Quick scan with names
```csharp
objectType: "table"
schema: "dbo"
detailLevel: "names"
limit: 100
```

#### 2. Filter and get summary
```csharp
objectType: "table"
schema: "dbo"
pattern: "User%"
detailLevel: "summary"
```

#### 3. Deep dive with full details
```csharp
objectType: "table"
schema: "dbo"
pattern: "Users"
detailLevel: "full"
```

## Combining with Other Tools

### Example: Analyze table then query data

#### 1. Find table structure
```csharp
// SearchDatabaseObjects
objectType: "table"
pattern: "Users"
schema: "dbo"
detailLevel: "full"
```

#### 2. Query the data
```csharp
// ExecuteSQL
sql: "SELECT TOP 10 * FROM dbo.Users"
```

### Example: Find procedure then execute

#### 1. Find procedure
```csharp
// SearchDatabaseObjects
objectType: "procedure"
pattern: "sp_GetUserById"
detailLevel: "full"
```

#### 2. Execute procedure
```csharp
// ExecuteSQL
sql: "EXEC dbo.sp_GetUserById @userId = 123"
```

## Handling Large Result Sets

### Strategy 1: Use schema filter
```csharp
// Instead of searching entire database
objectType: "table"
pattern: "%"

// Narrow down by schema
objectType: "table"
schema: "dbo"
pattern: "%"
```

### Strategy 2: Increase limit progressively
```csharp
// Start small
limit: 10

// Then increase if needed
limit: 50
limit: 100
```

### Strategy 3: Use specific patterns
```csharp
// Instead of
pattern: "%user%"

// Use more specific
pattern: "User%"
```

### Strategy 4: Check truncated flag
```json
{
  "count": 100,
  "truncated": true  // More results available
}
```
If truncated is true, refine your search pattern or increase limit.

## Error Recovery

### Handle missing schema gracefully
```csharp
// First, list available schemas
objectType: "schema"
detailLevel: "names"

// Then use valid schema
objectType: "table"
schema: "dbo"  // Valid schema from previous result
```

### Validate object existence
```csharp
// Search for table first
objectType: "table"
pattern: "Users"
schema: "dbo"

// If count > 0, proceed with detailed query
```

## Best Practices

1. **Start broad, then narrow down**
   - Begin with schema-level exploration
   - Then drill down to specific objects

2. **Use appropriate detail levels**
   - `names` for exploration (fast, low token usage)
   - `summary` for decision making
   - `full` for implementation

3. **Leverage patterns effectively**
   - Use specific patterns when you know what you're looking for
   - Use wildcards for discovery

4. **Set reasonable limits**
   - Start with small limits for exploration
   - Increase only when needed

5. **Filter by schema when possible**
   - Reduces search scope
   - Improves performance
   - Makes results more relevant

6. **Check for errors**
   - Always check response for error field
   - Handle missing objects gracefully
   - Validate parameters before use

7. **Combine with other tools**
   - Use SearchDatabaseObjects for discovery
   - Use ExecuteSQL for data operations
   - Use together for complete workflows
