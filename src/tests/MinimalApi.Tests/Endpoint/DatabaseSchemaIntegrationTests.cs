using Microsoft.AspNetCore.Mvc.Testing;

using Shared.Models;

using SharedWebComponents.Services;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Endpoint;

public class DatabaseSchemaIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ApiClient _client;

    public DatabaseSchemaIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
    }

    [Fact]  
    public async Task Post_UploadExample_ReturnsDocumentId()  
    {  
        // Arrange  
        var example = new Example  
                      {  
                          ServerName = "test-server1",  
                          DatabaseName = "test-database1",  
                          TableName = "test-table1",  
                          SqlExample = "SELECT * FROM test-table1",  
                          UserPromptExample = "Get all records from test-table1",
                      };  
        var cancellationToken = CancellationToken.None;  
  
        // Act  
        var documentId = await _client.UploadExampleAsync(example, cancellationToken);  
  
        // Assert  
        Assert.NotNull(documentId);  
        _testOutputHelper.WriteLine($"Uploaded Example DocumentId: {documentId}");  
    }  
  
    [Fact]  
    public async Task Get_AllExamples_ReturnsExpectedExamples()  
    {  
        var cancellationToken = CancellationToken.None;  
        await foreach (var example in _client.GetAllExamplesAsync(cancellationToken))  
        {  
            _testOutputHelper.WriteLine($"Example: {example.UserPromptExample}, SQL: {example.SqlExample}, Table: {example.TableName}, Database: {example.DatabaseName}, Server: {example.ServerName}");  
        }  
    }

    [Fact]
    public async Task Post_UploadDatabaseSchema_ReturnsExpectedLogMessages()
    {
        // Arrange  
        var connectionString = "Server=tcp:hack-rag-sql-server.database.windows.net,1433;Initial Catalog=test-database;Persist Security Info=False;User ID=database-admin;Password=DocAssistant.Charty;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        var cancellationToken = CancellationToken.None;

        // Act  
        var logMessages = new List<string>();
        await foreach (var logMessage in _client.UploadDatabaseSchemaAsync(connectionString, cancellationToken))
        {
            logMessages.Add(logMessage);
        }

        // Assert  
        Assert.NotEmpty(logMessages);
        Assert.Contains(logMessages, message => message.StartsWith("Uploading Table:"));
        Assert.Contains(logMessages, message => message.StartsWith("Uploaded DocumentId:") || message.StartsWith("Failed to upload table:"));

        // Print the log messages  
        foreach (var logMessage in logMessages)
        {
            _testOutputHelper.WriteLine(logMessage);
        }
    }

    [Fact]
    public async Task Get_AllDatabaseSchemas_ReturnsExpectedSchemas()
    {
        var cancellationToken = CancellationToken.None;

        await foreach (var schema in _client.GetAllDatabaseSchemasAsync(cancellationToken))
        {
            _testOutputHelper.WriteLine($"Schema: {schema.Schema}, Table: {schema.TableName}, Database: {schema.DatabaseName}, Server: {schema.ServerName}");
        }
    }
}
