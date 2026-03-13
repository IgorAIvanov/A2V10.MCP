# Database Connection Configuration

## Overview
This MCP server automatically connects to a SQL Server database when started by retrieving the connection string from the `appSettings.json` file in your project root.

## How It Works

1. **MCP Client Roots**: When the server starts, it requests the project roots from the MCP client.
2. **Configuration Discovery**: The server searches for `appSettings.json` files in each root directory.
3. **Connection String Extraction**: It looks for the connection string at `ConnectionStrings.Default` in the JSON structure.
4. **Automatic Connection**: The first valid connection string found is used to connect to the database.

## Configuration Steps

### 1. Create appSettings.json

Create an `appSettings.json` file in your project root directory with the following structure:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### 2. Connection String Format

For SQL Server, use one of the following formats:

**Windows Authentication:**
```json
"Default": "Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;"
```

**SQL Server Authentication:**
```json
"Default": "Server=localhost;Database=MyDatabase;User Id=myUser;Password=myPassword;TrustServerCertificate=true;"
```

### 3. Example File

A sample configuration file is provided as `appSettings.json.example`. Copy and modify it for your environment:

```bash
cp appSettings.json.example appSettings.json
```

## Logging

The server logs the following events:
- Discovery of project roots
- Location of `appSettings.json` files
- Connection status (success or failure)

Check the standard error output for these logs when running the server.

## Troubleshooting

### No Connection String Found
If you see "No connection strings found in appSettings.json files":
- Ensure `appSettings.json` exists in your project root
- Verify the JSON structure matches the expected format
- Check that the MCP client is providing the correct project roots

### Connection Failed
If the database connection fails:
- Verify the server name, database name, and credentials
- Check that SQL Server is running and accessible
- Ensure firewall rules allow the connection
- For local SQL Server instances, try `Server=localhost` or `Server=(localdb)\\MSSQLLocalDB`

### Multiple Roots
If multiple project roots are provided and multiple `appSettings.json` files exist:
- The server will use the first valid connection string found
- Check the logs to see which configuration file is being used

## Security Notes

- **Never commit `appSettings.json` with real credentials to version control**
- Add `appSettings.json` to your `.gitignore` file
- Use environment variables or secure secret management for production deployments
- Consider using Windows Authentication when possible to avoid storing passwords
