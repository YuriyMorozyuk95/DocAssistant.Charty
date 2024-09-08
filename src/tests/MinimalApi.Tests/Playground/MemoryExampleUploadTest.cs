using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shared.Models;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground
{
    public class MemoryExampleUploadTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly ITestOutputHelper _output;
        private readonly IAzureSqlSchemaGenerator _azureSqlSchemaGenerator;
        private readonly IMemoryManagerService _memoryManagerService;

        private readonly IMemorySearchService _memorySearchService;

        public MemoryExampleUploadTest(WebApplicationFactory<Program> factory, ITestOutputHelper output)
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
            await foreach (var example in _memorySearchService.GetAllExamples())
            {
                _output.WriteLine($"ServerName: {example.ServerName}");
                _output.WriteLine($"DatabaseName: {string.Join(",", example.DatabaseNames)}");
                _output.WriteLine($"TableName: {string.Join(",", example.TableNames)}");
                _output.WriteLine($"SQL Example: {example.SqlExample}");
                _output.WriteLine($"User Prompt Example: {example.UserPromptExample}");
            }
        }

        [Fact]
        public async Task UploadExampleToMemoryTest()
        {
            var examples = new List<Example>()
            {
                new Example
                {
                    ServerName = "hack-rag-sql-server.database.windows.net",
                    DatabaseNames = ["test-database"],
                    SqlExample = "SELECT [CustomerName], [Email] FROM [Customers];",
                    UserPromptExample = "List all orders along with the customer names who placed them."
                },
                new Example
                {
                    ServerName = "hack-rag-sql-server.database.windows.net",
                    DatabaseNames = ["test-database"],
                    SqlExample = "SELECT SUM([Quantity]) AS TotalQuantity  FROM [OrderDetails]",
                    UserPromptExample = "Get the total quantity of products ordered"
                },
                new Example
                {
                    ServerName = "hack-rag-sql-server.database.windows.net",
                    DatabaseNames = ["test-database"],
                    SqlExample = "SELECT [CustomerName], [Email]  FROM [Customers]",
                    UserPromptExample = "List all suppliers and their contact names"
                },
            };

            foreach (var example in examples)
            {
                var documentId = await _memoryManagerService.UploadExampleToMemory(example);
                Assert.NotNull(documentId);
                _output.WriteLine($"Uploaded Example DocumentId: {documentId}");
            }

        }

        [Fact]
        public async Task CleanUpExamples()
        {
            await _memoryManagerService.RemoveExampleSchemaIndex();
        }
    }
}
