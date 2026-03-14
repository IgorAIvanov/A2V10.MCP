// Copyright © 2026 Igor Ivanov. All rights reserved.
using System.Collections.Generic;

namespace A2v10.McpServer.Configuration
{
    /// <summary>
    /// Интерфейс для настроек сервиса
    /// </summary>
    public interface IServiceOptions
    {
        /// <summary>
        /// Режим только для чтения
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Максимальное количество строк
        /// </summary>
        int MaxRows { get; set; }

        /// <summary>
        /// Получить все настройки
        /// </summary>
        IReadOnlyDictionary<string, object> Options { get; }

        /// <summary>
        /// Устанавливает значение по ключу
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Получает значение по ключу
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Проверяет наличие ключа
        /// </summary>
        bool ContainsKey(string key);

        /// <summary>
        /// Удаляет значение по ключу
        /// </summary>
        bool Remove(string key);
    }
}
