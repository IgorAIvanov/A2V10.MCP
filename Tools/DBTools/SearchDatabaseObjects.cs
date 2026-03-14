// Copyright © 2026 Igor Ivanov. All rights reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace A2v10.McpServer.Tools.DBTools
{
    /// <summary>
    /// Object types that can be searched
    /// </summary>
    public enum DatabaseObjectType
    {
        Schema,
        Table,
        Column,
        Procedure,
        Function,
        Index
    }

    /// <summary>
    /// Detail level for search results
    /// - Names: Just object names (minimal tokens)
    /// - Summary: Names + brief metadata (row count, column count, etc.)
    /// - Full: Complete structure details
    /// </summary>
    public enum DetailLevel
    {
        Names,
        Summary,
        Full
    }

    /// <summary>
    /// Arguments for searching database objects
    /// </summary>
    public class SearchDatabaseObjectsArgs
    {
        public DatabaseObjectType ObjectType { get; set; }
        public string Pattern { get; set; } = "%";
        public string? Schema { get; set; }
        public string? Table { get; set; }
        public DetailLevel DetailLevel { get; set; } = DetailLevel.Names;
        public int Limit { get; set; } = 100;
    }

    /// <summary>
    /// Result of database object search
    /// </summary>
    public class SearchDatabaseObjectsResult
    {
        public string ObjectType { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public string? Table { get; set; }
        public string DetailLevel { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<object> Results { get; set; } = new List<object>();
        public bool Truncated { get; set; }
    }

    [McpServerToolType]
    public class SearchDatabaseObjects
    {
        private readonly IConnector _connector;

        public SearchDatabaseObjects(IConnector connector)
        {
            _connector = connector;
        }

        [McpServerTool, Description("Search and list database objects (schemas, tables, columns, procedures, functions, indexes) with flexible filtering and detail levels.")]
        public async Task<string> SearchDBObjects(
            [Description("Object type to search: Schema, Table, Column, Procedure, Function, Index")]
            string objectType,
            [Description("LIKE pattern (% = any chars, _ = one char). Default: %")]
            string? pattern = null,
            [Description("Filter to schema")]
            string? schema = null,
            [Description("Filter to table (requires schema; column/index only)")]
            string? table = null,
            [Description("Detail level: Names (minimal), Summary (metadata), Full (all). Default: Names")]
            string? detailLevel = null,
            [Description("Max results (default: 100, max: 1000)")]
            int? limit = null
        )
        {
            try
            {
                // Parse arguments
                var args = new SearchDatabaseObjectsArgs
                {
                    ObjectType = ParseObjectType(objectType),
                    Pattern = pattern ?? "%",
                    Schema = schema,
                    Table = table,
                    DetailLevel = ParseDetailLevel(detailLevel ?? "names"),
                    Limit = Math.Min(limit ?? 100, 1000)
                };

                // Validate arguments
                ValidateArguments(args);

                // Execute search
                var results = await SearchObjectsAsync(args);

                // Build result
                var result = new SearchDatabaseObjectsResult
                {
                    ObjectType = args.ObjectType.ToString().ToLower(),
                    Pattern = args.Pattern,
                    Schema = args.Schema,
                    Table = args.Table,
                    DetailLevel = args.DetailLevel.ToString().ToLower(),
                    Count = results.Count,
                    Results = results,
                    Truncated = results.Count == args.Limit
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    type = ex.GetType().Name
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        private DatabaseObjectType ParseObjectType(string objectType)
        {
            return objectType.ToLower() switch
            {
                "schema" => DatabaseObjectType.Schema,
                "table" => DatabaseObjectType.Table,
                "column" => DatabaseObjectType.Column,
                "procedure" => DatabaseObjectType.Procedure,
                "function" => DatabaseObjectType.Function,
                "index" => DatabaseObjectType.Index,
                _ => throw new ArgumentException($"Invalid object type: {objectType}. Valid types: schema, table, column, procedure, function, index")
            };
        }

        private DetailLevel ParseDetailLevel(string detailLevel)
        {
            return detailLevel.ToLower() switch
            {
                "names" => DetailLevel.Names,
                "summary" => DetailLevel.Summary,
                "full" => DetailLevel.Full,
                _ => throw new ArgumentException($"Invalid detail level: {detailLevel}. Valid levels: names, summary, full")
            };
        }

        private void ValidateArguments(SearchDatabaseObjectsArgs args)
        {
            // Validate table parameter
            if (!string.IsNullOrWhiteSpace(args.Table))
            {
                if (string.IsNullOrWhiteSpace(args.Schema))
                {
                    throw new ArgumentException("The 'table' parameter requires 'schema' to be specified");
                }
                if (args.ObjectType != DatabaseObjectType.Column && args.ObjectType != DatabaseObjectType.Index)
                {
                    throw new ArgumentException($"The 'table' parameter only applies to object_type 'column' or 'index', not '{args.ObjectType.ToString().ToLower()}'");
                }
            }
        }

        private async Task<List<object>> SearchObjectsAsync(SearchDatabaseObjectsArgs args)
        {
            // Validate schema if provided
            if (!string.IsNullOrWhiteSpace(args.Schema))
            {
                var schemas = await _connector.GetSchemasAsync();
                if (!schemas.Contains(args.Schema))
                {
                    throw new InvalidOperationException($"Schema '{args.Schema}' does not exist. Available schemas: {string.Join(", ", schemas)}");
                }
            }

            // Route to appropriate search function
            return args.ObjectType switch
            {
                DatabaseObjectType.Schema => await SearchSchemasAsync(args),
                DatabaseObjectType.Table => await SearchTablesAsync(args),
                DatabaseObjectType.Column => await SearchColumnsAsync(args),
                DatabaseObjectType.Procedure => await SearchProceduresAsync(args, "procedure"),
                DatabaseObjectType.Function => await SearchProceduresAsync(args, "function"),
                DatabaseObjectType.Index => await SearchIndexesAsync(args),
                _ => throw new NotSupportedException($"Unsupported object type: {args.ObjectType}")
            };
        }

        /// <summary>
        /// Convert SQL LIKE pattern to JavaScript-style regex
        /// Supports % (any chars) and _ (single char)
        /// </summary>
        private Regex LikePatternToRegex(string pattern)
        {
            // Escape special regex characters except % and _
            var escaped = Regex.Escape(pattern)
                .Replace("%", ".*")
                .Replace("_", ".");

            return new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Get row count estimate for a table
        /// </summary>
        private async Task<int?> GetTableRowCountAsync(string tableName, string? schemaName)
        {
            try
            {
                return await _connector.GetTableRowCountAsync(tableName, schemaName);
            }
            catch
            {
                // If we can't get row count, return null (not critical)
                return null;
            }
        }

        /// <summary>
        /// Get table comment from the connector if supported
        /// </summary>
        private async Task<string?> GetTableCommentAsync(string tableName, string? schemaName)
        {
            try
            {
                return await _connector.GetTableCommentAsync(tableName, schemaName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Search for schemas
        /// </summary>
        private async Task<List<object>> SearchSchemasAsync(SearchDatabaseObjectsArgs args)
        {
            var schemas = await _connector.GetSchemasAsync();
            var regex = LikePatternToRegex(args.Pattern);
            var matched = schemas.Where(s => regex.IsMatch(s)).Take(args.Limit).ToList();

            if (args.DetailLevel == DetailLevel.Names)
            {
                return matched.Select(name => (object)new { name }).ToList();
            }

            // For summary and full, add table count
            var results = new List<object>();
            foreach (var schemaName in matched)
            {
                try
                {
                    var tables = await _connector.GetTablesAsync(schemaName);
                    results.Add(new
                    {
                        name = schemaName,
                        table_count = tables.Count
                    });
                }
                catch
                {
                    results.Add(new
                    {
                        name = schemaName,
                        table_count = 0
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search for tables
        /// </summary>
        private async Task<List<object>> SearchTablesAsync(SearchDatabaseObjectsArgs args)
        {
            var regex = LikePatternToRegex(args.Pattern);
            var results = new List<object>();

            // Get schemas to search
            var schemasToSearch = string.IsNullOrWhiteSpace(args.Schema)
                ? await _connector.GetSchemasAsync()
                : new List<string> { args.Schema };

            // Search tables in each schema
            foreach (var schemaName in schemasToSearch)
            {
                if (results.Count >= args.Limit) break;

                try
                {
                    var tables = await _connector.GetTablesAsync(schemaName);
                    var matched = tables.Where(t => regex.IsMatch(t)).ToList();

                    foreach (var tableName in matched)
                    {
                        if (results.Count >= args.Limit) break;

                        if (args.DetailLevel == DetailLevel.Names)
                        {
                            results.Add(new
                            {
                                name = tableName,
                                schema = schemaName
                            });
                        }
                        else if (args.DetailLevel == DetailLevel.Summary)
                        {
                            // Get column count and table comment for summary
                            try
                            {
                                var columns = await _connector.GetTableSchemaAsync(tableName, schemaName);
                                var rowCount = await GetTableRowCountAsync(tableName, schemaName);
                                var comment = await GetTableCommentAsync(tableName, schemaName);

                                var result = new Dictionary<string, object?>
                                {
                                    ["name"] = tableName,
                                    ["schema"] = schemaName,
                                    ["column_count"] = columns.Count,
                                    ["row_count"] = rowCount
                                };

                                if (!string.IsNullOrEmpty(comment))
                                {
                                    result["comment"] = comment;
                                }

                                results.Add(result);
                            }
                            catch
                            {
                                results.Add(new
                                {
                                    name = tableName,
                                    schema = schemaName,
                                    column_count = (int?)null,
                                    row_count = (int?)null
                                });
                            }
                        }
                        else // Full detail
                        {
                            try
                            {
                                var columns = await _connector.GetTableSchemaAsync(tableName, schemaName);
                                var indexes = await _connector.GetTableIndexesAsync(tableName, schemaName);
                                var rowCount = await GetTableRowCountAsync(tableName, schemaName);
                                var comment = await GetTableCommentAsync(tableName, schemaName);

                                var result = new Dictionary<string, object?>
                                {
                                    ["name"] = tableName,
                                    ["schema"] = schemaName,
                                    ["column_count"] = columns.Count,
                                    ["row_count"] = rowCount
                                };

                                if (!string.IsNullOrEmpty(comment))
                                {
                                    result["comment"] = comment;
                                }

                                result["columns"] = columns.Select(col =>
                                {
                                    var colDict = new Dictionary<string, object?>
                                    {
                                        ["name"] = col.ColumnName,
                                        ["type"] = col.DataType,
                                        ["nullable"] = col.IsNullable == "YES",
                                        ["default"] = col.ColumnDefault
                                    };

                                    if (!string.IsNullOrEmpty(col.Description))
                                    {
                                        colDict["description"] = col.Description;
                                    }

                                    return colDict;
                                }).ToList();

                                result["indexes"] = indexes.Select(idx => new
                                {
                                    name = idx.IndexName,
                                    columns = idx.ColumnNames,
                                    unique = idx.IsUnique,
                                    primary = idx.IsPrimary
                                }).ToList();

                                results.Add(result);
                            }
                            catch (Exception ex)
                            {
                                results.Add(new
                                {
                                    name = tableName,
                                    schema = schemaName,
                                    error = $"Unable to fetch full details: {ex.Message}"
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Skip schemas we can't access
                    continue;
                }
            }

            return results;
        }

        /// <summary>
        /// Search for columns
        /// </summary>
        private async Task<List<object>> SearchColumnsAsync(SearchDatabaseObjectsArgs args)
        {
            var regex = LikePatternToRegex(args.Pattern);
            var results = new List<object>();

            // Get schemas to search
            var schemasToSearch = string.IsNullOrWhiteSpace(args.Schema)
                ? await _connector.GetSchemasAsync()
                : new List<string> { args.Schema };

            // Search columns in tables across schemas
            foreach (var schemaName in schemasToSearch)
            {
                if (results.Count >= args.Limit) break;

                try
                {
                    // Get tables to search
                    var tablesToSearch = !string.IsNullOrWhiteSpace(args.Table)
                        ? new List<string> { args.Table }
                        : await _connector.GetTablesAsync(schemaName);

                    foreach (var tableName in tablesToSearch)
                    {
                        if (results.Count >= args.Limit) break;

                        try
                        {
                            var columns = await _connector.GetTableSchemaAsync(tableName, schemaName);
                            var matchedColumns = columns.Where(col => regex.IsMatch(col.ColumnName ?? string.Empty)).ToList();

                            foreach (var column in matchedColumns)
                            {
                                if (results.Count >= args.Limit) break;

                                if (args.DetailLevel == DetailLevel.Names)
                                {
                                    results.Add(new
                                    {
                                        name = column.ColumnName,
                                        table = tableName,
                                        schema = schemaName
                                    });
                                }
                                else
                                {
                                    // Summary and full are the same for columns
                                    var result = new Dictionary<string, object?>
                                    {
                                        ["name"] = column.ColumnName,
                                        ["table"] = tableName,
                                        ["schema"] = schemaName,
                                        ["type"] = column.DataType,
                                        ["nullable"] = column.IsNullable == "YES",
                                        ["default"] = column.ColumnDefault
                                    };

                                    if (!string.IsNullOrEmpty(column.Description))
                                    {
                                        result["description"] = column.Description;
                                    }

                                    results.Add(result);
                                }
                            }
                        }
                        catch
                        {
                            // Skip tables we can't access
                            continue;
                        }
                    }
                }
                catch
                {
                    // Skip schemas we can't access
                    continue;
                }
            }

            return results;
        }

        /// <summary>
        /// Search for stored procedures and/or functions
        /// </summary>
        private async Task<List<object>> SearchProceduresAsync(SearchDatabaseObjectsArgs args, string? routineType = null)
        {
            var regex = LikePatternToRegex(args.Pattern);
            var results = new List<object>();

            // Get schemas to search
            var schemasToSearch = string.IsNullOrWhiteSpace(args.Schema)
                ? await _connector.GetSchemasAsync()
                : new List<string> { args.Schema };

            // Search procedures/functions in each schema
            foreach (var schemaName in schemasToSearch)
            {
                if (results.Count >= args.Limit) break;

                try
                {
                    var procedures = await _connector.GetStoredProceduresAsync(schemaName, routineType);
                    var matched = procedures.Where(p => regex.IsMatch(p)).ToList();

                    foreach (var procName in matched)
                    {
                        if (results.Count >= args.Limit) break;

                        if (args.DetailLevel == DetailLevel.Names)
                        {
                            results.Add(new
                            {
                                name = procName,
                                schema = schemaName
                            });
                        }
                        else
                        {
                            // Summary and full - get procedure details
                            try
                            {
                                var details = await _connector.GetStoredProcedureDetailAsync(procName, schemaName);
                                var result = new Dictionary<string, object?>
                                {
                                    ["name"] = procName,
                                    ["schema"] = schemaName,
                                    ["type"] = details.ProcedureType,
                                    ["language"] = details.Language
                                };

                                if (args.DetailLevel == DetailLevel.Full)
                                {
                                    result["parameters"] = details.ParameterList;
                                    result["definition"] = details.Definition;
                                }

                                result["return_type"] = details.ReturnType;

                                results.Add(result);
                            }
                            catch (Exception ex)
                            {
                                results.Add(new
                                {
                                    name = procName,
                                    schema = schemaName,
                                    error = $"Unable to fetch details: {ex.Message}"
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Skip schemas we can't access or databases that don't support procedures
                    continue;
                }
            }

            return results;
        }

        /// <summary>
        /// Search for indexes
        /// </summary>
        private async Task<List<object>> SearchIndexesAsync(SearchDatabaseObjectsArgs args)
        {
            var regex = LikePatternToRegex(args.Pattern);
            var results = new List<object>();

            // Get schemas to search
            var schemasToSearch = string.IsNullOrWhiteSpace(args.Schema)
                ? await _connector.GetSchemasAsync()
                : new List<string> { args.Schema };

            // Search indexes in tables across schemas
            foreach (var schemaName in schemasToSearch)
            {
                if (results.Count >= args.Limit) break;

                try
                {
                    // Get tables to search
                    var tablesToSearch = !string.IsNullOrWhiteSpace(args.Table)
                        ? new List<string> { args.Table }
                        : await _connector.GetTablesAsync(schemaName);

                    foreach (var tableName in tablesToSearch)
                    {
                        if (results.Count >= args.Limit) break;

                        try
                        {
                            var indexes = await _connector.GetTableIndexesAsync(tableName, schemaName);
                            var matchedIndexes = indexes.Where(idx => regex.IsMatch(idx.IndexName ?? string.Empty)).ToList();

                            foreach (var index in matchedIndexes)
                            {
                                if (results.Count >= args.Limit) break;

                                if (args.DetailLevel == DetailLevel.Names)
                                {
                                    results.Add(new
                                    {
                                        name = index.IndexName,
                                        table = tableName,
                                        schema = schemaName
                                    });
                                }
                                else
                                {
                                    // Summary and full are the same for indexes
                                    results.Add(new
                                    {
                                        name = index.IndexName,
                                        table = tableName,
                                        schema = schemaName,
                                        columns = index.ColumnNames,
                                        unique = index.IsUnique,
                                        primary = index.IsPrimary
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Skip tables we can't access
                            continue;
                        }
                    }
                }
                catch
                {
                    // Skip schemas we can't access
                    continue;
                }
            }

            return results;
        }
    }
}
