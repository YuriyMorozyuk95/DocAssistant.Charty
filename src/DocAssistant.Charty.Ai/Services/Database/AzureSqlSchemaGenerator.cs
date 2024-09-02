using Microsoft.Data.SqlClient;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Database  
{
    public interface IAzureSqlSchemaGenerator  
    {  
        List<TableSchema> GetTableNamesDdlSchemas(string connectionString);  
    }  
  
    internal class AzureSqlSchemaGenerator : IAzureSqlSchemaGenerator  
    {  
        public List<TableSchema> GetTableNamesDdlSchemas(string connectionString)  
        {  
            List<TableSchema> tables = new List<TableSchema>();  
  
            using SqlConnection connection = new SqlConnection(connectionString);  
            connection.Open();  
  
            // Query to get the server
            string serverName = GetServerName(connection);

            string databaseName = connection.Database;  
  
            // Query to get table schema    
            string query = @"    
                    SELECT     
                        TABLE_NAME,     
                        COLUMN_NAME,     
                        DATA_TYPE,     
                        CHARACTER_MAXIMUM_LENGTH,     
                        IS_NULLABLE     
                    FROM INFORMATION_SCHEMA.COLUMNS     
                    ORDER BY TABLE_NAME, ORDINAL_POSITION";  
  
            using SqlCommand command = new SqlCommand(query, connection);  
            using SqlDataReader reader = command.ExecuteReader();  
            string currentTable = null;  
            List<string> currentTableDdl = new();  
  
            while (reader.Read())  
            {  
                string tableName = reader["TABLE_NAME"].ToString();  
                string columnName = reader["COLUMN_NAME"].ToString();  
                string dataType = reader["DATA_TYPE"].ToString();  
                string maxLength = reader["CHARACTER_MAXIMUM_LENGTH"].ToString();  
                string isNullable = reader["IS_NULLABLE"].ToString();  
  
                if (currentTable != tableName)  
                {  
                    if (currentTable != null)  
                    {  
                        currentTableDdl.Add(");");  
                        tables.Add(new TableSchema  
                        {  
                            ServerName = serverName,  
                            DatabaseName = databaseName,  
                            TableName = currentTable,  
                            Schema = string.Join(Environment.NewLine, currentTableDdl),
                            ConnectionString = connectionString,
                        });  
                    }  
                    currentTable = tableName;  
                    currentTableDdl = new List<string> { $"CREATE TABLE {currentTable} (" };  
                }  
  
                string columnDefinition = $"{columnName} {dataType}";  
  
                if (!string.IsNullOrEmpty(maxLength) && maxLength != "-1")  
                {  
                    columnDefinition += $"({maxLength})";  
                }  
  
                if (isNullable == "NO")  
                {  
                    columnDefinition += " NOT NULL";  
                }  
  
                currentTableDdl.Add($"    {columnDefinition},");  
            }  
  
            if (currentTable != null)  
            {  
                currentTableDdl.Add(");");  
                tables.Add(new TableSchema  
                {  
                    ServerName = serverName,  
                    DatabaseName = databaseName,  
                    TableName = currentTable,  
                    Schema = string.Join(Environment.NewLine, currentTableDdl),
                    ConnectionString = connectionString,
                });  
            }  
  
            return tables;  
        }

        private static string GetServerName(SqlConnection connection)
        {
            string serverName = connection.DataSource;
            // Remove 'tcp:' and ',1433' from the server name if they exist  
            if (serverName.StartsWith("tcp:"))  
            {  
                serverName = serverName.Substring(4);  
            }  
            if (serverName.EndsWith(",1433"))  
            {  
                serverName = serverName.Substring(0, serverName.Length - 5);  
            }

            return serverName;
        }
    }  
}  
