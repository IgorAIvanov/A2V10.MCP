# Использование конфигурации приложения

После рефакторинга конфигурация загружается централизованно и предоставляет удобные методы для доступа к настройкам.

## Основные концепции

1. **ConfigHelper** - загружает `appSettings.json` в объект `AppConfiguration`
2. **AppConfiguration** - хранит все настройки в `AdditionalSettings` и предоставляет методы доступа
3. **IConfigurationService** - сервис для работы с конфигурацией через DI

## Структура appSettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyDb;...",
    "Analytics": "Server=localhost;Database=Analytics;..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "CustomSettings": {
    "MaxRetries": 3,
    "Timeout": 30
  }
}
```

## Способы чтения конфигурации

### 1. Через IConfigurationService (рекомендуется)

```csharp
public class MyService
{
    private readonly IConfigurationService _configService;

    public MyService(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task DoSomethingAsync()
    {
        // Получить строку подключения (приоритет: "Default", затем первая)
        var connectionString = _configService.Configuration.GetConnectionString();
        
        // Получить конкретную строку подключения
        var analyticsConnection = _configService.Configuration.GetConnectionString("Analytics");
        
        // Получить значение настройки верхнего уровня
        var timeout = _configService.Configuration.GetValue("Timeout", 30);
        
        // Получить вложенное значение
        var logLevel = _configService.Configuration.GetNestedValue<string>(
            "Logging", "LogLevel", "Information");
        
        // Использовать метод сервиса (альтернатива)
        var maxRetries = _configService.GetValue("MaxRetries", 5);
    }
}
```

### 2. Напрямую через AppConfiguration

```csharp
var config = _configService.Configuration;

// Проверка загрузки
if (!config.IsLoaded)
{
    throw new InvalidOperationException($"Configuration error: {config.ErrorMessage}");
}

// Получить строку подключения
string? connStr = config.GetConnectionString();
string? specificConnStr = config.GetConnectionString("MyDatabase");

// Получить простые значения
int maxRetries = config.GetValue("MaxRetries", 3);
string appName = config.GetValue<string>("AppName", "MyApp");

// Получить вложенные значения
var logLevel = config.GetNestedValue<string>("Logging", "LogLevel", "Information");

// Доступ к сырым данным (если нужно)
if (config.AdditionalSettings.TryGetValue("CustomSettings", out var customObj))
{
    if (customObj is Dictionary<string, object> custom)
    {
        // работа с вложенным объектом
    }
}
```

### 3. Примеры использования в разных сценариях

#### Инициализация базы данных

```csharp
public class DatabaseInitializer
{
    private readonly IConfigurationService _configService;
    
    public async Task InitializeAsync()
    {
        var config = _configService.Configuration;
        
        // Получаем строку подключения
        var connectionString = config.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No connection string found");
        }
        
        // Получаем дополнительные настройки БД
        var commandTimeout = config.GetNestedValue<int>("Database", "CommandTimeout", 30);
        var poolSize = config.GetNestedValue<int>("Database", "PoolSize", 100);
        
        // Инициализация...
    }
}
```

#### Чтение настроек приложения

```csharp
public class ApplicationSettings
{
    private readonly AppConfiguration _config;
    
    public ApplicationSettings(IConfigurationService configService)
    {
        _config = configService.Configuration;
    }
    
    public int MaxConcurrentRequests => _config.GetValue("MaxConcurrentRequests", 10);
    public TimeSpan RequestTimeout => TimeSpan.FromSeconds(
        _config.GetValue("RequestTimeoutSeconds", 30));
    public bool EnableCaching => _config.GetValue("EnableCaching", true);
}
```

#### Множественные строки подключения

```csharp
public class MultiDatabaseService
{
    private readonly AppConfiguration _config;
    
    public async Task MigrateDataAsync()
    {
        // Основная БД
        var mainDb = _config.GetConnectionString("Default");
        
        // БД аналитики
        var analyticsDb = _config.GetConnectionString("Analytics");
        
        // БД логов
        var logsDb = _config.GetConnectionString("Logs");
        
        // Миграция данных...
    }
}
```

## Регистрация в DI

Конфигурация уже зарегистрирована в `Program.cs`:

```csharp
// Регистрация сервиса конфигурации
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
```

Для использования просто инжектируйте `IConfigurationService` в конструктор:

```csharp
public class MyTool
{
    public MyTool(IConfigurationService configService)
    {
        // Сервис доступен
    }
}
```

## Типичные паттерны

### Паттерн 1: Проверка перед использованием

```csharp
var config = _configService.Configuration;
if (!config.IsLoaded)
{
    _logger.LogError("Configuration not loaded: {Error}", config.ErrorMessage);
    throw new InvalidOperationException("Configuration is not loaded");
}

var value = config.GetValue("MySetting", defaultValue);
```

### Паттерн 2: Безопасное получение с fallback

```csharp
// Пытаемся получить из конфигурации, иначе используем значение по умолчанию
var timeout = _configService.Configuration.GetValue("Timeout", 30);
var connectionString = _configService.Configuration.GetConnectionString() 
    ?? throw new InvalidOperationException("Connection string is required");
```

### Паттерн 3: Кэширование настроек

```csharp
public class CachedSettings
{
    private readonly Lazy<int> _maxRetries;
    private readonly Lazy<string?> _connectionString;
    
    public CachedSettings(IConfigurationService configService)
    {
        var config = configService.Configuration;
        _maxRetries = new Lazy<int>(() => config.GetValue("MaxRetries", 3));
        _connectionString = new Lazy<string?>(() => config.GetConnectionString());
    }
    
    public int MaxRetries => _maxRetries.Value;
    public string? ConnectionString => _connectionString.Value;
}
```

## Миграция старого кода

### Было:
```csharp
var connectionString = config.ConnectionString;
var allConnections = config.ConnectionStrings;
```

### Стало:
```csharp
var connectionString = config.GetConnectionString();
var specificConnection = config.GetConnectionString("MyDatabase");
```

## Best Practices

1. ✅ **Всегда проверяйте `IsLoaded`** перед использованием конфигурации
2. ✅ **Используйте значения по умолчанию** в `GetValue()` для устойчивости
3. ✅ **Инжектируйте `IConfigurationService`**, а не `AppConfiguration` напрямую
4. ✅ **Логируйте отсутствующие настройки** для упрощения отладки
5. ❌ **Не изменяйте `AdditionalSettings` напрямую** - это источник истины
6. ❌ **Не кэшируйте `AppConfiguration`** в статических полях

## Расширение функциональности

Если нужно добавить специфичные методы для вашего приложения:

```csharp
// Создайте extension methods
public static class AppConfigurationExtensions
{
    public static DatabaseSettings GetDatabaseSettings(this AppConfiguration config)
    {
        return new DatabaseSettings
        {
            ConnectionString = config.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string required"),
            CommandTimeout = config.GetNestedValue<int>("Database", "CommandTimeout", 30),
            MaxPoolSize = config.GetNestedValue<int>("Database", "MaxPoolSize", 100)
        };
    }
}

// Использование
var dbSettings = config.GetDatabaseSettings();
```
