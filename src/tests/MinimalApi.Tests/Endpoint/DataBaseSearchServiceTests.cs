// Copyright (c) Microsoft. All rights reserved.

using DocAssistant.Charty.Ai.Services.Search;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shared.Models;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Endpoint;

public class DataBaseSearchServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly DataBaseSearchService _dataBaseSearchService;

    public DataBaseSearchServiceTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var scope = factory.Services.CreateScope();
        _dataBaseSearchService = scope.ServiceProvider.GetRequiredService<IDataBaseSearchService>() as DataBaseSearchService;
    }

    [Fact]
    public async Task ShouldRetrieveDatabaseSchema()
    {
        var userPrompt = "List all customers' names and their emails.";
        var tableCount = 5;

        var result = await _dataBaseSearchService.GetSchemas(userPrompt, tableCount);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);

        foreach (var schema in result)
        {
            _testOutputHelper.WriteLine($"ConnectionString: {schema.Key}");
            _testOutputHelper.WriteLine($"Schema: {schema.Value}");
            _testOutputHelper.WriteLine("-----");
        }
    }

    [Theory]
    [InlineData("List all customers' names and their emails.")]
    [InlineData("Retrieve all orders placed by customer with ID 5.")]
    [InlineData("Show details of all products with their names and prices.")]
    [InlineData("Find the order details for order ID 10.")]
    [InlineData("List all suppliers and their contact names.")]
    [InlineData("Get the list of all firewall rules.")]
    [InlineData("Show the products supplied by supplier with ID 3.")]
    [InlineData("Retrieve the names and phone numbers of all customers.")]
    [InlineData("Get the total quantity of products ordered in order ID 15.")]
    [InlineData("List all orders along with the customer names who placed them.")]
    public async Task ShouldTranslatePromptToSql(string input)
    {
        var count = 10;
        var schemaResult = await _dataBaseSearchService.GetSchemas(input, 3);
        var schema = schemaResult.Values.First();
        var result = await _dataBaseSearchService.TranslatePromptToQuery(input, schema, count, default);

        _testOutputHelper.WriteLine("Generated SQL Query:");
        _testOutputHelper.WriteLine(result);
        _testOutputHelper.WriteLine("Schema:");
        _testOutputHelper.WriteLine(schema);
    }

    [Theory]
    [InlineData("List all customers' names and their emails.")]
    [InlineData("Retrieve all orders placed by customer with ID 5.")]
    [InlineData("Show details of all products with their names and prices.")]
    [InlineData("Find the order details for order ID 10.")]
    [InlineData("List all suppliers and their contact names.")]
    [InlineData("Get the list of all firewall rules.")]
    [InlineData("Show the products supplied by supplier with ID 3.")]
    [InlineData("Retrieve the names and phone numbers of all customers.")]
    [InlineData("Get the total quantity of products ordered in order ID 15.")]
    [InlineData("List all orders along with the customer names who placed them.")]
    public async Task CanSearchAndReturnSupportingContent(string userPrompt)
    {
        var config = new DataBaseSearchConfig
        {
            TableCount = 10,
            ResultsNumberLimit = 100,
        };

        var result = await _dataBaseSearchService.Search(userPrompt, config);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);

        foreach (var content in result)
        {
            _testOutputHelper.WriteLine($"Title: {content.Title}");
            _testOutputHelper.WriteLine($"Content: {content.Content}");
            _testOutputHelper.WriteLine($"IsDebug: {content.IsDebug}");
            _testOutputHelper.WriteLine($"SupportingContentType: {content.SupportingContentType}");
            _testOutputHelper.WriteLine("-----");
        }
    }
}
