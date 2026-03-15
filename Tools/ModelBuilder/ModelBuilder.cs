using A2v10.McpServer.Configuration;
using A2v10.Xaml;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
    internal class ModelBuilder
    {
        private readonly ILogger _logger;
        public ModelBuilder(ILogger<ModelBuilder> logger)
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



                return "Not implemented yet";
        }

        public string GetModelJson(string modelDefinition)
        {
            // Here you would parse the input modelDefinition and generate the corresponding model JSON.
            // This is a placeholder implementation.
            _logger.LogInformation("Generating model JSON from definition: {ModelDefinition}", modelDefinition);
            // load ./Resources/model.json as template and replace placeholders with values from modelDefinition
            var template = File.ReadAllText("./Resources/model.json");
            var path = "uncnoun";



            return $"{{ \"model\": \"Generated from {modelDefinition}\" }}";
        }
    }
}
