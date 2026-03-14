// Copyright © 2026 Igor Ivanov. All rights reserved.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using A2V10.McpServer.Tools.Xaml;
using A2v10.McpServer.Tools.DBTools;
using A2v10.McpServer.Configuration;

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

            // Регистрация сервиса конфигурации
            builder.Services.AddSingleton<IRemouteConfigurationService, RemouteConfigurationService>();

            // Регистрация SQL Server connector с ленивой инициализацией
            builder.Services.AddSingleton<IConnector>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<LazyConnector>>();
                var mcpServer = provider.GetRequiredService<ModelContextProtocol.Server.McpServer>();
                var remouteConfigService = provider.GetRequiredService<IRemouteConfigurationService>();
                var innerConnector = new SQLServerConnector();
                return new LazyConnector(logger, mcpServer, innerConnector, remouteConfigService);
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
