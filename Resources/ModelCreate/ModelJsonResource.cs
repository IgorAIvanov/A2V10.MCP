using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace A2V10.MCP.Resources.ModelCreate
{
    [McpServerResourceType]
    public class MyResources
    {
        [McpServerResource(UriTemplate = "config://app/settings", Name = "App Settings", MimeType = "application/json")]
        [Description("Returns application configuration settings")]
        public static string GetSettings() => JsonSerializer.Serialize(new { theme = "dark", language = "en" });
    }

}
