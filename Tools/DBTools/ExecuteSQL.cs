
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;


namespace A2v10.McpServer.Tools.DBTools
{
    [McpServerToolType]
    public class ExecuteSQL 
    {
        private readonly IConnector _connector;

        public ExecuteSQL(IConnector connector)
        {
            _connector = connector;
        }

        [McpServerTool, Description("Executes the specified SQL query and returns the result.")]
        public async Task<string> Execute(string sql)
        {
            var options = new ExecuteOptions
            {
                ReadOnly = false
            }; // или true, если только SELECT

            var result = await _connector.ExecuteSQLAsync(sql, options);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool, Description("Test database connection initialization")]
        public async Task<string> TestDatabaseInit()
        {
            try
            {
                var schemas = await _connector.GetSchemasAsync();
                return $"Success! Found {schemas.Count} schemas: {string.Join(", ", schemas)}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}\n\nStack trace: {ex.StackTrace}";
            }
        }


        [McpServerTool, Description("Lists the user's project roots")]
        public static async Task<string> ListProjectRoots(ModelContextProtocol.Server.McpServer server, CancellationToken cancellationToken)
        {
            var result = await server.RequestRootsAsync(new ListRootsRequestParams(), cancellationToken);

            var summary = new StringBuilder();
            foreach (var root in result.Roots)
            {
                summary.AppendLine($"- {root.Name ?? root.Uri}: {root.Uri}");
            }

            return summary.ToString();
        }
    }


}
