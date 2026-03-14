# Configuration Usage Examples

## Simple Examples

### Example 1: Get database connection string
```csharp
using A2v10.McpServer.Configuration;

public class DatabaseTool
{
    private readonly IConfigurationService _configService;
    
    public DatabaseTool(IConfigurationService configService)
    {
        _configService = configService;
    }
    
    public async Task ConnectAsync()
    {
        // Get default connection string
        var connectionString = _configService.Configuration.GetConnectionString();
        
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string not found in appSettings.json");
        
        // Use connection string...
        await _database.ConnectAsync(connectionString);
    }
}
```

### Example 2: Get named connection string
```csharp
// appSettings.json:
// {
//   "ConnectionStrings": {
//     "Default": "Server=...;Database=Main;",
//     "Analytics": "Server=...;Database=Analytics;"
//   }
// }

var mainDb = config.GetConnectionString();              // Gets "Default"
var analyticsDb = config.GetConnectionString("Analytics");
```

### Example 3: Get configuration values
```csharp
// appSettings.json:
// {
//   "MaxRetries": 3,
//   "TimeoutSeconds": 30,
//   "EnableFeatureX": true
// }

var config = _configService.Configuration;

int maxRetries = config.GetValue("MaxRetries", 5);         // Returns 3
int timeout = config.GetValue("TimeoutSeconds", 60);      // Returns 30
bool featureX = config.GetValue("EnableFeatureX", false);  // Returns true
string unknown = config.GetValue<string>("Unknown", "default"); // Returns "default"
```

### Example 4: Get nested configuration values
```csharp
// appSettings.json:
// {
//   "Database": {
//     "CommandTimeout": 30,
//     "MaxPoolSize": 100,
//     "EnableRetry": true
//   }
// }

var config = _configService.Configuration;

int commandTimeout = config.GetNestedValue<int>("Database", "CommandTimeout", 15);
int poolSize = config.GetNestedValue<int>("Database", "MaxPoolSize", 50);
bool enableRetry = config.GetNestedValue<bool>("Database", "EnableRetry", false);
```

### Example 5: Check if configuration is loaded
```csharp
public async Task<string> GetDataAsync()
{
    var config = _configService.Configuration;
    
    if (!config.IsLoaded)
    {
        _logger.LogError("Configuration error: {Error}", config.ErrorMessage);
        throw new InvalidOperationException("Configuration not loaded");
    }
    
    var connectionString = config.GetConnectionString();
    // ... use configuration
}
```

## Complete Working Example

```csharp
using Microsoft.Extensions.Logging;
using A2v10.McpServer.Configuration;

public class UserService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<UserService> _logger;
    
    public UserService(
        IConfigurationService configService,
        ILogger<UserService> logger)
    {
        _configService = configService;
        _logger = logger;
    }
    
    public async Task<User> GetUserAsync(int userId)
    {
        var config = _configService.Configuration;
        
        // Check if configuration is loaded
        if (!config.IsLoaded)
        {
            _logger.LogError("Configuration not loaded: {Error}", config.ErrorMessage);
            throw new InvalidOperationException("Configuration error");
        }
        
        // Get database connection
        var connectionString = config.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found");
        }
        
        // Get application settings
        var queryTimeout = config.GetValue("QueryTimeoutSeconds", 30);
        var enableCaching = config.GetValue("EnableUserCache", true);
        var cacheExpiration = config.GetNestedValue<int>("Cache", "ExpirationMinutes", 60);
        
        _logger.LogInformation(
            "Fetching user {UserId} (timeout: {Timeout}s, caching: {Caching})",
            userId, queryTimeout, enableCaching);
        
        // Use configuration values...
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        
        // Query with timeout
        using var cmd = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
        cmd.CommandTimeout = queryTimeout;
        cmd.Parameters.AddWithValue("@Id", userId);
        
        // Execute and return user...
        return await ExecuteQueryAsync(cmd, enableCaching, cacheExpiration);
    }
}
```

## appSettings.json Example

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyApp;Integrated Security=true;",
    "Analytics": "Server=localhost;Database=Analytics;Integrated Security=true;",
    "Logs": "Server=localhost;Database=Logs;Integrated Security=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "QueryTimeoutSeconds": 30,
  "MaxRetries": 3,
  "EnableUserCache": true,
  "Cache": {
    "ExpirationMinutes": 60,
    "MaxSize": 1000
  },
  "Database": {
    "CommandTimeout": 30,
    "MaxPoolSize": 100,
    "EnableRetry": true
  }
}
```

## Common Patterns

### Pattern: Settings Class
```csharp
public class AppSettings
{
    private readonly AppConfiguration _config;
    
    public AppSettings(IConfigurationService configService)
    {
        _config = configService.Configuration;
    }
    
    public string ConnectionString => 
        _config.GetConnectionString() 
        ?? throw new InvalidOperationException("Connection string required");
    
    public int MaxRetries => _config.GetValue("MaxRetries", 3);
    public int Timeout => _config.GetValue("TimeoutSeconds", 30);
    public bool EnableCache => _config.GetValue("EnableCache", true);
}

// Register in DI
builder.Services.AddSingleton<AppSettings>();

// Use
public class MyService
{
    private readonly AppSettings _settings;
    
    public MyService(AppSettings settings)
    {
        _settings = settings;
    }
    
    public void DoWork()
    {
        for (int i = 0; i < _settings.MaxRetries; i++)
        {
            // ...
        }
    }
}
```

### Pattern: Extension Methods
```csharp
public static class ConfigurationExtensions
{
    public static DatabaseConfig GetDatabaseConfig(this AppConfiguration config)
    {
        return new DatabaseConfig
        {
            ConnectionString = config.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string required"),
            CommandTimeout = config.GetNestedValue<int>("Database", "CommandTimeout", 30),
            MaxPoolSize = config.GetNestedValue<int>("Database", "MaxPoolSize", 100),
            EnableRetry = config.GetNestedValue<bool>("Database", "EnableRetry", false)
        };
    }
}

// Use
var dbConfig = _configService.Configuration.GetDatabaseConfig();
```

## Quick Reference

| Method | Description | Example |
|--------|-------------|---------|
| `GetConnectionString()` | Get default connection string | `config.GetConnectionString()` |
| `GetConnectionString(name)` | Get named connection string | `config.GetConnectionString("Analytics")` |
| `GetValue<T>(key, default)` | Get top-level setting | `config.GetValue("Timeout", 30)` |
| `GetNestedValue<T>(section, key, default)` | Get nested setting | `config.GetNestedValue<int>("DB", "Pool", 50)` |
| `IsLoaded` | Check if config loaded | `if (config.IsLoaded) { ... }` |
| `ConfigFilePath` | Get config file path | `config.ConfigFilePath` |

See [CONFIGURATION_USAGE.md](CONFIGURATION_USAGE.md) for detailed documentation.
