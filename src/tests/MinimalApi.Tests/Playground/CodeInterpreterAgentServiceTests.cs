using System.Diagnostics;  
using DocAssistant.Charty.Ai.Services.CodeInterpreter;  
using Microsoft.AspNetCore.Mvc.Testing;  
using Microsoft.Extensions.DependencyInjection;  
using Xunit;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Playground  
{  
    public class CodeInterpreterAgentServiceTests : IClassFixture<WebApplicationFactory<Program>>  
    {  
        private readonly WebApplicationFactory<Program> _factory;

        private readonly ITestOutputHelper _testOutputHelper;

        private readonly ICodeInterpreterAgentService _codeInterpreterAgentService;  
  
        public CodeInterpreterAgentServiceTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)  
        {  
            _factory = factory;
            _testOutputHelper = testOutputHelper;

            var scope = _factory.Services.CreateScope();  
            var services = scope.ServiceProvider;  
  
            _codeInterpreterAgentService = services.GetRequiredService<ICodeInterpreterAgentService>();  
        }  
  
        [Fact]  
        public async Task GenerateChart_ShouldGenerateAndDisplayImage()  
        {  
            // Arrange  
            string userPrompt = "Generate a bar chart for sales data.";  
            string htmlTable = @"  
                <table>  
                    <tr>  
                        <th>Month</th>  
                        <th>Sales</th>  
                    </tr>  
                    <tr>  
                        <td>Jan</td>  
                        <td>100</td>  
                    </tr>  
                    <tr>  
                        <td>Feb</td>  
                        <td>150</td>  
                    </tr>  
                    <tr>  
                        <td>Mar</td>  
                        <td>200</td>  
                    </tr>  
                </table>";  
  
            // Act  
            var result = await _codeInterpreterAgentService.GenerateChart(userPrompt, htmlTable);  
            string imagePath = await DownloadImageAsync(result.OriginUri);  
  
            // Assert  
            Assert.NotNull(result);  
            Assert.NotNull(result.OriginUri);  
  
            // Display the image  
            Process.Start(new ProcessStartInfo  
            {  
                FileName = "cmd.exe",  
                Arguments = $"/C start {imagePath}",  
                CreateNoWindow = true,  
                UseShellExecute = false  
            });  
        }  
  
        [Fact]  
        public async Task GenerateChart_ShouldOutputContent()  
        {  
            // Arrange  
            string userPrompt = "Generate a pie chart for sales data.";  
            string htmlTable = @"  
                <table>  
                    <tr>  
                        <th>Month</th>  
                        <th>Sales</th>  
                    </tr>  
                    <tr>  
                        <td>Jan</td>  
                        <td>100</td>  
                    </tr>  
                    <tr>  
                        <td>Feb</td>  
                        <td>150</td>  
                    </tr>  
                    <tr>  
                        <td>Mar</td>  
                        <td>200</td>  
                    </tr>  
                </table>";  
  
            // Act  
            var result = await _codeInterpreterAgentService.GenerateChart(userPrompt, htmlTable);  
  
            // Assert  
            Assert.NotNull(result);  
            Assert.NotNull(result.Content);  
  
            // Output the content  
            _testOutputHelper.WriteLine(result.Content);  
        }  
  
        private async Task<string> DownloadImageAsync(string imageUrl)
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetAsync(imageUrl);  
            response.EnsureSuccessStatusCode();  
  
            var tempPath = Path.GetTempPath();  
            var fileName = Path.GetFileName(imageUrl);  
            var filePath = Path.Combine(tempPath, fileName);  
  
            await using var fs = new FileStream(filePath, FileMode.Create);  
            await response.Content.CopyToAsync(fs);  
  
            return filePath;
        }  
    }  
}  
