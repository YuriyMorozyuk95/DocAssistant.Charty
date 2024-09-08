

using System.Diagnostics;
using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Experimental.Agents;

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

    public GenerateChartIntegrationTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;

        _azureOpenAiEndpoint = "https://hack-rag-openai.openai.azure.com/";
        _azureApiKey = "8dd7b47322e448fcb6237fa77f26713b";
        _deploymentName = "hack-gpt4o";

        _fileService = new OpenAIFileService(new Uri(_azureOpenAiEndpoint), apiKey: _azureApiKey);
        HttpClient.DefaultProxy = new WebProxy()
                                  {
                                      BypassProxyOnLocal = false,
                                      UseDefaultCredentials = true
                                  };
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

            var prompt = "List all customers' names and their emails.";
            var table ="""
                | CustomerName | Email |    
                | --- | --- |    
                | John Doe | john.doe@example.com |    
                | Jane Smith | jane.smith@example.com |    
                | Alice Johnson | alice.johnson@example.com |    
                | Liam Scott | liam.scott@example.com |    
                | Emma Green | emma.green@example.com |    
                | Noah Harris | noah.harris@example.com |    
                | Ava Thompson | ava.thompson@example.com |    
                | William Martinez | william.martinez@example.com |    
                | Sophia Robinson | sophia.robinson@example.com |    
                | James Clark | james.clark@example.com |    
                | Mia Rodriguez | mia.rodriguez@example.com |    
                | Benjamin Lee | benjamin.lee@example.com |    
                | Isabella Walker | isabella.walker@example.com |    
                | Lucas Hall | lucas.hall@example.com |    
                | Charlotte Allen | charlotte.allen@example.com |    
                | Henry Young | henry.young@example.com |    
                | Amelia King | amelia.king@example.com |    
                | Alexander Wright | alexander.wright@example.com |    
                | Evelyn Scott | evelyn.scott@example.com |    
                | Elijah Adams | elijah.adams@example.com |    
                | Abigail Baker | abigail.baker@example.com |    
                | Oliver Nelson | oliver.nelson@example.com |    
                | Emily Carter | emily.carter@example.com |    
                | Jacob Mitchell | jacob.mitchell@example.com |    
                | Harper Perez | harper.perez@example.com |    
                | Michael Roberts | michael.roberts@example.com |    
                | Ella Turner | ella.turner@example.com |    
                | Daniel Phillips | daniel.phillips@example.com |    
                | Grace Campbell | grace.campbell@example.com |    
                | Matthew Parker | matthew.parker@example.com |    
                | Chloe Evans | chloe.evans@example.com |    
                | Sebastian Edwards | sebastian.edwards@example.com |    
                | Avery Collins | avery.collins@example.com |
                """;

            var thread = await agent.NewThreadAsync();

            await InvokeAgentAsync(
                agent,
                thread,
                "1-first", $"Please create chart based on user prompt: {prompt} and  on data: {table}");

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
}
