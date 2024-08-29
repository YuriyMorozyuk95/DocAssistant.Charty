

using Microsoft.AspNetCore.Mvc.Testing;

using MinimalApi;

using Shared.Models;

using SharedWebComponents.Services;

using Xunit;
using Xunit.Abstractions;

public class ChatIntegrationTests : IClassFixture<WebApplicationFactory<Program>>  
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly ApiClient _client;  
  
    public ChatIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)  
    {
        _testOutputHelper = testOutputHelper;
        var httpClient = factory.CreateClient();  
        _client = new ApiClient(httpClient);  
    }  
  
    [Fact]  
    public async Task Post_Chat_ReturnsOkResponse()  
    {  
        // Arrange  
        var history = new ChatMessage[]  
                      {  
                          new ChatMessage("user", "Could you remove pet in store with id 11?")  
                      };  
        var request = new ChatRequest(history, null);  
  
        // Act  
        var response = await _client.ChatConversation(request);  
  
        // Assert  
        Assert.NotNull(response);  
        Assert.True(response.IsSuccessful);  
        Assert.NotNull(response.Response);  
        Assert.NotEmpty(response.Response.Choices);  
        Assert.All(response.Response.Choices, choice => Assert.NotNull(choice.Message));

        // Print the result  
        var result = response.Response;  
        foreach (var choice in result.Choices)  
        {  
            _testOutputHelper.WriteLine($"Role: {choice.Message.Role}");  
            _testOutputHelper.WriteLine($"Content: {choice.Message.Content}");  
        }  
    }  
} 
