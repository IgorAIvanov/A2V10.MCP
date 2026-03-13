using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using A2v10.McpServer.Tools.DBTools;
using A2v10.McpServer.Tools.Helpers;

namespace A2v10.McpServer.Services
{
    public class DatabaseInitializationService : IHostedService
    {
        private readonly ILogger<DatabaseInitializationService> _logger;
        private readonly ModelContextProtocol.Server.McpServer _mcpServer;
        private readonly IConnector _connector;

        public DatabaseInitializationService(
            ILogger<DatabaseInitializationService> logger,
            ModelContextProtocol.Server.McpServer mcpServer,
            IConnector connector)
        {
            _logger = logger;
            _mcpServer = mcpServer;
            _connector = connector;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Получаем roots от MCP клиента
                var rootsResult = await _mcpServer.RequestRootsAsync(
                    new ModelContextProtocol.Protocol.ListRootsRequestParams(),
                    cancellationToken);

                if (rootsResult.Roots == null || !rootsResult.Roots.Any())
                {
                    _logger.LogWarning("No project roots received from MCP client");
                    return;
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
                    return;
                }

                // Фильтруем строки подключения, исключая ошибки
                var validConnectionStrings = connectionStrings
                    .Where(kvp => !kvp.Value.StartsWith("Ошибка"))
                    .ToList();

                if (!validConnectionStrings.Any())
                {
                    _logger.LogWarning("No valid connection strings found");
                    foreach (var error in connectionStrings)
                    {
                        _logger.LogWarning($"Error reading {error.Key}: {error.Value}");
                    }
                    return;
                }

                // Используем первую найденную валидную строку подключения
                var firstConnectionString = validConnectionStrings.First();
                _logger.LogInformation($"Found connection string in: {firstConnectionString.Key}");

                if (validConnectionStrings.Count > 1)
                {
                    _logger.LogInformation($"Multiple connection strings found ({validConnectionStrings.Count}), using the first one");
                }

                // Подключаемся к базе данных
                await _connector.ConnectAsync(firstConnectionString.Value);
                _logger.LogInformation("Successfully connected to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database connection");
                // Не прерываем запуск приложения, просто логируем ошибку
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping database connection...");
            return _connector.DisconnectAsync();
        }
    }
}
