

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage;

namespace DocAssistant.Charty.Ai.Services;

public interface IDocumentStorageService
{
    string GetDocumentUrl(string documentId, string documentName, string index = "default");
    Task<string> GetDocumentContent(string documentId, string documentName, string index = "default");
    
}
internal class DocumentStorageService : IDocumentStorageService
{
    private readonly MemoryServerless _memoryServerless;

    private readonly ILogger<DocumentStorageService> _logger;

    private readonly BlobContainerClient _containerClient;

    public DocumentStorageService(IConfiguration configuration, BlobServiceClient blobServiceClient, MemoryServerless memoryServerless, ILogger<DocumentStorageService> logger)
    {
        _memoryServerless = memoryServerless;
        _logger = logger;
        var containerName = configuration["KernelMemory:Services:AzureBlobs:Container"];

        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> GetDocumentContent(string documentId, string documentName, string index = "default")  
    {  
        var relativePath = $"{index}/{documentId}/{documentName}";  
  
        // Get a reference to a blob  
        BlobClient blobClient = GetBlobClient(relativePath);  
  
        // Download the blob's content as a string  
        BlobDownloadInfo download = await blobClient.DownloadAsync();  
  
        using (StreamReader reader = new StreamReader(download.Content))  
        {  
            string content = await reader.ReadToEndAsync();  
            return content;  
        }  
    }  
    public string GetDocumentUrl(string documentId, string documentName, string index = "default")
    {
        var relativePath = $"{index}/{documentId}/{documentName}";

        // Get a reference to a blob  
        BlobClient blobClient = _containerClient.GetBlobClient(relativePath);

        // Get the blob URL  
        string blobUrl = blobClient.Uri.AbsoluteUri;

        return blobUrl;
    }

    public async Task WriteFileAsync(
        string documentId,
        string fileName,
        Stream streamContent,
        string fileType,
        string index = "default",
        CancellationToken cancellationToken = default)
    {
        var blobName = $"{index}/{documentId}/{fileName}";

        BlobClient blobClient = GetBlobClient(blobName);

        BlobUploadOptions options = new();
        BlobLeaseClient blobLeaseClient = null;
        BlobLease lease = null;

        if (await blobClient.ExistsAsync(cancellationToken))
        {
            blobLeaseClient = GetBlobLeaseClient(blobClient);
            lease = await LeaseBlobAsync(blobLeaseClient, cancellationToken);
            options = new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = lease.LeaseId } };
        }

        options.HttpHeaders = new BlobHttpHeaders { ContentType = fileType };

        _logger.LogTrace("Writing blob {0} ...", blobName);

        streamContent.Seek(0, SeekOrigin.Begin);
        await blobClient.UploadAsync(streamContent, options, cancellationToken);
        var size = streamContent.Length;
				

        if (size == 0)
        {
            _logger.LogWarning("The file {0} is empty", blobName);
        }

        if (lease != null && blobLeaseClient != null)
        {
            _logger.LogTrace("Releasing blob {0} ...", blobLeaseClient.Uri);
            await blobLeaseClient
                .ReleaseAsync(new BlobRequestConditions { LeaseId = lease.LeaseId }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _logger.LogTrace("Blob released {0} ...", blobLeaseClient.Uri);
        }

        _logger.LogTrace("Blob {0} ready, size {1}", blobName, size);
    }

   

    private BlobClient GetBlobClient(string blobName)
    {
        BlobClient? blobClient = _containerClient.GetBlobClient(blobName);
        if (blobClient == null)
        {
            throw new DocumentStorageException("Unable to instantiate Azure Blob blob client");
        }

        return blobClient;
    }

    private BlobLeaseClient GetBlobLeaseClient(BlobClient blobClient)
    {
        var blobLeaseClient = blobClient.GetBlobLeaseClient();
        if (blobLeaseClient == null)
        {
            throw new DocumentStorageException("Unable to instantiate Azure blob lease client");
        }

        return blobLeaseClient;
    }

    private async Task<BlobLease> LeaseBlobAsync(BlobLeaseClient blobLeaseClient, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Leasing blob {0} ...", blobLeaseClient.Uri);

        Response<BlobLease> lease = await blobLeaseClient
                                        .AcquireAsync(TimeSpan.FromSeconds(30), cancellationToken: cancellationToken)
                                        .ConfigureAwait(false);
        if (lease == null || !lease.HasValue)
        {
            throw new DocumentStorageException("Unable to lease blob");
        }

        _logger.LogTrace("Blob {0} leased", blobLeaseClient.Uri);

        return lease.Value;
    }
}
