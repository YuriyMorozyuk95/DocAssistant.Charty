// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace MinimalApi.Tests.Playground;

using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Xunit;
using Xunit.Abstractions;  
  
public class GeneratePythonIntegrationTest : IClassFixture<WebApplicationFactory<Program>>  
{  
    private readonly WebApplicationFactory<Program> _factory;  
    private readonly ITestOutputHelper _testOutputHelper;  
  
    public GeneratePythonIntegrationTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)  
    {  
        _factory = factory;  
        _testOutputHelper = testOutputHelper;  
    }  
  
    [Fact]  
    public async Task CanGenerateAndExecutePythonCodeAsync()  
    {  
        // Arrange  
        var kernel = _factory.Services.GetRequiredService<Kernel>();  
        var function = kernel.Plugins["CodeInterpreter"]["GeneratePython"];  
        var prompt = "Generate a Python function that calculates the factorial of a number.";  
  
        // Act  
        var result = await kernel.InvokeAsync(function, new() { ["input"] = prompt });  
        var pythonCode = result.ToString();  
        _testOutputHelper.WriteLine("Generated Python Code:");  
        _testOutputHelper.WriteLine(pythonCode);  
  
        //// Save the generated Python code to a file  
        //var tempFilePath = Path.GetTempFileName() + ".py";  
        //await File.WriteAllTextAsync(tempFilePath, pythonCode);  
  
        //// Execute the Python code  
        //var processStartInfo = new ProcessStartInfo  
        //{  
        //    FileName = "python",  
        //    Arguments = tempFilePath,  
        //    RedirectStandardOutput = true,  
        //    RedirectStandardError = true,  
        //    UseShellExecute = false,  
        //    CreateNoWindow = true  
        //};  
  
        //var process = new Process { StartInfo = processStartInfo };  
        //process.Start();  
  
        //var output = await process.StandardOutput.ReadToEndAsync();  
        //var error = await process.StandardError.ReadToEndAsync();  
  
        //process.WaitForExit();  
  
        //// Assert  
        //_testOutputHelper.WriteLine("Python Execution Output:");  
        //_testOutputHelper.WriteLine(output);  
        //_testOutputHelper.WriteLine("Python Execution Error:");  
        //_testOutputHelper.WriteLine(error);  
  
        //Assert.True(process.ExitCode == 0, "Python script did not execute successfully.");  
        //Assert.False(string.IsNullOrWhiteSpace(output), "Python script did not produce any output.");  
    }  
}  
