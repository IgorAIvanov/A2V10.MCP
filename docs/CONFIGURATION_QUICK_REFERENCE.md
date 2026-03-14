# Шпаргалка по работе с конфигурацией

## Быстрый старт

### Инжектирование в конструктор
```csharp
public class MyClass
{
    private readonly IConfigurationService _configService;
    
    public MyClass(IConfigurationService configService)
    {
        _configService = configService;
    }
}
```

### Основные операции

| Задача | Код |
|--------|-----|
| Получить строку подключения (Default) | `config.GetConnectionString()` |
| Получить именованную строку подключения | `config.GetConnectionString("Analytics")` |
| Получить значение настройки | `config.GetValue("MaxRetries", 3)` |
| Получить вложенное значение | `config.GetNestedValue<string>("Logging", "LogLevel", "Info")` |
| Проверить загрузку | `if (config.IsLoaded) { ... }` |
| Получить путь к файлу конфигурации | `config.ConfigFilePath` |

## Примеры кода

### 1. Подключение к БД
```csharp
var connStr = _configService.Configuration.GetConnectionString();
if (string.IsNullOrEmpty(connStr))
    throw new InvalidOperationException("No connection string");
await connector.ConnectAsync(connStr);
```

### 2. Чтение настроек приложения
```csharp
var config = _configService.Configuration;
var timeout = config.GetValue("TimeoutSeconds", 30);
var maxRetries = config.GetValue("MaxRetries", 3);
var enableCache = config.GetValue("EnableCache", true);
```

### 3. Работа с секциями
```csharp
// appSettings.json:
// "Database": { "PoolSize": 100, "Timeout": 30 }

var poolSize = config.GetNestedValue<int>("Database", "PoolSize", 50);
var timeout = config.GetNestedValue<int>("Database", "Timeout", 30);
```

## Структура appSettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=...;",
    "Analytics": "Server=...;Database=..."
  },
  "Logging": {
    "LogLevel": { "Default": "Information" }
  },
  "CustomSetting": "value",
  "NestedSettings": {
    "Key1": "value1",
    "Key2": 42
  }
}
```

## Проверка ошибок

```csharp
var config = _configService.Configuration;

// Вариант 1: Проверка IsLoaded
if (!config.IsLoaded)
{
    _logger.LogError("Config error: {Error}", config.ErrorMessage);
    return;
}

// Вариант 2: Проверка конкретного значения
var connStr = config.GetConnectionString();
if (string.IsNullOrEmpty(connStr))
{
    throw new InvalidOperationException("Connection string not found");
}
```

## API Reference

### AppConfiguration

| Метод | Описание | Пример |
|-------|----------|--------|
| `GetConnectionString(name?)` | Получить строку подключения | `GetConnectionString("Default")` |
| `GetValue<T>(key, default?)` | Получить значение по ключу | `GetValue("Timeout", 30)` |
| `GetNestedValue<T>(section, key, default?)` | Получить вложенное значение | `GetNestedValue<int>("DB", "Pool", 50)` |

### IConfigurationService

| Свойство/Метод | Описание |
|----------------|----------|
| `Configuration` | Текущая конфигурация (AppConfiguration) |
| `IsInitialized` | Проверка инициализации |
| `InitializeAsync(roots)` | Инициализация (вызывается автоматически) |
| `GetValue<T>(key, default?)` | Получить значение (делегирует в Configuration) |

## Типичные ошибки

❌ **НЕ делайте так:**
```csharp
// Не обращайтесь к несуществующим свойствам
var connStr = config.ConnectionString; // УДАЛЕНО!
var allConns = config.ConnectionStrings; // УДАЛЕНО!

// Не изменяйте AdditionalSettings напрямую
config.AdditionalSettings["Key"] = "value"; // НЕТ!
```

✅ **Делайте так:**
```csharp
var connStr = config.GetConnectionString();
var specificConn = config.GetConnectionString("MyDB");
var value = config.GetValue("Key", defaultValue);
```
