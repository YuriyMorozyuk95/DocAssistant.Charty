using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground;

public class AzureSqlSchemaGeneratorTest : IClassFixture<WebApplicationFactory<Program>>  
{  
    private readonly ITestOutputHelper _output;

    private readonly IAzureSqlSchemaGenerator? _azureSqlSchemaGenerator;

    public AzureSqlSchemaGeneratorTest(WebApplicationFactory<Program> factory, ITestOutputHelper output)  
    {  
        _output = output;

        using var scope = factory.Services.CreateScope();  
        _azureSqlSchemaGenerator = scope.ServiceProvider.GetService<IAzureSqlSchemaGenerator>(); 
    }  
  
    [Fact]  
    public void GenerateAzureSqlSchemaDdl()  
    {
        //var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=test-database;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=VintageShopDB;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        using (SqlConnection connection = new SqlConnection(connectionString))  
        {  
            connection.Open();  
  
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
  
            using (SqlCommand command = new SqlCommand(query, connection))  
            {  
                using (SqlDataReader reader = command.ExecuteReader())  
                {  
                    string currentTable = null;  
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
                                _output.WriteLine(");");  
                            }  
                            currentTable = tableName;  
                            _output.WriteLine($"CREATE TABLE {currentTable} (");  
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
  
                        _output.WriteLine($"    {columnDefinition},");  
                    }  
  
                    if (currentTable != null)  
                    {  
                        _output.WriteLine(");");  
                    }  
                }  
            }  
        }  
    }

    [Fact]  
    public void GenerateAzureSqlSchemaInTableItems()  
    {
        var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=test-database;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        List<string> tables = new List<string>();

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();  
  
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
        List<string> currentTableDdl = new List<string>();  
  
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
                    tables.Add(string.Join(Environment.NewLine, currentTableDdl));  
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
            tables.Add(string.Join(Environment.NewLine, currentTableDdl));  
        }

        // Assert or output the tables list for verification  
        Assert.NotEmpty(tables); // Example assertion to ensure the list is not empty  
  
        // Optional: Print the DDL statements for debugging purposes  
        foreach (var table in tables)  
        {
            _output.WriteLine("Table item");
            _output.WriteLine(table);  
        }  
    }

    [Fact]  
    public void Service_GenerateAzureSqlSchemaInTableItems()  
    {
        var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=test-database;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        var tables = _azureSqlSchemaGenerator.GetTableNamesDdlSchemas(connectionString);

        foreach (var table in tables)  
        {
            _output.WriteLine($"ServerName: {table.ServerName}");
            _output.WriteLine($"DatabaseName: {table.DatabaseName}");
            _output.WriteLine($"Table: {table.TableName}");
            _output.WriteLine(table.Schema);  
        } 
    }
}
