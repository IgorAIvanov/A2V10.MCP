using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using A2v10.McpServer.Tools.Helpers;

namespace A2v10.McpServer.Tools.DBTools
{
    /// <summary>
    /// Обертка для IConnector с ленивой инициализацией подключения к базе данных.
    /// Подключение происходит при первом обращении к базе данных.
    /// </summary>
    public class LazyConnector : IConnector
    {
        private readonly ILogger<LazyConnector> _logger;
        private readonly ModelContextProtocol.Server.McpServer _mcpServer;
        private readonly IConnector _innerConnector;
        private bool _isInitialized;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public LazyConnector(
            ILogger<LazyConnector> logger,
            ModelContextProtocol.Server.McpServer mcpServer,
            IConnector innerConnector)
        {
            _logger = logger;
            _mcpServer = mcpServer;
            _innerConnector = innerConnector;
            _isInitialized = false;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                _logger.LogInformation("Starting lazy database initialization...");

                // Получаем roots от MCP клиента
                var rootsResult = await _mcpServer.RequestRootsAsync(
                    new ListRootsRequestParams(),
                    CancellationToken.None);

                if (rootsResult.Roots == null || !rootsResult.Roots.Any())
                {
                    _logger.LogWarning("No project roots received from MCP client");
                    throw new InvalidOperationException("No project roots found. Cannot initialize database connection.");
                }

                // Извлекаем пути из URI
                var rootPaths = rootsResult.Roots
                    .Select(r => new Uri(r.Uri).LocalPath)
                    .ToList();

                _logger.LogInformation($"Found {rootPaths.Count} project root(s): {string.Join(", ", rootPaths)}");

                // Ищем строки подключения в appSettings.json
                var connectionStrings = await ConfigHelper.FindConnectionStringsAsync(rootPaths);

                if (!connectionStrings.Any())
                {
                    _logger.LogWarning("No connection strings found in appSettings.json files");
                    throw new InvalidOperationException("No connection strings found in appSettings.json files.");
                }

                // Фильтруем строки подключения, исключая ошибки
                var validConnectionStrings = connectionStrings
                    .Where(kvp => !kvp.Value.StartsWith("Ошибка"))
                    .ToList();

                if (!validConnectionStrings.Any())
                {
                    _logger.LogWarning("No valid connection strings found");
                    var errorStr = string.Join(", ", connectionStrings.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                    foreach (var error in connectionStrings)
                    {
                        _logger.LogWarning($"Error reading {error.Key}: {error.Value}");
                    }
                    throw new InvalidOperationException("No valid connection strings found. " + string.Join(", ", rootPaths) + ". Errors: " + errorStr);
                }

                // Используем первую найденную валидную строку подключения
                var firstConnectionString = validConnectionStrings.First();
                _logger.LogInformation($"Found connection string in: {firstConnectionString.Key}");

                if (validConnectionStrings.Count > 1)
                {
                    _logger.LogInformation($"Multiple connection strings found ({validConnectionStrings.Count}), using the first one");
                }

                // Подключаемся к базе данных
                await _innerConnector.ConnectAsync(firstConnectionString.Value);
                _logger.LogInformation("Successfully connected to database");

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database connection");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task ConnectAsync(string connectionString)
        {
            await _initializationLock.WaitAsync();
            try
            {
                await _innerConnector.ConnectAsync(connectionString);
                _isInitialized = true;
                _logger.LogInformation("Connected to database using explicit connection string");
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _innerConnector.DisconnectAsync();
            _isInitialized = false;
        }

        public async Task<List<string>> GetSchemasAsync()
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetSchemasAsync();
        }

        public async Task<List<string>> GetTablesAsync(string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetTablesAsync(schema);
        }

        public async Task<List<TableColumn>> GetTableSchemaAsync(string tableName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetTableSchemaAsync(tableName, schema);
        }

        public async Task<bool> TableExistsAsync(string tableName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.TableExistsAsync(tableName, schema);
        }

        public async Task<List<TableIndex>> GetTableIndexesAsync(string tableName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetTableIndexesAsync(tableName, schema);
        }

        public async Task<List<string>> GetStoredProceduresAsync(string? schema = null, string? routineType = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetStoredProceduresAsync(schema, routineType);
        }

        public async Task<StoredProcedure> GetStoredProcedureDetailAsync(string procedureName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetStoredProcedureDetailAsync(procedureName, schema);
        }

        public async Task<int?> GetTableRowCountAsync(string tableName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetTableRowCountAsync(tableName, schema);
        }

        public async Task<string?> GetTableCommentAsync(string tableName, string? schema = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.GetTableCommentAsync(tableName, schema);
        }

        public async Task<SQLResult> ExecuteSQLAsync(string sql, ExecuteOptions options, object[]? parameters = null)
        {
            await EnsureInitializedAsync();
            return await _innerConnector.ExecuteSQLAsync(sql, options, parameters);
        }
    }
}
