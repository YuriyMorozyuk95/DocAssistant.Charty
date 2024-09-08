

using System.Net;

using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Search.Documents.Indexes;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
#if DEBUG
        services.AddSingleton<TokenCredential, AzureCliCredential>();
#else
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
#endif

        services.AddSingleton(
            sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    string accountName = config["KernelMemory:Services:AzureBlobs:Account"];
                    string endpointSuffix = config["KernelMemory:Services:AzureBlobs:EndpointSuffix"];

                    string blobEndpoint = $"https://{accountName}.blob.{endpointSuffix}";

                    // Create a BlobServiceClient that will authenticate through Active Directory  
                    var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), sp.GetRequiredService<TokenCredential>());
                    return blobServiceClient;
                });

        services.AddSingleton(
            sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var azureStorageContainer = config["KernelMemory:Services:AzureBlobs:Container"];
                    return sp.GetRequiredService<BlobServiceClient>()
                        .GetBlobContainerClient(azureStorageContainer);
                });

        services.AddSingleton(
            sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var azureSearchServiceEndpoint = config["KernelMemory:Services:AzureAISearch:Endpoint"];

                    var credential = sp.GetRequiredService<TokenCredential>();

                    var searchIndexClient = new SearchIndexClient(
                        new Uri(azureSearchServiceEndpoint!),
                        credential,
                        new SearchClientOptions
                        {
                            Transport = new HttpClientTransport(
                                new HttpClient(
                                    new HttpClientHandler()
                                    {
                                        Proxy = new WebProxy()
                                        {
                                            BypassProxyOnLocal = false,
                                            UseDefaultCredentials = true,
                                        }
                                    }))
                        });

                    return searchIndexClient;
                });

        services.AddSingleton(
            sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var documentIntelligenceEndpoint = config["AzureDocumentIntelligenceEndpoint"];

                    ArgumentNullException.ThrowIfNullOrEmpty(documentIntelligenceEndpoint);

                    var credential = sp.GetRequiredService<TokenCredential>();

                    var documentAnalysisClient = new DocumentAnalysisClient(
                        new Uri(documentIntelligenceEndpoint),
                        credential,
                        new DocumentAnalysisClientOptions
                        {
                            Transport = new HttpClientTransport(
                                new HttpClient(
                                    new HttpClientHandler()
                                    {
                                        Proxy = new WebProxy()
                                        {
                                            BypassProxyOnLocal = false,
                                            UseDefaultCredentials = true,
                                        }
                                    }))
                        });
                    return documentAnalysisClient;
                });


        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
