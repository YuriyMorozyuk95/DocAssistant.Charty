﻿using DocAssistant.Charty.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

using Shared.Models;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground;

public class DocAssistantChatServiceTest : IClassFixture<WebApplicationFactory<Program>>  
{  
    private readonly DocAssistantChatService _docAssistantChatService;  
    private readonly ITestOutputHelper _testOutputHelper;  
  
    public DocAssistantChatServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)  
    {  
        _testOutputHelper = testOutputHelper;  
        using var scope = factory.Services.CreateScope();  
        _docAssistantChatService = scope.ServiceProvider.GetService<IDocAssistantChatService>() as DocAssistantChatService;  
    }  
  
    [Fact]  
    public async Task Ask_ShouldReturnChatAppResponse_WhenValidInputIsProvided()  
    {  
        // Arrange  
        var history = new[]  
                      {  
                          new ChatMessage("user", "List all customers' names and their emails."),  
                      };  
  
        var overrides = new RequestOverrides();  
        var cancellationToken = CancellationToken.None;  
  
        // Act  
        var response = await _docAssistantChatService.Ask(history, overrides, cancellationToken);  
  
        // Output  
        _testOutputHelper.WriteLine("Response received from DocAssistantChatService:");  
        _testOutputHelper.WriteLine($"Number of Choices: {response.Choices.Length}");  
        foreach (var choice in response.Choices)  
        {  
            _testOutputHelper.WriteLine($"Message Content: {choice.Message.Content}");  
        }  
    }  
}
