// Copyright © 2026 Igor Ivanov. All rights reserved.
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.IO;

namespace A2V10.MCP.Resources.ModelCreate
{
    [McpServerResourceType]
    public static class ModelScaffoldResources
    {
        [McpServerResource(UriTemplate = "template://model-scaffold/model-json", Name = "ModelScaffold Model JSON Template", MimeType = "application/json")]
        [Description("Returns the A2V10 model.json template for model scaffold.")]
        public static string GetModelJsonTemplate() => ReadTemplate("model.json");

        [McpServerResource(UriTemplate = "template://model-scaffold/index-view", Name = "ModelScaffold Index View XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 index view XAML template for model scaffold.")]
        public static string GetIndexViewTemplate() => ReadTemplate("index.view.xaml");

        [McpServerResource(UriTemplate = "template://model-scaffold/edit-dialog", Name = "ModelScaffold Edit Dialog XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 edit dialog XAML template for model scaffold.")]
        public static string GetEditDialogTemplate() => ReadTemplate("edit.dialog.xaml");

        [McpServerResource(UriTemplate = "template://model-scaffold/browse-dialog", Name = "ModelScaffold Browse Dialog XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 browse dialog XAML template for model scaffold.")]
        public static string GetBrowseDialogTemplate() => ReadTemplate("browse.dialog.xaml");

        [McpServerResource(UriTemplate = "template://model-scaffold/index-ts", Name = "ModelScaffold Index TypeScript Template", MimeType = "application/typescript")]
        [Description("Returns the A2V10 index TypeScript template for model scaffold.")]
        public static string GetIndexTsTemplate() => ReadTemplate("index.template.ts");

        [McpServerResource(UriTemplate = "template://model-scaffold/edit-ts", Name = "ModelScaffold Edit TypeScript Template", MimeType = "application/typescript")]
        [Description("Returns the A2V10 edit TypeScript template for model scaffold.")]
        public static string GetEditTsTemplate() => ReadTemplate("edit.template.ts");

        private static string ReadTemplate(string fileName)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ModelScaffold", "Resources", fileName);
            if (!File.Exists(fullPath))
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "ModelScaffold", "Resources", fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Template file not found: {fileName}");

            return File.ReadAllText(fullPath);
        }
    }
}
