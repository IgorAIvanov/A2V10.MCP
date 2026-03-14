// Copyright © 2026 Igor Ivanov. All rights reserved.


namespace A2v10.McpServer.Configuration
{
    /// <summary>
    /// Интерфейс сервиса для работы с конфигурацией приложения
    /// </summary>
    public interface IRemouteConfigurationService
    {
        /// <summary>
        /// Получить текущую конфигурацию
        /// </summary>
        AppConfiguration Configuration { get; }

        /// <summary>
        /// Загружена ли конфигурация
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Инициализация конфигурации из корневых каталогов проекта
        /// </summary>
        Task InitializeAsync(IEnumerable<string> rootPaths);

        /// <summary>
        /// Получить значение конфигурации по ключу
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);
    }
}
