using Azure.Identity;
using Azure.Search.Documents.Models;
using System.Text.Json;

using Azure.Search.Documents;

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Azure.Core;
using Microsoft.Extensions.Configuration;

using Shared.Models;
using DocAssistant.Charty.Ai.Extensions;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IMemorySearchService
{
    IAsyncEnumerable<TableSchema> SearchDataBaseSchema(string userPrompt, int supportingContentCount, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TableSchema> CanGetAllSchemas();
}

public class MemorySearchService : IMemorySearchService
{
    private readonly IDocumentStorageService _documentStorageService;

    private readonly ILogger<MemorySearchService> _logger;

    private readonly TokenCredential _tokenCredential;

    private readonly MemoryServerless _memory;

    private readonly string? _azureAiSearchEndpoint;

    public MemorySearchService(
        MemoryServerless memory,
        IDocumentStorageService documentStorageService,
        ILogger<MemorySearchService> logger,
        TokenCredential tokenCredential,
        IConfiguration configuration)
    {
        _memory = memory;
        _documentStorageService = documentStorageService;
        _logger = logger;
        _tokenCredential = tokenCredential;

        _azureAiSearchEndpoint = configuration["KernelMemory:Services:AzureAISearch:Endpoint"];
    }

    public async IAsyncEnumerable<TableSchema> SearchDataBaseSchema(string userPrompt, int supportingContentCount, CancellationToken cancellationToken = default)
    {
        SearchResult searchResult = null;
        string[] debugContent = null;

        try
        {
            var searchTablesSchemaPrompt = $"Please retrive all tables schema that will be helpful for writing sql-t query for Azure SQL Database for user prompt: \"{userPrompt}\"";

            searchResult = await _memory.SearchAsync(
                               searchTablesSchemaPrompt,
                               index: MemoryManagerService.DataBaseSchemaIndex,
                               limit: supportingContentCount,
                               cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while searching in DocAssistant memory");
            debugContent = [e.ToString()];
        }

        if (debugContent != null)
        {
            foreach (var error in debugContent)
            {
                //TODO
                //yield return error;
            }

            yield break;
        }

        if (searchResult.Results == null || !searchResult.Results.Any())
        {
            yield break;
        }

        foreach (var document in searchResult.Results)
        {
            var tableSchema = await _documentStorageService.GetDocumentContent(document.DocumentId, document.SourceName, MemoryManagerService.DataBaseSchemaIndex);

            var tags = document.Partitions.First().Tags;
            var server = tags.GetTagValue(TagsKeys.Server);
            var database = tags.GetTagValue(TagsKeys.Database);
            var table = tags.GetTagValue(TagsKeys.Table);
            var connectionString = tags.GetTagValue(TagsKeys.ConnectionString);


            yield return new TableSchema()
            {
                Schema = tableSchema,
                ServerName = server,
                DatabaseName = database,
                TableName = table,
                ConnectionString = connectionString,
            };
        }
    }


    public async IAsyncEnumerable<TableSchema> CanGetAllSchemas()
    {
        var searchClient = new SearchClient(
            new Uri(_azureAiSearchEndpoint),
            MemoryManagerService.DataBaseSchemaIndex,
            _tokenCredential);

        // Define the search options  
        var options = new SearchOptions
        {
            Size = 1000, // Adjust the size as needed, max is 1000 per request  
            Select = { "*" } // Select all fields  
        };

        SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>("*", options);

        // Assert that results are not null  
        // Create a list to hold the table schemas  
        var tableSchemas = new List<TableSchema>();

        // Iterate through the results and assert each document is not null  
        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            string serverName = null;
            string databaseName = null;
            string tableName = null;
            string schema = null;
            string connectionString = null;

            if (result.Document.TryGetValue("tags", out var tags))
            {
                var tagsArray = ((object[])tags).Skip(6).ToArray();
                serverName = tagsArray[0].ToString().Replace($"{TagsKeys.Server}:", string.Empty);
                databaseName = tagsArray[1].ToString().Replace($"{TagsKeys.Database}:", string.Empty);
                tableName = tagsArray[2].ToString().Replace($"{TagsKeys.Table}:", string.Empty);
                connectionString = tagsArray[3].ToString().Replace($"{TagsKeys.ConnectionString}:", string.Empty);
            }

            if (result.Document.TryGetValue("payload", out var payload))
            {
                using JsonDocument document = JsonDocument.Parse(payload.ToString());
                // Get the root element  
                JsonElement root = document.RootElement;

                // Get the value of the "text" property  
                if (root.TryGetProperty("text", out JsonElement textElement))
                {
                    schema = textElement.GetString();
                }
            }

            // Add the table schema to the list  
            yield return new TableSchema
            {
                DocumentId = result.Document["id"].ToString(),
                ServerName = serverName,
                DatabaseName = databaseName,
                TableName = tableName,
                Schema = schema,
                ConnectionString = connectionString,
            };
        }
    }
}
