// Copyright © 2026 Igor Ivanov. All rights reserved.
using System.Collections.Generic;

namespace A2v10.McpServer.Configuration
{
    /// <summary>
    /// Класс для хранения настроек сервиса в формате ключ-значение
    /// </summary>
    public class ServiceOptions : IServiceOptions
    {
        private readonly Dictionary<string, object> _options = new();

        /// <summary>
        /// Режим только для чтения
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Максимальное количество строк
        /// </summary>
        public int MaxRows { get; set; }

        /// <summary>
        /// Получить все настройки
        /// </summary>
        public IReadOnlyDictionary<string, object> Options => _options;

        /// <summary>
        /// Устанавливает значение по ключу
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (value != null)
            {
                _options[key] = value;
            }
        }

        /// <summary>
        /// Получает значение по ключу
        /// </summary>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            if (!_options.TryGetValue(key, out var value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Проверяет наличие ключа
        /// </summary>
        public bool ContainsKey(string key) => _options.ContainsKey(key);

        /// <summary>
        /// Удаляет значение по ключу
        /// </summary>
        public bool Remove(string key) => _options.Remove(key);
    }
}
