

using DocAssistant.Charty.Ai.Services;
using DocAssistant.Charty.Ai.Services.Database;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Blazor 📎 Clippy streaming endpoint
        //api.MapPost("openai/chat", OnPostChatPromptAsync);

        //// Long-form chat w/ contextual history endpoint
        api.MapPost("chat", OnPostChatAsync);

        api.MapPost("upload-database-schema", OnPostUploadDatabaseSchemaAsync);

        api.MapGet("get-all-database-schema", OnGetAllDatabaseSchemaAsync);

        //// Get all documents
        //api.MapGet("documents", OnGetDocumentsAsync);

        //// Get DALL-E image result from prompt
        //api.MapPost("images", OnPostImagePromptAsync);

        //api.MapGet("enableLogout", OnGetEnableLogout);

        return app;
    }

    private static IResult OnGetEnableLogout(HttpContext context)
    {
        var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
        var enableLogout = !string.IsNullOrEmpty(header);

        return TypedResults.Ok(enableLogout);
    }

    private static async Task<IResult> OnPostChatAsync(
        [FromBody]ChatRequest request,
        [FromServices]IDocAssistantChatService chatService,
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


    private static async IAsyncEnumerable<DocumentResponse> OnGetDocumentsAsync(
        BlobContainerClient client,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in client.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob is not null and { Deleted: false })
            {
                var props = blob.Properties;
                var baseUri = client.Uri;
                var builder = new UriBuilder(baseUri);
                builder.Path += $"/{blob.Name}";

                var metadata = blob.Metadata;
                var documentProcessingStatus = GetMetadataEnumOrDefault<DocumentProcessingStatus>(
                    metadata, nameof(DocumentProcessingStatus), DocumentProcessingStatus.NotProcessed);
                var embeddingType = GetMetadataEnumOrDefault<EmbeddingType>(
                    metadata, nameof(EmbeddingType), EmbeddingType.AzureSearch);

                yield return new(
                    blob.Name,
                    props.ContentType,
                    props.ContentLength ?? 0,
                    props.LastModified,
                    builder.Uri,
                    documentProcessingStatus,
                    embeddingType);

                static TEnum GetMetadataEnumOrDefault<TEnum>(
                    IDictionary<string, string> metadata,
                    string key,
                    TEnum @default) where TEnum : struct => metadata.TryGetValue(key, out var value)
                        && Enum.TryParse<TEnum>(value, out var status)
                            ? status
                            : @default;
            }
        }
    }

    private static async IAsyncEnumerable<string> OnPostUploadDatabaseSchemaAsync(  
        [FromBody] UploadDatabaseSchemaRequest request,  
        [FromServices] IAzureSqlSchemaGenerator azureSqlSchemaGenerator,  
        [FromServices] IMemoryManagerService memoryManagerService,  
        [EnumeratorCancellation] CancellationToken cancellationToken)  
    {  
        var tables = azureSqlSchemaGenerator.GetTableNamesDdlSchemas(request.ConnectionString);

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
        await foreach (var schema in memorySearchService.CanGetAllSchemas().WithCancellation(cancellationToken))  
        {  
            yield return schema;  
        }  
    }
}
