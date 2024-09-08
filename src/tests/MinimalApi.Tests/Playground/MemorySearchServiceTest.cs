// Copyright (c) Microsoft. All rights reserved.

using DocAssistant.Charty.Ai.Services.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;
using Xunit;
using Shared.Models;
using System.Xml.Serialization;

namespace MinimalApi.Tests.Playground  
{  
    public class MemorySearchServiceTest : IClassFixture<WebApplicationFactory<Program>>  
    {  
        private readonly ITestOutputHelper _output;  
        private readonly IMemorySearchService _memorySearchService;  
  
        public MemorySearchServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper output)  
        {  
            _output = output;  
            using var scope = factory.Services.CreateScope();  
            _memorySearchService = scope.ServiceProvider.GetRequiredService<IMemorySearchService>();  
        }  
  
        [Theory]
        [InlineData("List all customers' names and their emails.")]
        [InlineData("Show details of all products with their names and prices.")]
        [InlineData("List all suppliers and their contact names.")]
        public async Task SearchDataBaseSchema_ReturnsExpectedResults(string userPrompt)  
        {  
            // Arrange  
            int supportingContentCount = 500;
            var config = new DataBaseSearchConfig{ ResultsNumberLimit = supportingContentCount };
            CancellationToken cancellationToken = CancellationToken.None;  

            
            // Act  
            var results = _memorySearchService.SearchDataBaseSchema(userPrompt, config, cancellationToken);  
            var resultList = await results.ToListAsync(cancellationToken: cancellationToken);  

            
            XmlSerializer serializer = new XmlSerializer(typeof(List<TableSchema>));
            await using (StringWriter writer = new StringWriter())
            {  
                serializer.Serialize(writer, resultList);  
                string xml = writer.ToString();
                _output.WriteLine("Serialized list");
                _output.WriteLine(xml);  
            }

            // Assert  
            Assert.NotEmpty(resultList);  
  
            // Output results for debugging  
            foreach (var result in resultList)  
            {  
                _output.WriteLine(result.Schema);  
            }  
        }  
    }  
}  

