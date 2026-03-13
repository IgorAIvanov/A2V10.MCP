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
                if (!Directory.Exists(root))
                    continue;

                var appSettingsFiles = Directory.GetFiles(root, "appSettings.json", SearchOption.TopDirectoryOnly);

                if (appSettingsFiles.Length == 0)
                {
                    var subdirectories = Directory.GetDirectories(root);
                    foreach (var subdir in subdirectories)
                    {
                        appSettingsFiles = Directory.GetFiles(subdir, "appSettings.json", SearchOption.TopDirectoryOnly);
                        if (appSettingsFiles.Length > 0)
                            break;
                    }
                }

                if (appSettingsFiles.Length == 0)
                    continue;

                var appSettingsPath = appSettingsFiles[0];

                try
                {
                    using var stream = File.OpenRead(appSettingsPath);
                    var options = new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    using var doc = await JsonDocument.ParseAsync(stream, options);
                    var rootElement = doc.RootElement;

                    // Ищем строку соединения по пути "ConnectionStrings" -> "Default"
                    if (rootElement.TryGetProperty("ConnectionStrings", out var connStringsProp) &&
                        connStringsProp.TryGetProperty("Default", out var defaultConnProp))
                    {
                        var connectionString = defaultConnProp.GetString();
                        if (!string.IsNullOrEmpty(connectionString))
                        {
                            result[appSettingsPath] = connectionString;
                        }
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
