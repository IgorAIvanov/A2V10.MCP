# A2V10 MCP Server

MCP Server for A2V10 platform providing database and XAML tools.

## Features

- **Database Tools**: SQL Server connectivity, query execution, schema inspection
- **XAML Tools**: XAML tag information and validation
- **Automatic Database Connection**: Connects to SQL Server using configuration from your project's `appSettings.json`

## Quick Start

1. **Configure Database Connection**

   Create an `appSettings.json` file in your project root:

   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;"
     }
   }
   ```

   See [Database Connection Configuration](docs/DATABASE_CONNECTION.md) for more details.

2. **Build and Run**

   ```bash
   dotnet build
   dotnet run
   ```

3. **Test with MCP Inspector**

   ```bash
   npx -y @modelcontextprotocol/inspector "C:\AI\A2V10.MCP\bin\Debug\net10.0\A2V10.MCP.exe"
   ```

## Documentation

- [Database Connection Configuration](docs/DATABASE_CONNECTION.md) - How to configure database connections

## Project Structure

```
A2V10.MCP/
├── Program.cs                          # Application entry point
├── Tools/
│   ├── DBTools/                        # Database-related tools
│   │   ├── ExecuteSQL.cs               # SQL execution tool
│   │   ├── SearchObjects.cs            # Database object search
│   │   ├── SQLServerConnector.cs       # SQL Server connector implementation
│   │   └── ...
│   ├── Xaml/                           # XAML-related tools
│   └── Helpers/
│       └── ConfigHelper.cs             # Configuration file helper
└── docs/                               # Documentation
```

## How It Works

When the server starts:

1. The `DatabaseInitializationService` requests project roots from the MCP client
2. It searches for `appSettings.json` in each root directory
3. Extracts the connection string from `ConnectionStrings.Default`
4. Automatically connects to the SQL Server database
5. Makes the connection available to all database tools

## Requirements

- .NET 10.0
- SQL Server (for database features)

## License

[Add your license information here]
