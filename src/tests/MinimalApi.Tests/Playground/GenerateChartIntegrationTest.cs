﻿// Copyright (c) Microsoft. All rights reserved.

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

    }

    [Fact]
    public async Task TestAsync()
    {
        HttpClient.DefaultProxy = new WebProxy()
                                  {
                                      BypassProxyOnLocal = false,
                                      UseDefaultCredentials = true
                                  };

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
