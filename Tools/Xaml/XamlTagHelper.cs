// Copyright © 2026 Igor Ivanov. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using A2v10.Xaml;

namespace A2V10.McpServer.Tools.Xaml
{

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

            result.AddRange(GetAdditionalValidTags());
            return result.ToArray();
        }

        private static XamlTagInfo[] GetAdditionalValidTags()
        {
            return
            [
                new XamlTagInfo
                {
                    Tag = "Filter",
                    Attributes = ["For", "Operator", "Value", "DataType"]
                },
                new XamlTagInfo
                {
                    Tag = "FilterDescription",
                    Attributes = ["Property", "Label", "DataType", "Operator"]
                },
                new XamlTagInfo
                {
                    Tag = "DropDown",
                    Attributes = ["Label", "ItemsSource", "DisplayProperty", "ValueProperty", "Value"]
                }
            ];
        }
    }
}