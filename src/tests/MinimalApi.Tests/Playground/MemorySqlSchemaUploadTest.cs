using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shared.Models;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground
{
    public class MemorySqlSchemaUploadTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly ITestOutputHelper _output;
        private readonly IAzureSqlSchemaGenerator _azureSqlSchemaGenerator;
        private readonly IMemoryManagerService _memoryManagerService;

        private readonly IMemorySearchService _memorySearchService;

        public MemorySqlSchemaUploadTest(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _output = output;

            using var scope = factory.Services.CreateScope();
            _azureSqlSchemaGenerator = scope.ServiceProvider.GetRequiredService<IAzureSqlSchemaGenerator>();
            _memoryManagerService = scope.ServiceProvider.GetRequiredService<IMemoryManagerService>();
            _memorySearchService = scope.ServiceProvider.GetRequiredService<IMemorySearchService>();

            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        [Fact]  
        public async Task CanGetAllExamples()  
        {  
            await foreach (var example in _memorySearchService.CanGetAllExamples())  
            {  
                _output.WriteLine($"ServerName: {example.ServerName}");  
                _output.WriteLine($"DatabaseName: {example.DatabaseName}");  
                _output.WriteLine($"TableName: {example.TableName}");  
                _output.WriteLine($"SQL Example: {example.SqlExample}");  
                _output.WriteLine($"User Prompt Example: {example.UserPromptExample}");  
            }  
        }

        [Fact]  
        public async Task UploadExampleToMemoryTest()  
        {  
            var example = new Example  
                          {  
                              ServerName = "test-server",  
                              DatabaseName = "test-database",  
                              TableName = "test-table",  
                              SqlExample = "SELECT * FROM test-table",  
                              UserPromptExample = "Get all records from test-table"  
                          };  
  
            _output.WriteLine($"Uploading Example: {example.SqlExample}");  
            var documentId = await _memoryManagerService.UploadExampleToMemory(example);  
            Assert.NotNull(documentId);  
            _output.WriteLine($"Uploaded Example DocumentId: {documentId}");  
        }  
  
        [Fact]  
        public async Task CleanUpExamples()  
        {  
            await _memoryManagerService.RemoveExampleSchemaIndex();  
        }

        [Fact]
        public async Task GenerateAndUploadAzureSqlSchemaToMemory()
        {
            var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=test-database;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var tables = _azureSqlSchemaGenerator.GetTableNamesDdlSchemas(connectionString);

            foreach (var table in tables)
            {
                _output.WriteLine($"Uploading Table: {table.TableName}");
                var uploadedTable = await _memoryManagerService.UploadTableSchemaToMemory(table);

                Assert.NotNull(uploadedTable.DocumentId);
                _output.WriteLine($"Uploaded DocumentId: {uploadedTable.DocumentId}");
            }
        }

        [Fact]
        public async Task CanGetAllSchemas()
        {
            await foreach (var table in _memorySearchService.CanGetAllSchemas())
            {
                _output.WriteLine($"ServerName: {table.ServerName}");
                _output.WriteLine($"DatabaseName: {table.DatabaseName}");
                _output.WriteLine($"Table: {table.TableName}");
                _output.WriteLine(table.Schema);
            }
        }

        [Fact]
        public async Task CleanUp()
        {
            await _memoryManagerService.RemoveDataBaseSchemaIndex();
        }
    }
}
