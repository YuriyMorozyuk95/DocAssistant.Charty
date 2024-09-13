using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shared.Models;

using Xunit;

namespace MinimalApi.Tests.Playground;

public class IntentServiceTests : IClassFixture<WebApplicationFactory<Program>>  
{  
    private readonly WebApplicationFactory<Program> _factory;  
    private readonly IIntentService _intentService;  
  
    public IntentServiceTests(WebApplicationFactory<Program> factory)  
    {  
        _factory = factory;  
  
        using var scope = _factory.Services.CreateScope();  
        _intentService = scope.ServiceProvider.GetRequiredService<IIntentService>();  
    }  
  
    [Theory]  
    [InlineData("List all customers' names and their emails.", Intent.Query)]  
    [InlineData("Insert new customer record.", Intent.Insert)]  
    [InlineData("Create a new table for orders.", Intent.Create)]  
    [InlineData("Unrecognized operation.", Intent.Default)]  
    public async Task CheckIntent_Should_ReturnCorrectIntent(string input, Intent expectedIntent)  
    {  
        // Act  
        var result = await _intentService.CheckIntent(input);  
  
        // Assert  
        Assert.Equal(expectedIntent, result);  
    }  
}
