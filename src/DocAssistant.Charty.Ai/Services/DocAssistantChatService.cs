using System.Diagnostics;
using System.Text;

using DocAssistant.Charty.Ai.Extensions;
using DocAssistant.Charty.Ai.Services.CodeInterpreter;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
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

    private readonly IDataBaseSearchService _dataBaseSearchService;

    private readonly IExampleService _exampleService;

    private readonly ICodeInterpreterAgentService _codeInterpreterAgentService;

    private readonly IDataBaseInsertService _dataBaseInsertService;

    private readonly IDataBaseCreateTableService _dataBaseCreateTableService;

    private readonly IIntentService _intentService;

    public DocAssistantChatService(
        Kernel kernel,
        IConfiguration configuration,
        ILogger<DocAssistantChatService> logger,
        IDataBaseSearchService dataBaseSearchService,
        IExampleService exampleService,
        ICodeInterpreterAgentService codeInterpreterAgentService,
        IDataBaseInsertService dataBaseInsertService,
        IDataBaseCreateTableService dataBaseCreateTableService,
        IIntentService intentService)
    {
        _configuration = configuration;
        _logger = logger;
        _dataBaseSearchService = dataBaseSearchService;
        _exampleService = exampleService;
        _codeInterpreterAgentService = codeInterpreterAgentService;
        _dataBaseInsertService = dataBaseInsertService;
        _dataBaseCreateTableService = dataBaseCreateTableService;
        _intentService = intentService;

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

            if (overrides.DataBaseSearchConfig.Intent == Intent.Default)
            {
                overrides.DataBaseSearchConfig.Intent = await _intentService.CheckIntent(lastQuestion);
            }

            supportingContentList = await GetSupportingContent(lastQuestion, overrides, cancellationToken);

            var mergedSupportingContents = GetDocumentContents(supportingContentList);

            searchStopwatch.Stop();
            searchTime = TimeSpan.FromMilliseconds(searchStopwatch.ElapsedMilliseconds);
            _logger.LogInformation($"Search time: {searchTime}");

            var chatHistory = await ChatTurnToChatHistory(history, mergedSupportingContents, cancellationToken);

            var chatServiceStopwatch = Stopwatch.StartNew();

            string answer = null;
            if (overrides.DataBaseSearchConfig.Intent is Intent.Query or Intent.Default)
            {
                var chatMessageContents = await _chatService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);

                completionTime = TimeSpan.FromMilliseconds(chatServiceStopwatch.ElapsedMilliseconds);

                _logger.LogInformation($"Completion time: {completionTime}");

                if (chatMessageContents.Count == 0)
                {
                    throw new Exception("Ai model not respond");
                }

                answer = chatMessageContents[0].Content;
            }
            else
            {
                 var sqlResult = supportingContentList.Where(x => x.SupportingContentType == SupportingContentType.TableResult)
                    .Select(x => x.Content);

                answer = string.Join("/n", sqlResult);
            }


            answer = await PrepareSupportingContentForClient(supportingContentList, lastQuestion, answer, overrides.DataBaseSearchConfig.Intent);

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

    private async Task<string> PrepareSupportingContentForClient(List<SupportingContentDto> supportingContentList, string lastQuestion, string answer, Intent intent)
    {
        SupportingContentDto supportingCharts = null;

        switch(intent)
        {
            case Intent.Default:
            case Intent.Query:
            {
                var tableResult = supportingContentList.Where(x => x.SupportingContentType == SupportingContentType.TableResult).Select(x => x?.Content).ToList();

                foreach (var tableResultPerDb in tableResult)
                {
                    if (tableResultPerDb == null)
                    {
                        continue;
                    }

                    supportingCharts = await _codeInterpreterAgentService.GenerateChart(lastQuestion, tableResultPerDb);
                    supportingContentList.Add(supportingCharts);
                }
                break;
            }
            case Intent.Create:
            case Intent.Insert:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(intent), intent, null);
        }

        var examples = supportingContentList.Where(x => x.SupportingContentType == SupportingContentType.Examples).ToList();
        foreach (var example in examples)
        {
            example.Content = _exampleService.TranslateXmlToMarkdown(example.Content);
        }

        if (supportingCharts != null)
        {
            var stringBuilder = new StringBuilder(answer);
            stringBuilder.AppendLine("<br />");
            stringBuilder.AppendLine(supportingCharts.Content);

            return stringBuilder.ToString();
        }

        return answer;
    }

    private async Task<List<SupportingContentDto>> GetSupportingContent(string lastQuestion, RequestOverrides requestOverrides, CancellationToken cancellationToken = default)
    {
        List<SupportingContentDto> supportingContentList = [];

        switch (requestOverrides.DataBaseSearchConfig.Intent)
        {
            case Intent.Default:
            case Intent.Query:
            {
                var supportingContents = await _dataBaseSearchService.Search(lastQuestion, requestOverrides.DataBaseSearchConfig, cancellationToken);
                supportingContentList.AddRange(supportingContents);
                break;
            }
            case Intent.Create:
            {
                var supportingContents = await _dataBaseCreateTableService.CreateTable(lastQuestion, requestOverrides.DataBaseSearchConfig, cancellationToken);
                supportingContentList.AddRange(supportingContents);
                break;
            }
            case Intent.Insert:
            {
                var supportingContents = await _dataBaseInsertService.Insert(lastQuestion, requestOverrides.DataBaseSearchConfig, cancellationToken);
                supportingContentList.AddRange(supportingContents);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

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
