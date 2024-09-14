using System.Text;

using DocAssistant.Charty.Ai.Extensions;
using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.SemanticKernel;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Search;

public interface IDataBaseSearchService
{
    Task<List<SupportingContentDto>> Search(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default);
}

public class DataBaseSearchService : IDataBaseSearchService
{
    private readonly IMemorySearchService _memorySearchService;

    private readonly Kernel _kernel;

    private readonly ISqlExecutorService _sqlExecutorService;

    private readonly IExampleService _exampleService;

    private readonly IDataBaseRegistryService _dataBaseRegistryService;

    public DataBaseSearchService(
        IMemorySearchService memorySearchService,
        Kernel kernel,
        ISqlExecutorService sqlExecutorService,
        IExampleService exampleService,
        IDataBaseRegistryService dataBaseRegistryService)
    {
        _memorySearchService = memorySearchService;
        _kernel = kernel;
        _sqlExecutorService = sqlExecutorService;
        _exampleService = exampleService;
        _dataBaseRegistryService = dataBaseRegistryService;
    }

    public async Task<List<SupportingContentDto>> Search(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default)  
    {  
        var supportingContent = new List<SupportingContentDto>();  
  
        try  
        {  
            if (string.IsNullOrEmpty(config.Query))  
            {  
                await ProcessQueryGeneration(userPrompt, config, supportingContent, cancellationToken);  
            }  
            else  
            {  
                await ProcessExistingQuery(config, supportingContent, cancellationToken);  
            }  
        }  
        catch (Exception e)  
        {  
            supportingContent.Add(CreateErrorContent(e));  
        }  
  
        return supportingContent;  
    }  
  
    private async Task ProcessQueryGeneration(string userPrompt, DataBaseSearchConfig config, List<SupportingContentDto> supportingContent, CancellationToken cancellationToken)  
    {  
        var schemas = await GetSchemas(userPrompt, config, cancellationToken);

        var examples = await GetExamples(userPrompt, config, cancellationToken);  
        AddExamplesContent(supportingContent, examples);
  
        foreach (var schema in schemas)  
        {  
            AddSchemaContent(supportingContent, schema);  
  
            config.Query = await TranslatePromptToQuery(userPrompt, schema.Value, examples, config.RowLimit, cancellationToken);  
            AddSqlQueryContent(supportingContent, config.Query);  
  
            var tableResult = await _sqlExecutorService.GetHtmlTable(schema.Key, config.Query, config.RowLimit, cancellationToken);  
            AddTableResultContent(supportingContent, tableResult);  
        }  
    }  
  
    private async Task ProcessExistingQuery(DataBaseSearchConfig config, List<SupportingContentDto> supportingContent, CancellationToken cancellationToken)  
    {  
        var database = config.DatabaseFilter.FirstOrDefault();  
        var connectionString = await _dataBaseRegistryService.GetConnectionStringByDatabaseName(database);
        
        var tableResult = await _sqlExecutorService.GetHtmlTable(connectionString, config.Query, config.RowLimit, cancellationToken);  
        AddTableResultContent(supportingContent, tableResult);  
    }  
  
    private SupportingContentDto CreateErrorContent(Exception e)  
    {  
        return new SupportingContentDto  
        {  
            IsDebug = true,  
            Title = "Error",  
            Content = e.ToString(),  
        };  
    }  
  
    private void AddSchemaContent(List<SupportingContentDto> supportingContent, KeyValuePair<string, string> schema)  
    {  
        supportingContent.Add(new SupportingContentDto  
        {  
            Title = "Schema",  
            Content = schema.Value,  
            IsDebug = true,  
            SupportingContentType = SupportingContentType.Schema,  
        });  
    }  
  
    private void AddExamplesContent(List<SupportingContentDto> supportingContent, string examples)  
    {  
        supportingContent.Add(new SupportingContentDto  
        {  
            Title = "Examples",  
            Content = examples,  
            IsDebug = true,  
            SupportingContentType = SupportingContentType.Examples,  
        });  
    }  
  
    private void AddSqlQueryContent(List<SupportingContentDto> supportingContent, string query)  
    {  
        supportingContent.Add(new SupportingContentDto  
        {  
            Title = "T-SQL query",  
            Content = query,  
            IsDebug = true,  
            SupportingContentType = SupportingContentType.SqlQuery,  
        });  
    }  
  
    private void AddTableResultContent(List<SupportingContentDto> supportingContent, SupportingContentDto tableResult)  
    {  
        supportingContent.Add(tableResult);  
    }


    public async Task<string> TranslatePromptToQuery(string prompt, string schema, string examples, int count, CancellationToken cancellationToken)
    {
        var sqlTranslatorFunc = GetSqlTranslatorFunc();

        var arguments = new KernelArguments()
                            {
                                { "input", prompt },
                                { "count", count.ToString() },
                                { "schema", schema },
                                { "examples", examples },
                                { "dateTime", DateTimeOffset.UtcNow.ToString() },
                            };

        var result = await _kernel.InvokeAsync(sqlTranslatorFunc, arguments, cancellationToken);

        return result.ExtractSqlScript();
    }

    private KernelFunction GetSqlTranslatorFunc()
    {
        var func = _kernel.Plugins["DatabasePlugin"]["GenerateSql"];

        return func;
    }

    public async Task<string> GetExamples(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default)
    {
        var examples = await _memorySearchService.SearchExamples(userPrompt, dataBaseSearchConfig, cancellationToken).ToListAsync(cancellationToken: cancellationToken);

        var builder = new StringBuilder();

        foreach (var example in examples)
        {
            var xmlExample = _exampleService.GenerateXmlResponse(example.UserPromptExample, example.SqlExample);
            builder.AppendLine(xmlExample);
        }

        return builder.ToString();
    }

    public async Task<Dictionary<string, string>> GetSchemas(string userPrompt, DataBaseSearchConfig dataBaseSearchConfig, CancellationToken cancellationToken = default)
    {
        var schemasPerDatabase = new Dictionary<string, string>();
        var tableSchemas = await _memorySearchService.SearchDataBaseSchema(userPrompt, dataBaseSearchConfig, cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        var schemaPerConnectionString = tableSchemas.GroupBy(x => x.ConnectionString);

        foreach (var tableInConnectionString in schemaPerConnectionString)
        {
            var firstItem = tableInConnectionString.First();
            StringBuilder builder = new StringBuilder();

            // Append server and database information in Markdown  
            builder.AppendLine($"## Server: {firstItem.ServerName}");
            builder.AppendLine($"### Database: {firstItem.DatabaseName}");
            builder.AppendLine();

            foreach (var table in tableInConnectionString)
            {
                // Append table name and schema in Markdown  
                builder.AppendLine($"#### Table: {table.TableName}");
                builder.AppendLine("```sql");
                builder.AppendLine(table.Schema);
                builder.AppendLine("```");
                builder.AppendLine(); // Add a blank line for better readability  
            }

            schemasPerDatabase.Add(firstItem.ConnectionString, builder.ToString());
        }

        return schemasPerDatabase;
    }
}
