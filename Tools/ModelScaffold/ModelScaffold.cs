// Copyright © 2026 Igor Ivanov. All rights reserved.
using A2v10.McpServer.Configuration;
using A2v10.Xaml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace A2V10.MCP.Tools.ModelBuilder
{

    public static class GenerateOptions
    {
        public const string SQLDataDefinition = "sqlDefinition";
        public const string StoreProcedures = "storeProcedures";
        public const string ModelJson = "modelJson";
        public const string IndexView = "indexView";
        public const string Dialog = "dialog";
        public const string Action = "action";
    }
    


        [McpServerToolType]
    internal class ModelScaffold
    {
        private readonly ILogger _logger;
        public ModelScaffold(ILogger<ModelScaffold> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Build A2V10 model files. Input: json model definition (meta.modelname.json)")]
        public async Task<string> Build(string modelDefinition, string options)
        {
            if (modelDefinition == null)
                throw new ArgumentNullException(nameof(modelDefinition));

            var execOptions = new List<string>();
            if (!string.IsNullOrEmpty(options))
                execOptions = new List<string>(options.Split(',').Select(o => o.Trim().ToLower()));
            else
            // add all options by default, using reflection to get all constants from GenerateOptions
            {
                var fields = typeof(GenerateOptions).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null)?.ToString()?.ToLower();
                    if (value != null)
                        execOptions.Add(value);
                }
            }

            var result = "";
            //if (execOptions.Contains(GenerateOptions.ModelJson)) 
            { result = result + GetModelJson(modelDefinition) + Environment.NewLine; }

            return result;
        }

        public string GetModelJson(string modelDefinition)
        {
            // Here you would parse the input modelDefinition and generate the corresponding model JSON.
            // This is a placeholder implementation.
            _logger.LogInformation("Generating model JSON from definition: {ModelDefinition}", modelDefinition);
            // load ./Resources/model.json as template and replace placeholders with values from modelDefinition
            var template = File.ReadAllText("./Resources/model.json");
           
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            using var doc =  JsonDocument.Parse(modelDefinition, options);
            var rootElement = doc.RootElement;
            var keys = new Dictionary<string, string>();
            keys.Add("SchemaName", rootElement.GetProperty("schema").GetString() ?? string.Empty);
            keys.Add("ModelName", rootElement.GetProperty("name").GetString() ?? string.Empty);

            var result = ReplaceTemplatePlaceholders(template, keys);
            result = "{\"fileName\": \"model.json\", \"content\": " + result + "}";

            return result;
        }

        private static string ReplaceTemplatePlaceholders(string template, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            if (replacements == null || replacements.Count == 0)
                return template;

            var result = new StringBuilder(template);
           

            foreach (var kvp in replacements)
            {
                var placeholder = $"$({kvp.Key})";
                result.Replace(placeholder, kvp.Value);
            }
            
            
            

            return result.ToString();
        }
    }
}
