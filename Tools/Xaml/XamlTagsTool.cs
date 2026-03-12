using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace A2V10.McpServer.Tools.Xaml
{

    [McpServerToolType]
    public static class XamlTagsTool
    {
        [McpServerTool, Description("Returns all unique tag names from A2v10.ViewEngine.Xaml.")]
        public static string GetAllTags()
        {
            var tags = XamlTagHelper.GetCachedTags();
            var tagNames = tags.Select(t => t.Tag).ToArray();
            return JsonSerializer.Serialize(tagNames);
        }

        [McpServerTool, Description("Returns all attributes for the specified tag name from A2v10.ViewEngine.Xaml.")]
        public static string GetAttributesForTag(string tagName)
        {
            var tags = XamlTagHelper.GetCachedTags();
            var tag = tags.FirstOrDefault(t => t.Tag == tagName);
            return tag != null ? JsonSerializer.Serialize(tag.Attributes) : JsonSerializer.Serialize(Array.Empty<string>());
        }

        [McpServerTool, Description("Returns all unique tag names and their attributes from A2v10.ViewEngine.Xaml.")]
        public static string GetTagsWithAttributes()
        {
            var tags = XamlTagHelper.GetCachedTags();
            return JsonSerializer.Serialize(tags);
        }
    }
}