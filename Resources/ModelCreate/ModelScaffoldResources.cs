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
        [McpServerResource(UriTemplate = "docs://model-create/step-by-step", Name = "A2V10 Model Creation Guide", MimeType = "text/markdown")]
        [Description("Step-by-step guide for creating a new A2V10 model.")]
        public static string GetModelCreationGuide() => ReadTemplate("create-model-step-by-step.md");

        [McpServerResource(UriTemplate = "docs://model-create/sql-dataset-to-json", Name = "A2V10 SQL DataSet to JSON", MimeType = "text/markdown")]
        [Description("Explains how multiple SQL result sets (A2V10 DataSet markup) are transformed into the JSON model.")]
        public static string GetSqlDataSetToJsonGuide() => ReadTemplate("a2v10-sql-dataset-to-json.md");

        [McpServerResource(UriTemplate = "docs://model-create/json-to-sql-update", Name = "A2V10 JSON to SQL Update", MimeType = "text/markdown")]
        [Description("Explains how JSON submitted by the client is mapped to SQL TVPs using ...Metadata and ...Update stored procedures.")]
        public static string GetJsonToSqlUpdateGuide() => ReadTemplate("a2v10-json-to-sql-update.md");

        [McpServerResource(UriTemplate = "template://model-scaffold/model-json", Name = "ModelScaffold Model JSON Template and Schema", MimeType = "application/json")]
        [Description("Returns the A2V10 model.json template and schema for model scaffold.")]
        public static string GetModelJsonTemplate()
        {
            var templates = new Dictionary<string, string>()
            {
                ["template"] = ReadTemplate("model.json"),
                ["schema"] = ReadTemplate("model-json-schema.json")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        [McpServerResource(UriTemplate = "template://model-scaffold/agent-schema-json", Name = "ModelScaffold Agent Schema JSON", MimeType = "application/json")]
        [Description("Returns the A2V10 agent schema.json template for model scaffold.")]
        public static string GetAgentSchemaJsonTemplate() => ReadTemplate("agent.schema.json", "AgentExample");

        [McpServerResource(UriTemplate = "template://model-scaffold/agent-meta-example", Name = "ModelScaffold Agent Metadata Example", MimeType = "application/json")]
        [Description("Returns the A2V10 agent meta.json example (generated model metadata).")]
        public static string GetAgentMetaExampleTemplate() => ReadTemplate("agent.meta.json", "AgentExample");

        [McpServerResource(UriTemplate = "template://model-scaffold/model-sql", Name = "ModelScaffold Model SQL Example", MimeType = "application/sql")]
        [Description("Returns the A2V10 model.sql template for model scaffold.")]
        public static string GetModelSqlTemplate() => ReadTemplate("model.sql", "AgentExample");

        [McpServerResource(UriTemplate = "template://model-scaffold/agent-sql-example", Name = "ModelScaffold Agent SQL Example", MimeType = "application/sql")]
        [Description("Returns the A2V10 agent SQL example script (stored procedures, table types, etc.).")]
        public static string GetAgentSqlExampleTemplate() => ReadTemplate("agent.sql", "AgentExample");


        [McpServerResource(UriTemplate = "template://model-scaffold/index-view", Name = "ModelScaffold Index View XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 index view XAML template for model scaffold.")]
        public static string GetIndexViewTemplate() {
            var templates = new Dictionary<string, string>()
            {
                ["index"] = ReadTemplate("index.view.xaml"),
                ["agentIndexXamlExample"] = ReadTemplate("edit.dialog.xaml", "AgentExample")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        [McpServerResource(UriTemplate = "template://model-scaffold/edit-dialog", Name = "ModelScaffold Edit Dialog XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 edit dialog XAML template for model scaffold.")]
        public static string GetEditDialogTemplate() {
            var templates = new Dictionary<string, string>()
            {
                ["edit"] = ReadTemplate("edit.dialog.xaml"),
                ["agentEditXamlExample"] = ReadTemplate("edit.dialog.xaml", "AgentExample")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        [McpServerResource(UriTemplate = "template://model-scaffold/browse-dialog", Name = "ModelScaffold Browse Dialog XAML", MimeType = "application/xml")]
        [Description("Returns the A2V10 browse dialog XAML template for model scaffold.")]
        public static string GetBrowseDialogTemplate() { 
            var templates = new Dictionary<string, string>()
            {
                ["browse"] = ReadTemplate("browse.dialog.xaml"),
                ["agentBrowseXamlExample"] = ReadTemplate("browse.dialog.xaml", "AgentExample")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        [McpServerResource(UriTemplate = "template://model-scaffold/index-ts", Name = "ModelScaffold Index TypeScript Template", MimeType = "application/typescript")]
        [Description("Returns the A2V10 index TypeScript template for model scaffold.")]
        public static string GetIndexTsTemplate() {
            var templates = new Dictionary<string, string>()
            {
                ["index"] = ReadTemplate("index.template.ts"),
                ["agentIndexTsExample"] = ReadTemplate("index.template.ts", "AgentExample")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        [McpServerResource(UriTemplate = "template://model-scaffold/edit-ts", Name = "ModelScaffold Edit TypeScript Template", MimeType = "application/typescript")]
        [Description("Returns the A2V10 edit TypeScript template for model scaffold.")]
        public static string GetEditTsTemplate() {
            var templates = new Dictionary<string, string>()
            {
                ["edit"] = ReadTemplate("edit.template.ts"),
                ["agentEditTsExample"] = ReadTemplate("edit.template.ts", "AgentExample")
            };
            return System.Text.Json.JsonSerializer.Serialize(templates);
        }

        private static string ReadTemplate(string fileName, string subFolder = "")
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ModelScaffold", "Resources", subFolder, fileName);
            if (!File.Exists(fullPath))
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "ModelScaffold", "Resources", subFolder, fileName);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Template file not found: {fileName}");

            return File.ReadAllText(fullPath);
        }
    }
}
