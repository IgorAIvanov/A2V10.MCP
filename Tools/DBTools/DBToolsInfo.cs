

namespace A2v10.McpServer.Tools.DBTools
{
    public class SQLResult
    {
        public List<object>? Rows { get; set; }
        public int RowCount { get; set; }

        public SQLResult()
        {
            Rows = new List<object>();
            RowCount = 0;
        }

        public SQLResult(List<object>? rows, int rowCount)
        {
            Rows = rows ?? new List<object>();
            RowCount = rowCount;
        }
    }
    

    public class TableColumn
    {
        public string? ColumnName { get; set; }
        public string? DataType { get; set; }
        public string? IsNullable { get; set; }
        public string? ColumnDefault { get; set; }
        public string? Description { get; set; }

        public TableColumn() { }

        public TableColumn(string? columnName, string? dataType, string? isNullable, string? columnDefault, string? description)
        {
            ColumnName = columnName;
            DataType = dataType;
            IsNullable = isNullable;    
            ColumnDefault = columnDefault;
            Description = description;
        }
    }

    public class TableIndex
    {
        public string? IndexName { get; set; }
        public List<string>? ColumnNames { get; set; }
        public bool? IsUnique { get; set; }
        public bool? IsPrimary { get; set; }

        public TableIndex() { }

        public TableIndex(string? indexName, List<string>? columnNames, bool? isUnique, bool? isPrimary)
        {
            IndexName = indexName;
            ColumnNames = columnNames ?? new List<string>();
            IsUnique = isUnique;
            IsPrimary = isPrimary;
        }
    }

    public class StoredProcedure
    {
        public string? ProcedureName { get; set; }
        public string? ProcedureType { get; set; } // "procedure" | "function"
        public string? Language { get; set; }
        public string? ParameterList { get; set; }
        public string? ReturnType { get; set; }
        public string? Definition { get; set; }

        public StoredProcedure() { }

        public StoredProcedure(string? procedureName, string? procedureType, string? language, string? parameterList, string? returnType, string? definition)
        {
            ProcedureName = procedureName;
            ProcedureType = procedureType;
            Language = language;
            ParameterList = parameterList;
            ReturnType = returnType;
            Definition = definition;
        }
    }

    public static class DatabaseObjectTypes
    {
        public const string Schema = "schema";
        public const string Table = "table";
        public const string Column = "column";
        public const string Procedure = "procedure";
        public const string Function = "function";
        public const string Index = "index";
    }

    public static class DetailLevels
    {
        public const string Names = "names";
        public const string Summary = "summary";
        public const string Full = "full";
    }

    public class ExecuteOptions
    {
        /// <summary>Maximum number of rows to return (applied via database-native LIMIT)</summary>
        public int? MaxRows { get; set; }

        /// <summary>
        /// Restrict to read-only SQL operations (application-level enforcement)
        /// Validates SQL keywords before execution to prevent write operations.
        /// Note: SDK-level readonly enforcement is set via ConnectorConfig.readonly
        /// </summary>
        public bool? ReadOnly { get; set; }

        public ExecuteOptions() { }

        public ExecuteOptions(int? maxRows, bool? readOnly)
        {
            MaxRows = maxRows;
            ReadOnly = readOnly;
        }
}

public interface IConnector
    {
        /// <summary> A unique identifier for the connector </summary>
      //  ConnectorType Id { get; }

        /// <summary> Human-readable name of the connector </summary>
        //string Name { get; }

        /// <summary> Get the source ID for this connector instance (set by ConnectorManager) </summary>
       // string GetId();

        /// <summary> Connect to the database using DSN, with optional init script and database-specific configuration </summary>
        Task ConnectAsync(string connectionString);

        /// <summary> Close the connection </summary>
        Task DisconnectAsync();

        /// <summary> Get all schemas in the database </summary>
        Task<List<string>> GetSchemasAsync();

        /// <summary> Get all tables in the database or in a specific schema </summary>
        Task<List<string>> GetTablesAsync(string? schema = null);

        /// <summary> Get schema information for a specific table </summary>
        Task<List<TableColumn>> GetTableSchemaAsync(string tableName, string? schema = null);

        /// <summary> Check if a table exists </summary>
        Task<bool> TableExistsAsync(string tableName, string? schema = null);

        /// <summary> Get indexes for a specific table </summary>
        Task<List<TableIndex>> GetTableIndexesAsync(string tableName, string? schema = null);

        /// <summary> Get stored procedures/functions in the database or in a specific schema </summary>
        Task<List<string>> GetStoredProceduresAsync(string? schema = null, string? routineType = null);

        /// <summary> Get details for a specific stored procedure/function </summary>
        Task<StoredProcedure> GetStoredProcedureDetailAsync(string procedureName, string? schema = null);

        /// <summary> Get estimated row count for a table using database statistics </summary>
        Task<int?> GetTableRowCountAsync(string tableName, string? schema = null);

        /// <summary> Get the comment/description for a table </summary>
        Task<string?> GetTableCommentAsync(string tableName, string? schema = null);

        /// <summary> Execute a SQL query with execution options and optional parameters </summary>
        Task<SQLResult> ExecuteSQLAsync(string sql, ExecuteOptions options, object[]? parameters = null);
    }
}
