using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;

namespace A2v10.McpServer.Tools.DBTools
{
    public interface ISqlServerHelper
    {
        Task<string> ExecuteQueryAsync(string sql);
    }

    public class SqlServerHelper : ISqlServerHelper
    {
        private readonly string _connectionString;
        public SqlServerHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string> ExecuteQueryAsync(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var result = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.GetValue(i);
                result.Add(row);
            }
            return JsonSerializer.Serialize(result);
        }
    }
}
