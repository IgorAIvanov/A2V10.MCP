// Copyright © 2026 Igor Ivanov. All rights reserved.
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace A2v10.McpServer.Tools
{

    [McpServerToolType]
    public static class EchoTool
    {
        [McpServerTool, Description("Echoes the message back to the client.")]
        public static string Echo(string message) => $"hello {message}";
    }
}