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

   If needed, you can run MCP in read-only mode:

   ```bash
   dotnet run -- --readOnly true
   ```

3. **Test with MCP Inspector**

   ```bash
   npx -y @modelcontextprotocol/inspector "path\ToMCP\A2V10.MCP\bin\Debug\net10.0\A2V10.MCP.exe"
   ```

## Run Published MCP

To use MCP, you can clone the repository and build the project yourself, or use the precompiled version from the `publish` directory.

1. Copy all files from the `publish` directory to a separate folder, for example `C:\Tools\A2V10.MCP`.
2. Configure your agent to launch MCP from that folder. You can also pass the `--readOnly` argument when starting it.

Example agent configuration:

```json
{
  "command": "C:\\Tools\\A2V10.MCP\\A2V10.MCP.exe",
  "args": ["--readOnly", "true"]
}
```

If you want to build the project yourself, run:

```bash
dotnet build
```

After the build, you can use the executable from `bin\Debug\net10.0`, or publish the project and copy all files from `publish` to a separate folder in the same way for use with your agent.

If the `--readOnly` argument is not specified, the default value is `true`.

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

MIT
