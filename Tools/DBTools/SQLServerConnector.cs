using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;

namespace A2v10.McpServer.Tools.DBTools
{
    public class SQLServerConnector : IConnector
    {
        private SqlConnection? _connection;

        public async Task ConnectAsync(string connectionString)
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    return;
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            _connection = new SqlConnection(connectionString);
            await _connection.OpenAsync();
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                if (_connection.State != ConnectionState.Closed)
                    await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task<List<string>> GetSchemasAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT SCHEMA_NAME
                    FROM INFORMATION_SCHEMA.SCHEMATA
                    ORDER BY SCHEMA_NAME";
                var schemas = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader.GetString(0));
                }
                return schemas;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get schemas: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetTablesAsync(string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema
                    ORDER BY TABLE_NAME";
                command.Parameters.AddWithValue("@schema", schemaToUse);

                var tables = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
                return tables;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get tables: {ex.Message}", ex);
            }
        }

        public async Task<List<TableColumn>> GetTableSchemaAsync(string tableName, string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT c.COLUMN_NAME as    column_name,
                           c.DATA_TYPE as      data_type,
                           c.IS_NULLABLE as    is_nullable,
                           c.COLUMN_DEFAULT as column_default,
                           ep.value as         description
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN sys.columns sc
                      ON sc.name = c.COLUMN_NAME
                      AND sc.object_id = OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME))
                    LEFT JOIN sys.extended_properties ep
                      ON ep.major_id = sc.object_id
                      AND ep.minor_id = sc.column_id
                      AND ep.name = 'MS_Description'
                    WHERE c.TABLE_NAME = @tableName
                      AND c.TABLE_SCHEMA = @schema
                    ORDER BY c.ORDINAL_POSITION
                ";
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schema", schemaToUse);

                var columns = new List<TableColumn>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new TableColumn
                    {
                        ColumnName = reader["column_name"] as string ?? string.Empty,
                        DataType = reader["data_type"] as string ?? string.Empty,
                        IsNullable = reader["is_nullable"] as string ?? string.Empty,
                        ColumnDefault = reader["column_default"] == DBNull.Value ? null : reader["column_default"]?.ToString(),
                        Description = string.IsNullOrEmpty(reader["description"]?.ToString()) ? null : reader["description"]?.ToString()
                    });
                }
                return columns;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get schema for table {tableName}: {ex.Message}", ex);
            }
        }
        

        public async Task<bool> TableExistsAsync(string tableName, string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) as count
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = @tableName
                      AND TABLE_SCHEMA = @schema";
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schema", schemaToUse);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to check if table exists: {ex.Message}", ex);
            }       
        }

        public async Task<List<TableIndex>> GetTableIndexesAsync(string tableName, string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT i.name AS index_name,
                           i.is_unique,
                           i.is_primary_key,
                           c.name AS column_name,
                           ic.key_ordinal
                    FROM sys.indexes i
                             INNER JOIN
                         sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                             INNER JOIN
                         sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                             INNER JOIN
                         sys.tables t ON i.object_id = t.object_id
                             INNER JOIN
                         sys.schemas s ON t.schema_id = s.schema_id
                    WHERE t.name = @tableName
                      AND s.name = @schema
                    ORDER BY i.name,
                             ic.key_ordinal
                ";
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schema", schemaToUse);

                var indexMap = new Dictionary<string, (List<string> Columns, bool IsUnique, bool IsPrimary)>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var indexName = reader.GetString(0);
                    var isUnique = reader.GetBoolean(1);        
                    var isPrimary = reader.GetBoolean(2);
                    var columnName = reader.GetString(3);
                    // key_ordinal is at reader.GetInt32(4) but not used for grouping

                    if (!indexMap.ContainsKey(indexName))
                    {
                        indexMap[indexName] = (new List<string>(), isUnique, isPrimary);
                    }
                    indexMap[indexName].Columns.Add(columnName);
                }

                var indexes = new List<TableIndex>();
                foreach (var kvp in indexMap)
                {
                    indexes.Add(new TableIndex
                    {
                        IndexName = kvp.Key,
                        ColumnNames = kvp.Value.Columns,
                        IsUnique = kvp.Value.IsUnique,
                        IsPrimary = kvp.Value.IsPrimary
                    });
                }
                return indexes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get indexes for table {tableName}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetStoredProceduresAsync(string? schema = null, string? routineType = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                string typeFilter;
                if (string.Equals(routineType, "function", StringComparison.OrdinalIgnoreCase))
                    typeFilter = "AND ROUTINE_TYPE = 'FUNCTION'";
                else if (string.Equals(routineType, "procedure", StringComparison.OrdinalIgnoreCase))
                    typeFilter = "AND ROUTINE_TYPE = 'PROCEDURE'";
                else
                    typeFilter = "AND (ROUTINE_TYPE = 'PROCEDURE' OR ROUTINE_TYPE = 'FUNCTION')";

                var query = $@"
                    SELECT ROUTINE_NAME
                    FROM INFORMATION_SCHEMA.ROUTINES
                    WHERE ROUTINE_SCHEMA = @schema
                      {typeFilter}
                    ORDER BY ROUTINE_NAME
                ";

                using var command = _connection.CreateCommand();
                command.CommandText = query;
                command.Parameters.AddWithValue("@schema", schemaToUse);

                var routines = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    routines.Add(reader["ROUTINE_NAME"] as string ?? string.Empty);
                }
                return routines;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get stored procedures: {ex.Message}", ex);
            }
        }

        public async Task<StoredProcedure> GetStoredProcedureDetailAsync(string procedureName, string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;

                // 1. Get basic procedure information
                using var routineCommand = _connection.CreateCommand();
                routineCommand.CommandText = @"
                    SELECT ROUTINE_NAME as procedure_name,
                           ROUTINE_TYPE,
                           DATA_TYPE as return_data_type
                    FROM INFORMATION_SCHEMA.ROUTINES
                    WHERE ROUTINE_NAME = @procedureName
                      AND ROUTINE_SCHEMA = @schema
                ";
                routineCommand.Parameters.AddWithValue("@procedureName", procedureName);
                routineCommand.Parameters.AddWithValue("@schema", schemaToUse);

                using var routineReader = await routineCommand.ExecuteReaderAsync();
                if (!await routineReader.ReadAsync())
                    throw new Exception($"Stored procedure '{procedureName}' not found in schema '{schemaToUse}'");

                var routineName = routineReader["procedure_name"]?.ToString() ?? string.Empty;
                var routineType = routineReader["ROUTINE_TYPE"]?.ToString() ?? string.Empty;
                var returnDataType = routineReader["return_data_type"]?.ToString();
                routineReader.Close();

                // 2. Get parameter information
                using var paramCommand = _connection.CreateCommand();
                paramCommand.CommandText = @"
                    SELECT PARAMETER_NAME,
                           PARAMETER_MODE,
                           DATA_TYPE,
                           CHARACTER_MAXIMUM_LENGTH,
                           ORDINAL_POSITION
                    FROM INFORMATION_SCHEMA.PARAMETERS
                    WHERE SPECIFIC_NAME = @procedureName
                      AND SPECIFIC_SCHEMA = @schema
                    ORDER BY ORDINAL_POSITION
                ";
                paramCommand.Parameters.AddWithValue("@procedureName", procedureName);
                paramCommand.Parameters.AddWithValue("@schema", schemaToUse);

                var parameterList = new List<string>();
                using (var paramReader = await paramCommand.ExecuteReaderAsync())
                {
                    while (await paramReader.ReadAsync())
                    {
                        var paramName = paramReader["PARAMETER_NAME"]?.ToString() ?? string.Empty;
                        var paramMode = paramReader["PARAMETER_MODE"]?.ToString() ?? string.Empty;
                        var dataType = paramReader["DATA_TYPE"]?.ToString() ?? string.Empty;
                        var charMaxLenObj = paramReader["CHARACTER_MAXIMUM_LENGTH"];
                        var lengthStr = (charMaxLenObj != DBNull.Value && Convert.ToInt32(charMaxLenObj) > 0)
                            ? $"({charMaxLenObj})" : string.Empty;
                        parameterList.Add($"{paramName} {paramMode} {dataType}{lengthStr}");
                    }
                }
                var parameterListStr = string.Join(", ", parameterList);

                // 3. Get the procedure definition
                using var defCommand = _connection.CreateCommand();
                defCommand.CommandText = @"
                    SELECT definition
                    FROM sys.sql_modules sm
                             JOIN sys.objects o ON sm.object_id = o.object_id
                             JOIN sys.schemas s ON o.schema_id = s.schema_id
                    WHERE o.name = @procedureName
                      AND s.name = @schema
                ";
                defCommand.Parameters.AddWithValue("@procedureName", procedureName);
                defCommand.Parameters.AddWithValue("@schema", schemaToUse);

                string? definition = null;
                using (var defReader = await defCommand.ExecuteReaderAsync())
                {
                    if (await defReader.ReadAsync())
                        definition = defReader["definition"]?.ToString();
                }

                return new StoredProcedure
                {
                    ProcedureName = routineName,
                    ProcedureType = routineType == "PROCEDURE" ? "procedure" : "function",
                    Language = "sql",
                    ParameterList = parameterListStr,
                    ReturnType = routineType == "FUNCTION" ? returnDataType : null,
                    Definition = definition
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get stored procedure details: {ex.Message}", ex);
            }
        }

        public async Task<int?> GetTableRowCountAsync(string tableName, string? schema = null)
        {
            throw new NotImplementedException();
        }

        public async Task<string?> GetTableCommentAsync(string tableName, string? schema = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            try
            {
                var schemaToUse = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT ep.value as table_comment
                    FROM sys.extended_properties ep
                    JOIN sys.tables t ON ep.major_id = t.object_id
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE ep.minor_id = 0
                      AND ep.name = 'MS_Description'
                      AND t.name = @tableName
                      AND s.name = @schema
                ";
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schema", schemaToUse);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var comment = reader["table_comment"];
                    return comment == DBNull.Value ? null : comment?.ToString();
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<SQLResult> ExecuteSQLAsync(string sql, ExecuteOptions options, object[]? parameters = null)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Not connected to SQL Server database");

            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentNullException(nameof(sql), "SQL query cannot be null or empty.");

            // ReadOnly enforcement: разрешаем только SELECT
            if (options?.ReadOnly == true)
            {
                var sqlTrim = sql.TrimStart();
                if (!sqlTrim.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only SELECT statements are allowed in read-only mode.");
            }

            // Добавить TOP N для SELECT, если задано MaxRows
            string finalSql = sql;
            if (options?.MaxRows is int maxRows && maxRows > 0)
            {
                var sqlTrim = sql.TrimStart();
                if (sqlTrim.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    // Вставить TOP N после SELECT (с учетом SELECT DISTINCT)
                    int selectIdx = sqlTrim.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                    int afterSelect = selectIdx + 6;
                    string rest = sqlTrim.Substring(afterSelect).TrimStart();
                    if (rest.StartsWith("DISTINCT", StringComparison.OrdinalIgnoreCase))
                    {
                        finalSql = sqlTrim.Insert(afterSelect + 8, $" TOP {maxRows}");
                    }
                    else
                    {
                        finalSql = sqlTrim.Insert(afterSelect, $" TOP {maxRows}");
                    }
                }
            }

            using var command = _connection.CreateCommand();
            command.CommandText = finalSql;

            // Добавить параметры
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
                }
            }

            var result = new SQLResult();
            using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object?>>();
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
                rowCount++;
            }
            result.Rows = rows.Cast<object>().ToList();
            result.RowCount = rowCount;
            return result;
        }
    }
}
