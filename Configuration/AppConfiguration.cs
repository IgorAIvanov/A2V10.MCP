using System.Collections.Generic;

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
        /// Строка подключения к базе данных
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Все строки подключения из секции ConnectionStrings
        /// </summary>
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();

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
    }
}
