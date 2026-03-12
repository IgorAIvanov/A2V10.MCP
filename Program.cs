using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Reflection;
using System.Xml.Linq;
using System.IO;

using A2v10.Xaml;
using A2v10.McpServer;
using A2V10.McpServer.Tools.Xaml;

namespace A2v10.McpServer
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            // Инициализация кэша тегов XAML
            XamlTagHelper.InitializeCache();

            await builder.Build().RunAsync();
        }
    }
}
