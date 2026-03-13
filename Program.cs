using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using A2V10.McpServer.Tools.Xaml;
using A2v10.McpServer.Services;

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


            // Регистрация SQL Server connector в DI
            builder.Services.AddSingleton<Tools.DBTools.IConnector>(provider =>
                    new Tools.DBTools.SQLServerConnector()
                );

            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            // Регистрация сервиса инициализации базы данных
            builder.Services.AddHostedService<DatabaseInitializationService>();

            // Инициализация кэша тегов XAML
            XamlTagHelper.InitializeCache();

            await builder.Build().RunAsync();
        }
    }
}
