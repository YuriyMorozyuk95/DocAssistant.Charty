using System.Text;

using DocAssistant.Charty.Ai.Extensions;
using DocAssistant.Charty.Ai.Services.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Search;

public interface IDataBaseSearchService
{
    Task<List<SupportingContent>> Search(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default);
}

public class DataBaseSearchService : IDataBaseSearchService
{
    private readonly IMemorySearchService _memorySearchService;

    private readonly Kernel _kernel;

    private readonly ISqlExecutorService _sqlExecutorService;

    public DataBaseSearchService(IMemorySearchService memorySearchService, Kernel kernel, IConfiguration configuration, ISqlExecutorService sqlExecutorService)
    {
        _memorySearchService = memorySearchService;
        _kernel = kernel;
        _sqlExecutorService = sqlExecutorService;
    }

    public async Task<List<SupportingContent>> Search(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default)
    {
        var supportingContent = new List<SupportingContent>();

        try
        {
            if (string.IsNullOrEmpty(config.Query))
            {
                var schemas = await GetSchemas(userPrompt, config.TableCount, cancellationToken);

                foreach(var schema in schemas)
                {
                    supportingContent.Add(new SupportingContent
                                          {
                                              Title = "Schema",
                                              Content = schema.Value,
                                              IsDebug = true,
                                              SupportingContentType = SupportingContentType.Schema,
                                          });

                    config.Query = await TranslatePromptToQuery(userPrompt, schema.Value, config.ResultsNumberLimit, cancellationToken);

                    supportingContent.Add(new SupportingContent
                                          {
                                              Title = "T-SQL query",
                                              Content = config.Query,
                                              IsDebug = true,
                                              SupportingContentType = SupportingContentType.SqlQuery,
                                          });

                    var tableResult = await _sqlExecutorService.GetMarkdownTable(schema.Key, config.Query, config.ResultsNumberLimit, cancellationToken);

                    supportingContent.Add(new SupportingContent
                                          {
                                              Title = "Table Result",
                                              Content = tableResult,
                                              IsDebug = true,
                                              SupportingContentType = SupportingContentType.TableResult,
                                          });
                }
            }

        }
        catch (Exception e)
        {
            supportingContent.Add(new SupportingContent
            {
                IsDebug = true,
                Title = "Error",
                Content = e.ToString(),
            });
        }

        return supportingContent;
    }

    public async Task<string> TranslatePromptToQuery(string prompt, string schema, int count, CancellationToken cancellationToken)
    {
        var sqlTranslatorFunc = GetSqlTranslatorFunc();

        var arguments = new KernelArguments()
                            {
                                { "input", prompt },
                                { "count", count.ToString() },
                                { "schema", schema },
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

    public async Task<Dictionary<string, string>> GetSchemas(string userPrompt, int tableCount, CancellationToken cancellationToken = default)  
    {  
        var schemasPerDatabase = new Dictionary<string, string>();  
        var tableSchemas = await _memorySearchService.SearchDataBaseSchema(userPrompt, tableCount, cancellationToken).ToListAsync(cancellationToken: cancellationToken);  
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

