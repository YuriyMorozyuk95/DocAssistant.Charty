﻿

using DocAssistant.Charty.Ai.Services;
using DocAssistant.Charty.Ai.Services.Database;
using DocAssistant.Charty.Ai.Services.Search;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        api.MapPost("chat", OnPostChatAsync);

        api.MapPost("upload-database-schema", OnPostUploadDatabaseSchemaAsync);

        api.MapGet("get-all-database-schema", OnGetAllDatabaseSchemaAsync);

        api.MapPost("upload-example", OnPostUploadExampleAsync);

        api.MapGet("get-all-examples", OnGetAllExamplesAsync);

        api.MapGet("get-all-servers", OnGetAllServersAsync);

        return app;
    }

    private static async Task<IResult> OnPostChatAsync(
        [FromBody] ChatRequest request,
        [FromServices] IDocAssistantChatService chatService,
        CancellationToken cancellationToken)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.Ask(
                request.History, request.Overrides, cancellationToken);

            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }


    private static async IAsyncEnumerable<string> OnPostUploadDatabaseSchemaAsync(
        [FromBody] UploadDatabaseSchemaRequest request,
        [FromServices] IAzureSqlSchemaGenerator azureSqlSchemaGenerator,
        [FromServices] IMemoryManagerService memoryManagerService,
        [FromServices] IDataBaseRegistryService dataBaseRegistryService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tables = azureSqlSchemaGenerator.GetTableNamesDdlSchemas(request.ConnectionString);

        yield return $"Uploading Server info";
        var server = await dataBaseRegistryService.AddNewServer(tables);
        yield return $"Server info {server.ServerName} uploaded";

        foreach (var table in tables)
        {
            yield return $"Uploading Table: {table.TableName}";

            var uploadedTable = await memoryManagerService.UploadTableSchemaToMemory(table, cancellationToken);

            if (uploadedTable.DocumentId != null)
            {
                yield return $"Uploaded DocumentId: {uploadedTable.DocumentId}";
            }
            else
            {
                yield return $"Failed to upload table: {table.TableName}";
            }
        }
    }

    private static async IAsyncEnumerable<TableSchema> OnGetAllDatabaseSchemaAsync(
        [FromServices] IMemorySearchService memorySearchService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var schema in memorySearchService.GetAllSchemas().WithCancellation(cancellationToken))
        {
            yield return schema;
        }
    }

    private static async Task<IResult> OnPostUploadExampleAsync(
        [FromBody] Example example,
        [FromServices] IMemoryManagerService memoryManagerService,
        CancellationToken cancellationToken)
    {
        var documentId = await memoryManagerService.UploadExampleToMemory(example, cancellationToken);
        if (documentId != null)
        {
            return TypedResults.Ok(new { DocumentId = documentId });
        }
        return Results.BadRequest("Failed to upload example.");
    }

    private static async IAsyncEnumerable<Example> OnGetAllExamplesAsync(
        [FromServices] IMemorySearchService memorySearchService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var example in memorySearchService.GetAllExamples().WithCancellation(cancellationToken))
        {
            yield return example;
        }
    }

    private static async Task<IResult> OnGetAllServersAsync(  
        [FromServices] IDataBaseRegistryService dataBaseRegistryService,  
        CancellationToken cancellationToken)  
    {  
        var servers = await dataBaseRegistryService.GetAllServers();  
        return TypedResults.Ok(servers);  
    } 
}
