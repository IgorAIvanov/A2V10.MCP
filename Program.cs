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

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

public class XamlTagInfo
{
    public string Tag { get; set; } = string.Empty;
    public string[] Attributes { get; set; } = Array.Empty<string>();
}



public static class XamlTagHelper
{
    private static XamlTagInfo[]? _cachedTags;

    public static void InitializeCache()
    {
        _cachedTags = GetXamlTagsWithAttributesFromReference();
    }

    public static XamlTagInfo[] GetCachedTags()
    {
        return _cachedTags ?? Array.Empty<XamlTagInfo>();
    }

    private static XamlTagInfo[] GetXamlTagsWithAttributesFromReference()
    {
        var baseType = typeof(XamlElement);
        var asm = baseType.Assembly;

        var tagTypes = asm.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t) && t.IsPublic)
            .ToArray();

        var result = new List<XamlTagInfo>();
        foreach (var type in tagTypes)
        {
            string tagName = type.Name;
            var xamlNameAttr = type.GetCustomAttributes(false)
                .FirstOrDefault(a => a.GetType().Name == "XamlNameAttribute");
            if (xamlNameAttr != null)
            {
                var nameProp = xamlNameAttr.GetType().GetProperty("Name");
                if (nameProp != null)
                {
                    string? name = nameProp.GetValue(xamlNameAttr) as string;
                    if (!string.IsNullOrEmpty(name))
                        tagName = name;
                }
            }
            var attrs = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.DeclaringType != typeof(object))
                .Select(p => p.Name)
                .Distinct()
                .ToArray();
            result.Add(new XamlTagInfo { Tag = tagName, Attributes = attrs });
        }
        return result.ToArray();
    }
}

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}

[McpServerToolType]
public static class XamlTagTool
{
    [McpServerTool, Description("Returns all unique tag names and their attributes from A2v10.ViewEngine.Xaml.")]
    public static string GetTagsWithAttributes()
    {
        var tags = XamlTagHelper.GetCachedTags();
        return JsonSerializer.Serialize(tags);
    }
}

