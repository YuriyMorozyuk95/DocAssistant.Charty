// Copyright (c) Microsoft. All rights reserved.

using DocAssistant.Charty.Ai.Services.Database;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground;

public class QueryServiceTest  
{  
    private readonly ITestOutputHelper _output;  
    private readonly SqlExampleService _queryService;  
  
    public QueryServiceTest(ITestOutputHelper output)  
    {  
        _output = output;  
        _queryService = new SqlExampleService();  
    }  
  
    [Fact]  
    public void GenerateXmlResponse_ShouldReturnCorrectXml()  
    {  
        // Arrange  
        string userQuery = "List all customers' names and their emails.";  
        string sql = "SELECT CustomerName, Email FROM Customers;";  
  
        string expectedXml = @"<Example>  
  <UserPrompt>List all customers' names and their emails.</UserPrompt>  
  <AssistantResponse>  
    <TSQLQuery>SELECT CustomerName, Email FROM Customers;</TSQLQuery>  
  </AssistantResponse>  
</Example>";  
  
        // Act  
        string result = _queryService.GenerateXmlResponse(userQuery, sql);  
  
        // Assert  
       _output.WriteLine(result); 
    }  
}
