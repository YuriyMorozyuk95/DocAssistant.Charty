// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

namespace DocAssistant.Charty.Ai.Services;

public interface IMemorySearchService
{
    IAsyncEnumerable<SupportingContent> SearchDocument(string userPrompt, int supportingContentCount);
    IAsyncEnumerable<string> SearchDataWarehouseSchema(string userPrompt, int supportingContentCount, string tableType, CancellationToken cancellationToken = default);
}

public class MemorySearchService : IMemorySearchService
{
    private readonly IDocumentStorageService _documentStorageService;

    private readonly ILogger<MemorySearchService> _logger;

    private readonly MemoryServerless _memory;

    public MemorySearchService(MemoryServerless memory, IDocumentStorageService documentStorageService, ILogger<MemorySearchService> logger)
    {
        _memory = memory;
        _documentStorageService = documentStorageService;
        _logger = logger;
    }

    public async IAsyncEnumerable<SupportingContent> SearchDocument(string userPrompt, int supportingContentCount)
    {
        SearchResult searchResult = null;
        List<SupportingContent> debugContent = null;

        try
        {
            searchResult = await _memory.SearchAsync(userPrompt, limit: supportingContentCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while searching in DocAssistant memory");
            debugContent =
            [
                new SupportingContent
                {
                    IsDebug = true,
                    Title = "Error",
                    Content = e.ToString(),
                }

            ];
        }

        if(debugContent != null)
        {
            foreach(var error in debugContent)
            {
                yield return error;
            }

            yield break;
        }

        if (searchResult.Results == null || !searchResult.Results.Any())  
        {  
            yield break;  
        }  
  
        foreach (var document in searchResult.Results)  
        {  
            var partitions = document?.Partitions;  
  
            if (partitions.Count == 0)  
            {  
                continue;  
            }  
  
            foreach (var partition in partitions)  
            {  
                var supportingContent = new SupportingContent  
                                        {  
                                            Title = document.SourceName,  
                                            Content = partition.Text,  
                                            OriginUri = _documentStorageService.GetDocumentUrl(document.DocumentId, document.SourceName),
                                            DocumentId = document.DocumentId,  
                                        };  
  
                yield return supportingContent;  
            }  
        }  
    }

    public async IAsyncEnumerable<string> SearchDataWarehouseSchema(string userPrompt, int supportingContentCount, string tableType, CancellationToken cancellationToken = default)
    {
        SearchResult searchResult = null;
        string[] debugContent = null;

        try
        {
            searchResult = await _memory.SearchAsync(
                               userPrompt,
                               index: MemoryManagerService.DataWarehouseSchemaIndex,
                               filter: MemoryFilters.ByTag(TagsKeys.TableType, tableType),
                               limit: supportingContentCount,
                               cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while searching in DocAssistant memory");
            debugContent = [e.ToString()];
        }

        if(debugContent != null)
        {
            foreach(var error in debugContent)
            {
                yield return error;
            }

            yield break;
        }

        if (searchResult.Results == null || !searchResult.Results.Any())  
        {  
            yield break;  
        }  
  
        foreach (var document in searchResult.Results)  
        {  
            var tableSchema = await _documentStorageService.GetDocumentContent(document.DocumentId, document.SourceName, MemoryManagerService.DataWarehouseSchemaIndex);

            yield return tableSchema;
        }  
    }


}
