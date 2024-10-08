﻿

using Azure.Core;
using Azure.Identity;
using FluentAssertions;
using MinimalApi.Services;
using NSubstitute;

namespace MinimalApi.Tests;

public class AzureComputerVisionServiceTest
{
    [EnvironmentVariablesFact("AZURE_COMPUTER_VISION_ENDPOINT")]
    public async Task VectorizeImageTestAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException();
        using var httpClient = new HttpClient();
        var imageUrl = @"https://learn.microsoft.com/azure/ai-services/computer-vision/media/quickstarts/presentation.png";

        var service = new AzureComputerVisionService(httpClient, endpoint, new DefaultAzureCredential());
        var result = await service.VectorizeImage(imageUrl);

        result.ModelVersion.Should().NotBeNullOrEmpty();

        // download image to local file, and verify the api on local image.
        var tempFile = Path.GetTempFileName();
        tempFile = Path.ChangeExtension(tempFile, ".png");
        try
        {
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(imageUrl);
            using var fileStream = File.OpenWrite(tempFile);
            await stream.CopyToAsync(fileStream);
            fileStream.Flush();
            fileStream.Close();

            var localResult = await service.VectorizeImage(tempFile);

            localResult.ModelVersion.Should().NotBeNullOrEmpty();
            localResult.Vector.Should().BeEquivalentTo(result.Vector);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [EnvironmentVariablesFact("AZURE_COMPUTER_VISION_ENDPOINT")]
    public async Task VectorizeTextTestAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException();
        using var httpClient = new HttpClient();
        var service = new AzureComputerVisionService(httpClient, endpoint, new DefaultAzureCredential());
        var text = "Hello world";
        var result = await service.VectorizeText(text);

        result.ModelVersion.Should().NotBeNullOrEmpty();
        result.Vector.Length.Should().Be(1024);
    }
}
