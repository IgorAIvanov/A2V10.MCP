// Copyright © 2026 Igor Ivanov. All rights reserved.

using Microsoft.Extensions.Logging;
using A2v10.McpServer.Tools.Helpers;

namespace A2v10.McpServer.Configuration
{
    /// <summary>
    /// Сервис для централизованного управления конфигурацией приложения
    /// </summary>
    public class RemouteConfigurationService : IRemouteConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private AppConfiguration _configuration;

        public RemouteConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _configuration = new AppConfiguration();
        }

        public AppConfiguration Configuration => _configuration;

        public bool IsInitialized => _configuration.IsLoaded;

        public async Task InitializeAsync(IEnumerable<string> rootPaths)
        {
            if (_configuration.IsLoaded)
            {
                _logger.LogInformation("Configuration already loaded from: {Path}", _configuration.ConfigFilePath);
                return;
            }

            await _initializationLock.WaitAsync();
            try
            {
                if (_configuration.IsLoaded)
                    return;

                _logger.LogInformation("Starting configuration initialization...");

                // Загружаем полную конфигурацию
                var configResult = await ConfigHelper.LoadConfigurationAsync(rootPaths);

                if (configResult == null)
                {
                    var errorMsg = "Failed to load configuration from appSettings.json";
                    _logger.LogError(errorMsg);
                    _configuration = new AppConfiguration
                    {
                        IsLoaded = false,
                        ErrorMessage = errorMsg
                    };
                    throw new InvalidOperationException(errorMsg);
                }

                _configuration = configResult;
                _logger.LogInformation("Configuration successfully loaded from: {Path}", _configuration.ConfigFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize configuration");
                _configuration.ErrorMessage = ex.Message;
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            if (!_configuration.IsLoaded)
            {
                _logger.LogWarning("Attempting to get configuration value '{Key}' before configuration is loaded", key);
                return defaultValue;
            }

            if (_configuration.AdditionalSettings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T typedValue)
                        return typedValue;

                    // Попытка конвертации
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert configuration value '{Key}' to type {Type}", key, typeof(T));
                    return defaultValue;
                }
            }

            _logger.LogDebug("Configuration key '{Key}' not found, returning default value", key);
            return defaultValue;
        }
    }
}
