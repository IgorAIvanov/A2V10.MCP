// Copyright © 2026 Igor Ivanov. All rights reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
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
            var tag = tags.FirstOrDefault(t => string.Equals(t.Tag, tagName, StringComparison.OrdinalIgnoreCase));
            return tag != null ? JsonSerializer.Serialize(tag.Attributes) : JsonSerializer.Serialize(Array.Empty<string>());
        }

        [McpServerTool, Description("Returns tags starting with the specified prefix.")]
        public static string SuggestTags(string? prefix = null, int limit = 20)
        {
            var max = Math.Clamp(limit, 1, 100);
            var tags = XamlTagHelper.GetCachedTags()
                .Select(t => t.Tag)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(prefix))
                tags = tags.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return JsonSerializer.Serialize(tags.OrderBy(t => t).Take(max).ToArray());
        }

        [McpServerTool, Description("Returns attributes for a tag filtered by prefix.")]
        public static string SuggestAttributes(string tagName, string? prefix = null, int limit = 20)
        {
            var max = Math.Clamp(limit, 1, 100);
            var tag = XamlTagHelper.GetCachedTags()
                .FirstOrDefault(t => string.Equals(t.Tag, tagName, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
                return JsonSerializer.Serialize(Array.Empty<string>());

            IEnumerable<string> attrs = tag.Attributes;
            if (!string.IsNullOrWhiteSpace(prefix))
                attrs = attrs.Where(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return JsonSerializer.Serialize(attrs.OrderBy(a => a).Take(max).ToArray());
        }

        [McpServerTool, Description("Validates that tag and attributes exist in A2v10.ViewEngine.Xaml.")]
        public static string ValidateElement(string tagName, string[] attributes)
        {
            var errors = new List<string>();
            var tag = XamlTagHelper.GetCachedTags()
                .FirstOrDefault(t => string.Equals(t.Tag, tagName, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
            {
                errors.Add($"Unknown tag '{tagName}'.");
            }
            else
            {
                var known = new HashSet<string>(tag.Attributes, StringComparer.OrdinalIgnoreCase);
                foreach (var attr in attributes ?? Array.Empty<string>())
                {
                    if (!known.Contains(attr))
                        errors.Add($"Unknown attribute '{attr}' for tag '{tagName}'.");
                }
            }

            return JsonSerializer.Serialize(new
            {
                IsValid = errors.Count == 0,
                Errors = errors
            });
        }

        [McpServerTool, Description("Validates full XAML document and returns unknown tags/attributes with locations.")]
        public static string ValidateXamlDocument(string xaml)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(xaml))
            {
                errors.Add("XAML content is empty.");
                return JsonSerializer.Serialize(new { IsValid = false, Errors = errors });
            }

            XDocument document;
            try
            {
                document = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                errors.Add($"XAML parse error: {ex.Message}");
                return JsonSerializer.Serialize(new { IsValid = false, Errors = errors });
            }

            if (document.Root == null)
            {
                errors.Add("XAML document has no root element.");
                return JsonSerializer.Serialize(new { IsValid = false, Errors = errors });
            }

            var tags = XamlTagHelper.GetCachedTags();
            var tagMap = tags
                .GroupBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var element in document.Root.DescendantsAndSelf())
            {
                var tagName = element.Name.LocalName;
                if (!IsValidTag(tagName, tagMap, out var tagInfo))
                {
                    errors.Add($"Unknown tag '{tagName}'{GetLocationSuffix(element)}.");
                    continue;
                }

                var knownAttributes = new HashSet<string>(tagInfo.Attributes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    var attributeName = attribute.Name.LocalName;
                    if (!knownAttributes.Contains(attributeName))
                        errors.Add($"Unknown attribute '{attributeName}' for tag '{tagName}'{GetLocationSuffix(attribute)}.");
                }
            }

            return JsonSerializer.Serialize(new
            {
                IsValid = errors.Count == 0,
                Errors = errors
            });
        }

        private static bool IsValidTag(string tagName, Dictionary<string, XamlTagInfo> tagMap, out XamlTagInfo? tagInfo)
        {
            tagInfo = null;

            if (tagMap.TryGetValue(tagName, out var foundTag))
            {
                tagInfo = foundTag;
                return true;
            }

            if (tagName.Contains('.'))
            {
                var parts = tagName.Split('.');
                bool allPartsValid = true;

                foreach (var part in parts)
                {
                    if (!tagMap.ContainsKey(part))
                    {
                        allPartsValid = false;
                        break;
                    }
                }

                if (allPartsValid && parts.Length > 0)
                {
                    tagInfo = tagMap[parts[^1]];
                    return true;
                }
            }

            return false;
        }

        private static string GetLocationSuffix(XObject node)
        {
            if (node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                return $" at line {lineInfo.LineNumber}, position {lineInfo.LinePosition}";
            return String.Empty;
        }
    }
}