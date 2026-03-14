using System.Collections.Generic;
using System.Linq;

namespace A2v10.McpServer.Configuration
{
    /// <summary>
    /// Модель конфигурации приложения из appSettings.json
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Путь к файлу appSettings.json, из которого была загружена конфигурация
        /// </summary>
        public string ConfigFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Дополнительные параметры конфигурации (можно расширять по необходимости)
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new();

        /// <summary>
        /// Признак успешной загрузки конфигурации
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// Сообщение об ошибке при загрузке (если есть)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Получает строку подключения из конфигурации.
        /// Приоритет: "Default", затем первая найденная.
        /// </summary>
        /// <param name="name">Имя строки подключения. Если null, используется приоритет.</param>
        /// <returns>Строка подключения или null, если не найдена</returns>
        public string? GetConnectionString(string? name = null)
        {
            if (!AdditionalSettings.TryGetValue("ConnectionStrings", out var connStringsObj))
                return null;

            if (connStringsObj is not Dictionary<string, object> connStrings)
                return null;

            // Если указано конкретное имя, ищем его
            if (!string.IsNullOrEmpty(name))
            {
                return connStrings.TryGetValue(name, out var value) ? value?.ToString() : null;
            }

            // Приоритет: "Default", затем первая найденная
            if (connStrings.TryGetValue("Default", out var defaultConn))
                return defaultConn?.ToString();

            return connStrings.Values.FirstOrDefault()?.ToString();
        }
    }
}
