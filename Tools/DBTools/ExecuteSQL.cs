
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;


namespace A2v10.McpServer.Tools.DBTools
{
    [McpServerToolType]
    public class ExecuteSQL 
    {
        private readonly SQLServerConnector _connector;

        public ExecuteSQL(SQLServerConnector connector)
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
