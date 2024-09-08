using DocAssistant.Charty.Ai.Services.Database;

using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground;

public class QueryServiceTest
{
    private readonly ITestOutputHelper _output;
    private readonly ExampleService _queryService;

    public QueryServiceTest(ITestOutputHelper output)
    {
        _output = output;
        _queryService = new ExampleService();
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

    [Fact]
    public void TranslateXmlToMarkdown_ShouldReturnCorrectMarkdown()
    {
        // Arrange  
        string xml = @"<Example>
  <UserPrompt>List all suppliers and their contact names</UserPrompt>
  <AssistantResponse>
    <TSQLQuery>SELECT [CustomerName], [Email]  FROM [Customers]</TSQLQuery>
  </AssistantResponse>
</Example>
<Example>
  <UserPrompt>List all orders along with the customer names who placed them.</UserPrompt>
  <AssistantResponse>
    <TSQLQuery>SELECT [CustomerName], [Email] FROM [Customers];</TSQLQuery>
  </AssistantResponse>
</Example>
<Example>
  <UserPrompt>Get the total quantity of products ordered</UserPrompt>
  <AssistantResponse>
    <TSQLQuery>SELECT SUM([Quantity]) AS TotalQuantity  FROM [OrderDetails]</TSQLQuery>
  </AssistantResponse>
</Example>";

        // Act  
        string result = _queryService.TranslateXmlToMarkdown(xml);

        // Assert  
        _output.WriteLine(result);
    }
}
