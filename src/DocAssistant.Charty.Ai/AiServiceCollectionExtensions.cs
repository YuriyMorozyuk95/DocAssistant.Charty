using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;

using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;

using DocAssistant.Charty.Ai.Services;
using DocAssistant.Charty.Ai.Services.Search;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace DocAssistant.Charty.Ai;

public static class AiServiceCollectionExtensions
{
	public static void AddAiServices(this IServiceCollection services)
	{
		services.AddSingleton(
			sp =>
				{
					var config = sp.GetRequiredService<IConfiguration>();
					var azureOpenAiServiceEndpoint = config["KernelMemory:Services:AzureOpenAIText:Endpoint"];

					ArgumentException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

					var credential = sp.GetRequiredService<TokenCredential>();

					//var openAiClient = new OpenAIClient(
					//	,
					//	credential,
					//	new OpenAIClientOptions
					//	{
					//		Transport = new HttpClientPipelineTransport(
					//			new HttpClient(
					//				new HttpClientHandler()
					//				{
					//					Proxy = new WebProxy()
					//							{
					//								BypassProxyOnLocal = false,
					//								UseDefaultCredentials = true,
					//							}
					//				}))
					//	});

                    var openAiClient = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint), credential);

					return openAiClient;
				});

        services.AddSingleton(
            sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var openAiClient = sp.GetRequiredService<OpenAIClient>();

                    var deployedChtGptModelName = config["AzureOpenAiChatGptDeployment"];

                    var kernel = Kernel.CreateBuilder()
                        .AddAzureOpenAIChatCompletion(deployedChtGptModelName, openAiClient)
                        .Build();

                    var path = Path.Combine(AppContext.BaseDirectory, "Plugins", "CodeInterpreter");
                    kernel.ImportPluginFromPromptDirectory(path);

                    //path = Path.Combine(AppContext.BaseDirectory, "Plugins", "DatabasePlugin");
                    //kernel.ImportPluginFromPromptDirectory(path);

                    return kernel;
                });

        services.AddSingleton<MemoryServerless>(
			sp =>
				{
					//Creating new scope with service collection for 
					var kernelMemoryServiceCollection = new ServiceCollection
														{
															services
														};

					//Build config and bind sections
					var config = sp.GetRequiredService<IConfiguration>();
					var azureOpenAiTextConfig = new AzureOpenAIConfig();
					var azureOpenAiEmbeddingConfig = new AzureOpenAIConfig();
					var azureAiSearchConfig = new AzureAISearchConfig();
					var azureBlobConfig = new AzureBlobsConfig();
					var searchClientConfig = new SearchClientConfig();

					config.BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAiTextConfig);
					config.BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAiEmbeddingConfig);
					config.BindSection("KernelMemory:Services:AzureBlobs", azureBlobConfig);
					config.BindSection("KernelMemory:Services:AzureAISearch", azureAiSearchConfig);
					config.BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

					//Build Kernel memory instance
					var memory = new KernelMemoryBuilder(kernelMemoryServiceCollection)
						.WithAzureOpenAITextGeneration(azureOpenAiTextConfig)
						.WithAzureOpenAITextEmbeddingGeneration(azureOpenAiEmbeddingConfig)
						.WithAzureBlobsDocumentStorage(azureBlobConfig)
						.WithAzureAISearchMemoryDb(azureAiSearchConfig)
						.WithSearchClientConfig(searchClientConfig)
						.Build<MemoryServerless>();

					var serviceProvider = kernelMemoryServiceCollection.BuildServiceProvider();

					return memory;
				});



        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddScoped<IMemoryManagerService, MemoryManagerService>();
        services.AddScoped<IMemorySearchService, MemorySearchService>();
        services.AddScoped<IDataBaseSearchService, DataBaseSearchService>();
        services.AddScoped<ISqlExecutorService, SqlExecutorService>();
        services.AddScoped<IDocAssistantChatService, DocAssistantChatService>();

        //services.AddTransient<IDocAssistantChatService, IDocAssistantChatService>();
    }
}
