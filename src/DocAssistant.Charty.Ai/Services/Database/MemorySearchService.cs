﻿using Azure.Identity;
using Azure.Search.Documents.Models;
using System.Text.Json;

using Azure.Search.Documents;

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Azure.Core;
using Microsoft.Extensions.Configuration;

using Shared.Models;
using DocAssistant.Charty.Ai.Extensions;
using Azure;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IMemorySearchService
{
    IAsyncEnumerable<TableSchema> SearchDataBaseSchema(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Example> SearchExamples(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TableSchema> GetAllSchemas();
    IAsyncEnumerable<Example> GetAllExamples();
}

public class MemorySearchService : IMemorySearchService
{
    private readonly IDocumentStorageService _documentStorageService;

    private readonly ILogger<MemorySearchService> _logger;

    private readonly MemoryServerless _memory;

    private readonly string? _azureAiSearchEndpoint;

    private readonly string? _apiKey;

    public MemorySearchService(
        MemoryServerless memory,
        IDocumentStorageService documentStorageService,
        ILogger<MemorySearchService> logger,
        IConfiguration configuration)
    {
        _memory = memory;
        _documentStorageService = documentStorageService;
        _logger = logger;

        _azureAiSearchEndpoint = configuration["KernelMemory:Services:AzureAISearch:Endpoint"];
        _apiKey = configuration["KernelMemory:Services:AzureAISearch:APIKey"];
    }

    public async IAsyncEnumerable<TableSchema> SearchDataBaseSchema(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default)
    {
        SearchResult searchResult = null;
        string[] debugContent = null;

        try
        {
            var searchTablesSchemaPrompt = $"Please retrive all tables schema that will be helpful for writing sql-t query for Azure SQL Database for user prompt: \"{userPrompt}\"";

            var filter = CreateFilter(dataBaseSearchConfig);

            searchResult = await _memory.SearchAsync(
                               searchTablesSchemaPrompt,
                               index: MemoryManagerService.DataBaseSchemaIndex,
                               filter: filter,
                               limit: dataBaseSearchConfig.TableCount,
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

    private MemoryFilter CreateFilter(DataBaseSearchConfig dataBaseSearchConfig)
    {
        var filter = new MemoryFilter();

        if (dataBaseSearchConfig?.ServerFilter?.ServerName != null)
        {
            filter.Add(TagsKeys.Server, dataBaseSearchConfig.ServerFilter.ServerName);
        }

        if (dataBaseSearchConfig?.DatabaseFilter != null && dataBaseSearchConfig.DatabaseFilter.Any())
        {
            filter.Add(TagsKeys.Database, dataBaseSearchConfig.DatabaseFilter);
        }

        if (dataBaseSearchConfig?.TableFilter != null && dataBaseSearchConfig.TableFilter.Any())
        {
            filter.Add(TagsKeys.Table, dataBaseSearchConfig.TableFilter);
        }

        return filter;
    }

    public async IAsyncEnumerable<Example> SearchExamples(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default)
    {
        SearchResult searchResult = null;
        string[] debugContent = null;

        try
        {
            var searchExamplesPrompt = $"Please retrieve all examples that will be helpful for the user prompt: \"{userPrompt}\"";

            var filter = CreateFilter(dataBaseSearchConfig);

            searchResult = await _memory.SearchAsync(
                           searchExamplesPrompt,
                           index: MemoryManagerService.ExamplesIndex,
                           filter: filter,
                           limit: dataBaseSearchConfig.ExamplesCount,
                           cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while searching for examples in DocAssistant memory");
            debugContent = new[] { e.ToString() };
        }

        if (debugContent != null)
        {
            foreach (var error in debugContent)
            {
                // TODO: Yield return error  
            }
            yield break;
        }

        if (searchResult.Results == null || !searchResult.Results.Any())
        {
            yield break;
        }

        foreach (var document in searchResult.Results)
        {
            var exampleContent = await _documentStorageService.GetDocumentContent(document.DocumentId, document.SourceName, MemoryManagerService.ExamplesIndex);
            var tags = document.Partitions.First().Tags;
            var server = tags.GetTagValue(TagsKeys.Server);

            tags.TryGetValue(TagsKeys.Database, out var databases);
            tags.TryGetValue(TagsKeys.Table, out var tables);

            var sqlExample = tags.GetTagValue(TagsKeys.SqlExample);

            yield return new Example
            {
                DocumentId = document.DocumentId,
                ServerName = server,
                DatabaseNames = databases,
                TableNames = tables,
                SqlExample = sqlExample,
                UserPromptExample = exampleContent,
            };
        }
    }

    public async IAsyncEnumerable<TableSchema> GetAllSchemas()
    {
        var searchClient = new SearchClient(
            new Uri(_azureAiSearchEndpoint),
            MemoryManagerService.DataBaseSchemaIndex,
            new AzureKeyCredential(_apiKey));

        // Define the search options  
        var options = new SearchOptions
        {
            Size = 1000, // Adjust the size as needed, max is 1000 per request  
            Select = { "*" } // Select all fields  
        };

        SearchResults<SearchDocument> results = null;
        try
        {
            results = await searchClient.SearchAsync<SearchDocument>("*", options);
        }
        catch (RequestFailedException ex)
        {
            // Handle specific Azure Search exceptions  
            _logger.LogError($"Azure Search error: {ex.Message}");
            yield break;
        }

        if (results == null)
        {
            yield break;
        }

        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            string serverName = null;
            string databaseName = null;
            string tableName = null;
            string schema = null;
            string connectionString = null;

            if (result.Document.TryGetValue("tags", out var tags))
            {
                var tagsArray = ((object[])tags).Skip(6).Cast<string>().ToArray();
                serverName = tagsArray.GetTagValue(TagsKeys.Server);
                databaseName = tagsArray.GetTagValue(TagsKeys.Database);
                tableName = tagsArray.GetTagValue(TagsKeys.Table);
                connectionString = tagsArray.GetTagValue(TagsKeys.ConnectionString);
            }

            if (result.Document.TryGetValue("payload", out var payload))
            {
                try
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
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors  
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
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

    public async IAsyncEnumerable<Example> GetAllExamples()
    {
        var searchClient = new SearchClient(
            new Uri(_azureAiSearchEndpoint),
            MemoryManagerService.ExamplesIndex,
            new AzureKeyCredential(_apiKey));

        var options = new SearchOptions
        {
            Size = 1000,
            Select = { "*" }
        };

        SearchResults<SearchDocument> results = null;
        try
        {
            results = await searchClient.SearchAsync<SearchDocument>("*", options);
        }
        catch (RequestFailedException ex)
        {
            // Handle specific Azure Search exceptions  
            Console.WriteLine($"Azure Search error: {ex.Message}");
            yield break;
        }

        if (results == null)
        {
            yield break;
        }

        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            string serverName = null;
            IEnumerable<string> databaseName = null;
            IEnumerable<string> tableName = null;
            string userPromptExample = null;
            string sqlExample = null;

            if (result.Document.TryGetValue("tags", out var tags))
            {
                var tagsArray = ((object[])tags).Skip(6).Cast<string>().ToArray();
                sqlExample = tagsArray.GetTagValue(TagsKeys.SqlExample);
                serverName = tagsArray.GetTagValue(TagsKeys.Server);
                databaseName = tagsArray.GetTagValues(TagsKeys.Database);
                tableName = tagsArray.GetTagValues(TagsKeys.Table);
            }

            if (result.Document.TryGetValue("payload", out var payload))
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(payload.ToString());
                    JsonElement root = document.RootElement;
                    if (root.TryGetProperty("text", out JsonElement textElement))
                    {
                        userPromptExample = textElement.GetString();
                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors  
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                }
            }

            yield return new Example
            {
                DocumentId = result.Document["id"].ToString(),
                ServerName = serverName,
                DatabaseNames = databaseName,
                TableNames = tableName?.ToList(), // Ensure tableName is not null before calling ToList()  
                SqlExample = sqlExample,
                UserPromptExample = userPromptExample,
            };
        }
    }

}
