using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IMemoryManagerService
{
    Task<TableSchema> UploadTableSchemaToMemory(
        TableSchema tableSchema,
        CancellationToken cancellationToken = default);

    Task RemoveDataBaseSchemaIndex();

    //TODO add upload sql example to memory
    //Task<TableSchema> UploadDataBaseSchemaToMemory(
    //    TableSchema tableSchema,
    //    CancellationToken cancellationToken = default);

}

public class MemoryManagerService : IMemoryManagerService
{
    public const string DataBaseSchemaIndex = "database-schema-index";

    private readonly string _containerName;

    private readonly BlobServiceClient _blobServiceClient;

    private readonly MemoryServerless _memory;

    public MemoryManagerService(
        MemoryServerless memory,
        IConfiguration configuration,
        BlobServiceClient blobServiceClient)
    {
        _memory = memory;
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["KernelMemory:Services:AzureBlobs:Container"];
    }

    public async Task<TableSchema> UploadTableSchemaToMemory(
       TableSchema tableSchema,
        CancellationToken cancellationToken = default)
    {
        await CreateContainerIfNotExistAsync();

        var id = Guid.NewGuid().ToString();

        var tag = new TagCollection
            {
                { TagsKeys.Server, tableSchema.ServerName },
                { TagsKeys.Database, tableSchema.DatabaseName },
                { TagsKeys.Table, tableSchema.TableName },
                { TagsKeys.ConnectionString, tableSchema.ConnectionString }
            };

        tableSchema.DocumentId = await _memory.ImportTextAsync(
                          tableSchema.Schema,
                          documentId: id,
                          tags: tag,
                          steps: new[]
                                 {
                                     Constants.PipelineStepsExtract,
                                     Constants.PipelineStepsPartition,
                                     "gen_embeddings_parallel",
                                     Constants.PipelineStepsSaveRecords
                                 },
                          index: DataBaseSchemaIndex,
                          cancellationToken: cancellationToken);

        return tableSchema;
    }


    public async Task CreateContainerIfNotExistAsync()
    {
        // Create the container client.    
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Create the container if it does not exist.  
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
    }

    public async Task RemoveDataBaseSchemaIndex()
    {
        await _memory.DeleteIndexAsync(DataBaseSchemaIndex);
    }
}
