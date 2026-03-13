using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;


namespace A2v10.McpServer.Tools.DBTools
{
    [McpServerToolType]
    public class SerchObjects
    {
        private readonly ISqlServerHelper _sqlHelper;

        public SerchObjects(ISqlServerHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }


        [McpServerTool, Description("Search and list database objects (schemas, tables, columns, procedures, indexes) with pattern matching and token-efficient progressive disclosure")]
        public async Task<string> Execute(string sql)
        {
            return await _sqlHelper.ExecuteQueryAsync(sql);
        }
    }
}
