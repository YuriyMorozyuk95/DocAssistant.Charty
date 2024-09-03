

using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Experimental.Agents;
using Shared.Models;

namespace MinimalApi.Tests.Playground;

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

public class GenerateChartIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly OpenAIFileService _fileService;

    private readonly string _azureOpenAiEndpoint;

    private readonly string _azureApiKey;

    private readonly string _deploymentName;

    private readonly string _containerName;

    private readonly BlobContainerClient _containerClient;

    public GenerateChartIntegrationTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;

        _azureOpenAiEndpoint = "https://hack-rag-openai.openai.azure.com/";
        _azureApiKey = "8dd7b47322e448fcb6237fa77f26713b";
        _deploymentName = "hack-gpt4o";
        _containerName = "images";
        using var scope = factory.Services.CreateScope();
        BlobServiceClient? blobServiceClient = scope.ServiceProvider.GetService<BlobServiceClient>();
        _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

        _fileService = new OpenAIFileService(new Uri(_azureOpenAiEndpoint), apiKey: _azureApiKey);

    }

    [Fact]
    public async Task TestAsync()
    {
       

        var agent = await new AgentBuilder()
            .WithAzureOpenAIChatCompletion(_azureOpenAiEndpoint, _deploymentName, _azureApiKey)
            .WithCodeInterpreter()
            .BuildAsync();

        try
        {
            var thread = await agent.NewThreadAsync();

            await InvokeAgentAsync(
                agent,
                thread,
                "1-first", @"
Display this data using a bar-chart with no summation:

Banding  Brown Pink Yellow  Sum
X00000   339   433     126  898
X00300    48   421     222  691
X12345    16   395     352  763
Others    23   373     156  552
Sum      426  1622     856 2904
");

            await InvokeAgentAsync(agent, thread, "2-colors", "Can you regenerate this same chart using the category names as the bar colors?");
            await InvokeAgentAsync(agent, thread, "3-line", "Can you regenerate this as a line chart?");
        }
        finally
        {
            await agent.DeleteAsync();
        }
    }

    private async Task InvokeAgentAsync(IAgent agent,IAgentThread thread, string imageName, string question)
    {
        await foreach (var message in thread.InvokeAsync(agent, question))
        {
            if (message.ContentType == ChatMessageType.Image)
            {
                var filename = $"{imageName}.jpg";
                var path = Path.Combine(Environment.CurrentDirectory, filename);
                _testOutputHelper.WriteLine($"# {message.Role}: {message.Content}");
                _testOutputHelper.WriteLine($"# {message.Role}: {path}");
                SupportingImageRecord imageRecord = await UploadImageAsync(path);

                var content = await _fileService.GetFileContentAsync(message.Content);
                await using var outputStream = File.OpenWrite(filename);
                await outputStream.WriteAsync(content.Data!.Value);

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C start {path}"
                    });
            }
            else
            {
                _testOutputHelper.WriteLine($"# {message.Role}: {message.Content}");
            }
        }

        _testOutputHelper.WriteLine("");
    }

    public async Task<SupportingImageRecord> UploadImageAsync(string filePath)
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);

        string fileName = Path.GetFileName(filePath);

        // Отримуємо BlobClient для нашого файлу  
        BlobClient blobClient = _containerClient.GetBlobClient(fileName);
        string fileUrl = blobClient.Uri.ToString();

        // Завантажуємо файл  
        await blobClient.UploadAsync(filePath, overwrite: true);

        return new SupportingImageRecord(fileName, fileUrl);
    }
}
