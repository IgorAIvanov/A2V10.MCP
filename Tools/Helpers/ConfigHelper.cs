// Copyright © 2026 Igor Ivanov. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A2v10.McpServer.Configuration;

namespace A2v10.McpServer.Tools.Helpers
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Загружает полную конфигурацию из appSettings.json в указанных корневых каталогах.
        /// </summary>
        /// <param name="rootPaths">Список путей к корневым каталогам</param>
        /// <returns>Объект конфигурации или null, если не найдена</returns>
        public static async Task<AppConfiguration?> LoadConfigurationAsync(IEnumerable<string> rootPaths)
        {
            foreach (var root in rootPaths)
            {
                if (!Directory.Exists(root))
                    continue;

                var appSettingsPath = FindAppSettingsFile(root);
                if (appSettingsPath == null)
                    continue;

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

                    var config = new AppConfiguration
                    {
                        ConfigFilePath = appSettingsPath,
                        IsLoaded = true
                    };

                    // Загружаем все настройки
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        config.AdditionalSettings[property.Name] = JsonElementToObject(property.Value);
                    }

                    return config;
                }
                catch (Exception ex)
                {
                    return new AppConfiguration
                    {
                        ConfigFilePath = appSettingsPath,
                        IsLoaded = false,
                        ErrorMessage = $"Ошибка чтения: {ex.Message}"
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Ищет файл appSettings.json в каталоге или его подкаталогах.
        /// </summary>
        private static string? FindAppSettingsFile(string rootPath)
        {
            // Сначала ищем в корне
            var appSettingsFiles = Directory.GetFiles(rootPath, "appSettings.json", SearchOption.TopDirectoryOnly);
            if (appSettingsFiles.Length > 0)
                return appSettingsFiles[0];

            // Затем в подкаталогах первого уровня
            var subdirectories = Directory.GetDirectories(rootPath);
            foreach (var subdir in subdirectories)
            {
                appSettingsFiles = Directory.GetFiles(subdir, "appSettings.json", SearchOption.TopDirectoryOnly);
                if (appSettingsFiles.Length > 0)
                    return appSettingsFiles[0];
            }

            return null;
        }

        /// <summary>
        /// Конвертирует JsonElement в объект C#.
        /// </summary>
        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(JsonElementToObject)
                    .ToArray(),
                _ => element.GetRawText()
            };
        }

            }
        }
