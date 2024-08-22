// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

namespace DocAssistant.Charty.Ai.Services;

public interface IMemoryManagerService
{
}

public class MemoryManagerService : IMemoryManagerService
{
    public const string DataWarehouseSchemaIndex = "datawarehouse-schema-index";

    private readonly string _containerName;

    private readonly SearchIndexClient _searchIndexClient;

    private readonly BlobServiceClient _blobServiceClient;

    private readonly MemoryServerless _memory;

    public MemoryManagerService(
        MemoryServerless memory,
        IConfiguration configuration,
        SearchIndexClient searchIndexClient,
        BlobServiceClient blobServiceClient)
    {
        _memory = memory;
        _searchIndexClient = searchIndexClient;
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["KernelMemory:Services:AzureBlobs:Container"];
    }

    public async Task UploadMemoryAsync(
        string id,
        string fileName,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        Stream adaptedStream = null;
        string adaptedName = fileName;

        try
        {
            await CreateContainerIfNotExistAsync();



            var tags = new TagCollection
                            {
                                { TagsKeys.File, adaptedName },
                            };

            var document = new Document(id, tags);
            document.AddStream(adaptedName, adaptedStream);

            GlobalStatus.CurrentOrLastDocumentId = id;
            await _memory.ImportDocumentAsync(
                document,
                steps: new[]
                        {
                                Constants.PipelineStepsExtract,
                                Constants.PipelineStepsPartition,
                                "gen_embeddings_parallel",
                                Constants.PipelineStepsSaveRecords
                        },
                cancellationToken: cancellationToken);

        }
        catch (Exception e)
        {
            GlobalStatus.LastIndexErrorMessage = e.ToString();
        }
        finally
        {
            if (stream != null)
            {
                await stream.DisposeAsync();
            }

            if (adaptedStream != null)
            {
                await adaptedStream.DisposeAsync();
            }
        }
    }

    public async Task<string> UploadToDataWarehouseSchemaMemoryAsync(
        string record,
        string type,
        CancellationToken cancellationToken = default)
    {
        await CreateContainerIfNotExistAsync();

        var id = Guid.NewGuid().ToString();

        var tag = new TagCollection
            {
                { TagsKeys.TableType, type }
            };

        return await _memory.ImportTextAsync(
            record,
            documentId: id,
            tags: tag,
            steps: new[]
                    {
                                Constants.PipelineStepsExtract,
                                Constants.PipelineStepsPartition,
                                "gen_embeddings_parallel",
                                Constants.PipelineStepsSaveRecords
                    },
            index: DataWarehouseSchemaIndex,
            cancellationToken: cancellationToken);
    }

    public async Task CreateContainerIfNotExistAsync()
    {
        // Create the container client.    
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Create the container if it does not exist.  
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
    }

    public async Task RemoveMemoryAsync()
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.DeleteIfExistsAsync();

        var indexes = await _memory.ListIndexesAsync();
        foreach (var index in indexes)
        {
            await _searchIndexClient.DeleteIndexAsync(index.Name);
        }
    }

    public async Task RemoveDataWarehouseSchemaIndexAsync()
    {
        await _searchIndexClient.DeleteIndexAsync(DataWarehouseSchemaIndex);
    }
}
