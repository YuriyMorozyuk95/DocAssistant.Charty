﻿using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

using Shared.Models;
using System.ComponentModel;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IMemoryManagerService
{
    Task<TableSchema> UploadTableSchemaToMemory(
        TableSchema tableSchema,
        CancellationToken cancellationToken = default);


    Task<string> UploadExampleToMemory(
        Example example,
        CancellationToken cancellationToken = default);

    Task RemoveDataBaseSchemaIndex();

    Task RemoveExampleSchemaIndex();
}

public class MemoryManagerService : IMemoryManagerService
{
    public const string DataBaseSchemaIndex = "database-schema-index";
    public const string ExamplesIndex = "examples-index";

    private readonly string _containerName;

    private readonly BlobServiceClient _blobServiceClient;

    private readonly MemoryServerless _memory;

    public MemoryManagerService(
        MemoryServerless memory,
        IConfiguration configuration,
        IExampleService exampleService,
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
        try
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
        catch (Exception e)
        {
            throw;
        }
    }

    public async Task<string> UploadExampleToMemory(
        Example example,
        CancellationToken cancellationToken = default)
    {
        await CreateContainerIfNotExistAsync();

        var id = Guid.NewGuid().ToString();

        var tag = new TagCollection
                  {
                      { TagsKeys.SqlExample, example.SqlExample },
                  };

        if (example.ServerName != null)
        {
            tag.Add(TagsKeys.Server, example.ServerName);
        }
        if (example.DatabaseNames.Any())
        {
            tag.Add(TagsKeys.Database, example.DatabaseNames.ToList());
        }
        if (example.TableNames.Any())
        {
            tag.Add(TagsKeys.Table, example.TableNames.ToList());
        }

        await _memory.ImportTextAsync(example.UserPromptExample,
                                      documentId: id,
                                      tags: tag,
                                      steps: new[]
                                             {
                                                 Constants.PipelineStepsExtract,
                                                 Constants.PipelineStepsPartition,
                                                 "gen_embeddings_parallel",
                                                 Constants.PipelineStepsSaveRecords
                                             },
                                      index: ExamplesIndex,
                                      cancellationToken: cancellationToken);

        return id;
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

    public async Task RemoveExampleSchemaIndex()
    {
        await _memory.DeleteIndexAsync(ExamplesIndex);
    }
}
