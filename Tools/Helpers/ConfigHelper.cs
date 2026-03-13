using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace A2v10.McpServer.Tools.Helpers
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Ищет файлы appSettings.json в указанных корневых каталогах и возвращает найденные строки соединения.
        /// </summary>
        /// <param name="rootPaths">Список путей к корневым каталогам</param>
        /// <returns>Словарь: путь к appSettings.json -> строка соединения (если найдена)</returns>
        public static async Task<Dictionary<string, string>> FindConnectionStringsAsync(IEnumerable<string> rootPaths)
        {
            var result = new Dictionary<string, string>();
            foreach (var root in rootPaths)
            {
                var appSettingsPath = Path.Combine(root, "appSettings.json");
                if (!File.Exists(appSettingsPath))
                    continue;

                try
                {
                    using var stream = File.OpenRead(appSettingsPath);
                    using var doc = await JsonDocument.ParseAsync(stream);
                    var rootElement = doc.RootElement;

                    // Ищем строку соединения по пути "ConnectionStrings" -> "Default"
                    if (rootElement.TryGetProperty("ConnectionStrings", out var connStringsProp) &&
                        connStringsProp.TryGetProperty("Default", out var defaultConnProp))
                    {
                        result[appSettingsPath] = defaultConnProp.GetString();
                    }
                }
                catch (Exception ex)
                {
                    result[appSettingsPath] = $"Ошибка чтения: {ex.Message}";
                }
            }
            return result;
        }
    }
}
