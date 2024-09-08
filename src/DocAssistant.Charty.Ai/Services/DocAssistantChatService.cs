using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;

using DocAssistant.Charty.Ai.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Microsoft.SemanticKernel;
using DocAssistant.Charty.Ai.Services.Search;

using Shared.Models;
using DocAssistant.Charty.Ai.Services.Database;

namespace DocAssistant.Charty.Ai.Services;

public interface IDocAssistantChatService
{
    Task<ChatAppResponse> Ask(ChatMessage[] history, RequestOverrides overrides, CancellationToken cancellationToken = default);
}

public class DocAssistantChatService : IDocAssistantChatService
{
    private readonly IChatCompletionService _chatService;

    private readonly IConfiguration _configuration;

    private readonly ILogger<DocAssistantChatService> _logger;

    private readonly int _maxTokens;

    private readonly IMemorySearchService _memorySearchService;

    private readonly IDataBaseSearchService _dataBaseSearchService;

    private readonly IExampleService _exampleService;

    public DocAssistantChatService(
        Kernel kernel,
        IConfiguration configuration,
        ILogger<DocAssistantChatService> logger,
        IMemorySearchService memorySearchService,
        IDataBaseSearchService dataBaseSearchService,
        IExampleService exampleService)
    {
        _configuration = configuration;
        _logger = logger;
        _memorySearchService = memorySearchService;
        _dataBaseSearchService = dataBaseSearchService;
        _exampleService = exampleService;

        _chatService = kernel.GetRequiredService<IChatCompletionService>();

        _maxTokens = _configuration.GetRequiredConfigurationValue<int>("MaxTokens");
    }

    public async Task<ChatAppResponse> Ask(ChatMessage[] history, RequestOverrides overrides, CancellationToken cancellationToken = default)
    {
        var error = string.Empty;
        var tokensSpent = 0;
        var searchTime = TimeSpan.Zero;
        var completionTime = TimeSpan.Zero;
        var supportingContentList = new List<SupportingContentDto>();

        try
        {
            SupportingImageRecord[] images = [];
            SupportingContentRecord[] documentContentList = [];

            var lastQuestion = history.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
                               ? userQuestion
                               : throw new InvalidOperationException("Use question is null");

            // Start the stopwatch for search  
            var searchStopwatch = Stopwatch.StartNew();

            supportingContentList = await GetSupportingContent(lastQuestion, overrides, cancellationToken);

            var mergedSupportingContents = GetDocumentContents(supportingContentList);

            searchStopwatch.Stop();
            searchTime = TimeSpan.FromMilliseconds(searchStopwatch.ElapsedMilliseconds);
            _logger.LogInformation($"Search time: {searchTime}");

            var chatHistory = await ChatTurnToChatHistory(history, mergedSupportingContents, cancellationToken);

            //var setting = (overrides.SearchParameters != null) ? new OpenAIPromptExecutionSettings()
            //{
            //    MaxTokens = request.SearchParameters.MaxResponse,
            //    Temperature = request.SearchParameters.Temperature,
            //} : null;

            var chatServiceStopwatch = Stopwatch.StartNew();
            var chatMessageContents = await _chatService.GetChatMessageContentsAsync(chatHistory, null, cancellationToken: cancellationToken);
            completionTime = TimeSpan.FromMilliseconds(chatServiceStopwatch.ElapsedMilliseconds);

            _logger.LogInformation($"Completion time: {completionTime}");

            if (chatMessageContents.Count == 0)
            {
                throw new Exception("Ai model not respond");
            }

            var answerChatMessageContent = chatMessageContents[0];

            var answer = answerChatMessageContent.Content;
            //tokensSpent = answerChatMessageContent.Metadata != null && answerChatMessageContent.Metadata.ContainsKey("Usage")
            //                ? ((CompletionsUsage)answerChatMessageContent.Metadata["Usage"]).TotalTokens
            //                : 0;


            PrepareSupportingContentForClient(supportingContentList);

            var responseMessage = new ResponseMessage("assistant", answer);
            var responseContext = new ResponseContext(
                DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray(),
                DataPointsImages: images?.Select(x => new SupportingImageRecord(x.Title, x.Url)).ToArray());

            var choice = new ResponseChoice(
                Index: 0,
                Message: responseMessage,
                Context: responseContext,
                DataPoints: supportingContentList.ToArray());

            return new ChatAppResponse(new[] { choice });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while generating answer");

            //var response = ApproachResponseFactory.FromServerException(e, supportingContentList, tokensSpent, searchTime, completionTime);

            throw new Exception("Error while generating answer", e);
        }
    }

    private void PrepareSupportingContentForClient(List<SupportingContentDto> supportingContentList)
    {
        var examples = supportingContentList.Where(x => x.SupportingContentType == SupportingContentType.Examples);
        foreach(var example in examples)
        {
            example.Content = _exampleService.TranslateXmlToMarkdown(example.Content);
        }

        var tableResults = supportingContentList.Where(x => x.SupportingContentType == SupportingContentType.TableResult);
        foreach(var table in tableResults)
        {
            table.Content = table.Content?.Replace("\n", "<br>") ?? string.Empty;
        }
    }

    private async Task<List<SupportingContentDto>> GetSupportingContent(string lastQuestion, RequestOverrides requestOverrides, CancellationToken cancellationToken = default)
    {
        List<SupportingContentDto> supportingContentList = [];

        var supportingContents = await _dataBaseSearchService.Search(lastQuestion, requestOverrides.DataBaseSearchConfig, cancellationToken);

        supportingContentList.AddRange(supportingContents);

        return supportingContentList;
    }

    private async Task<ChatHistory> ChatTurnToChatHistory(
        ChatMessage[] history,
        string mergedSupportingContents,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = await ReadSystemPrompt(mergedSupportingContents, cancellationToken);

        var chatHistory = new ChatHistory(systemPrompt);

        // add chat history
        foreach (var message in history)
        {
            if (message.IsUser)
            {
                chatHistory.AddUserMessage(message.Content);
            }
            else
            {
                chatHistory.AddAssistantMessage(message.Content);
            }
        }

        CheckTokensLimit(chatHistory);

        return chatHistory;
    }

    private void CheckTokensLimit(ChatHistory chatHistory)
    {
        var allMessages = chatHistory.Select(message => message?.Content);
        var concatenatedMessages = string.Join(" ", allMessages);

        var totalTokens = DefaultGPTTokenizer.StaticCountTokens(concatenatedMessages);

        if (totalTokens > _maxTokens)
        {
            throw new InvalidOperationException($"Token limit exceeded. The total tokens used is {totalTokens}, but the limit is {_maxTokens}, please clean up history to fix the issue.");
        }

        _logger.LogInformation("Remaining tokens: {x}", _maxTokens - totalTokens);
    }

    private async Task<string> ReadSystemPrompt(string mergedSupportingContents, CancellationToken cancellationToken)
    {
        var systemPromptPath = _configuration["RagChatGptAiAssistant:SystemPromptPath"];
        var path = string.Concat(AppContext.BaseDirectory, systemPromptPath);
        var basePrompt = await File.ReadAllTextAsync(path, cancellationToken);

        var systemMessageBuilder = new StringBuilder(basePrompt);

        systemMessageBuilder.AppendLine("\r\n## Helpful information ##");

        systemMessageBuilder.AppendLine($"-Current DateTime: {DateTimeOffset.UtcNow}");

        if (!string.IsNullOrEmpty(mergedSupportingContents))
        {
            systemMessageBuilder.AppendLine("References:");
            systemMessageBuilder.AppendLine("");
            systemMessageBuilder.AppendLine("## Source ##");
            systemMessageBuilder.AppendLine(mergedSupportingContents);
            systemMessageBuilder.AppendLine("## End ##");
        }

        return systemMessageBuilder.ToString();
    }

    private string GetDocumentContents(ICollection<SupportingContentDto> documentContentList)
    {
        string documentContents
            ;
        if (documentContentList.Count == 0)
        {
            documentContents = "no source available.";
        }
        else
        {
            documentContents = string.Join(
                "\r",
                documentContentList.Where(x => x.SupportingContentType == SupportingContentType.TableResult)
                    .Select(
                        x =>
                        {
                            var stringBuilder = new StringBuilder();
                            stringBuilder.AppendLine(x.Title);
                            stringBuilder.AppendLine(x.Content);
                            stringBuilder.AppendLine();

                            return stringBuilder.ToString();
                        }));
        }

        return documentContents;
    }
}
