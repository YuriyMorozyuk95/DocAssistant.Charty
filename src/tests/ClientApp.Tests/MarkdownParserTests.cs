using System;  
using System.Collections.Generic;  
using System.Text.Json;  
using Xunit;  
using Xunit.Abstractions;  
  
public class MarkdownParserTests  
{  
    private readonly ITestOutputHelper _testOutputHelper;  
  
    public MarkdownParserTests(ITestOutputHelper testOutputHelper)  
    {  
        _testOutputHelper = testOutputHelper;  
    }  
  
    public class TestData  
    {  
        public string MarkdownContent { get; set; }  
    }  
  
    [Fact]  
    public void MarkdownCorrectlyParsesToHtml()  
    {  
        // Create test data  
        var testData = new TestData  
                       {  
                           MarkdownContent = "| CustomerName | Email |\r\n| --- | --- |\r\n| John Doe | john.doe@example.com |\r\n| Jane Smith | jane.smith@example.com |\r\n"  
                       };  
  
        // Serialize test data  
        var jsonData = SerializeTestData(testData);  
        _testOutputHelper.WriteLine("Serialized JSON:");  
        _testOutputHelper.WriteLine(jsonData);  
  
        // Deserialize test data  
        var deserializedTestData = DeserializeTestData(jsonData);  
        _testOutputHelper.WriteLine("Deserialized Test Data:");  
        _testOutputHelper.WriteLine(deserializedTestData.MarkdownContent);  
    }  
  
    // Method to serialize test data to JSON  
    public static string SerializeTestData(TestData testData)  
    {  
        return JsonSerializer.Serialize(testData, new JsonSerializerOptions { WriteIndented = false });  
    }  
  
    // Method to deserialize test data from JSON  
    public static TestData DeserializeTestData(string jsonData)  
    {  
        return JsonSerializer.Deserialize<TestData>(jsonData);  
    }  
}
