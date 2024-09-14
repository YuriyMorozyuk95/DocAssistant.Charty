using System.Text;
using System.Text.RegularExpressions;

using DocAssistant.Charty.Ai.Extensions;
using DocAssistant.Charty.Ai.Services.Search;

using Microsoft.SemanticKernel;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IDataBaseCreateTableService
{
    Task<List<SupportingContentDto>> CreateTable(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default);
}

public class DataBaseCreateTableService : IDataBaseCreateTableService
{
    private readonly IMemorySearchService _memorySearchService;
    private readonly Kernel _kernel;
    private readonly ISqlExecutorService _sqlExecutorService;
    private readonly IExampleService _exampleService;

    private readonly IDataBaseRegistryService _dataBaseRegistryService;

    private readonly IMemoryManagerService _memoryManagerService;

    public DataBaseCreateTableService(
        IMemorySearchService memorySearchService,
        Kernel kernel,
        ISqlExecutorService sqlExecutorService,
        IExampleService exampleService,
        IDataBaseRegistryService dataBaseRegistryService,
        IMemoryManagerService memoryManagerService)
    {
        _memorySearchService = memorySearchService;
        _kernel = kernel;
        _sqlExecutorService = sqlExecutorService;
        _exampleService = exampleService;
        _dataBaseRegistryService = dataBaseRegistryService;
        _memoryManagerService = memoryManagerService;
    }

    public async Task<List<SupportingContentDto>> CreateTable(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default)
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
                await ProcessExistingQuery(userPrompt, config, supportingContent, cancellationToken);
            }

            var server = config.ServerFilter.ServerName;
            var database = config.DatabaseFilter.FirstOrDefault();
            var table = GetTableNameFromCreateTableScript(config.Query);

            var updatedDatabase = await _dataBaseRegistryService.AddTableForServer(server, database, table);

            var newTableSchema = new TableSchema
            {
                ServerName = server,
                DatabaseName = database,
                TableName = table,
                Schema = config.Query,
                ConnectionString = updatedDatabase.ConnectionString,
            };

            await _memoryManagerService.UploadTableSchemaToMemory(newTableSchema, cancellationToken);
        }
        catch (Exception e)
        {
            supportingContent.Add(CreateErrorContent(e));
        }

        return supportingContent;
    }

    public async Task ProcessQueryGeneration(string userPrompt, DataBaseSearchConfig config, List<SupportingContentDto> supportingContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var schemas = await GetSchemas(userPrompt, config, cancellationToken);
            var examples = await GetExamples(userPrompt, config, cancellationToken);

            AddExamplesContent(supportingContent, examples);
            foreach (var schema in schemas)
            {
                AddSchemaContent(supportingContent, schema);
                config.Query = await TranslatePromptToCreateTableScript(userPrompt, schema.Value, examples, cancellationToken);
                AddSqlQueryContent(supportingContent, config.Query);

                var tableResult = await _sqlExecutorService.ExecuteCreateTableScript(schema.Key, config.Query, cancellationToken);
                AddTableResultContent(supportingContent, tableResult);
            }
        }
        catch (Exception e)
        {
            supportingContent.Add(CreateErrorContent(e));
        }
    }

    private void AddTableResultContent(List<SupportingContentDto> supportingContent, string tableResult)
    {
        supportingContent.Add(new SupportingContentDto
        {
            Title = "Create Table Result",
            Content = tableResult,
            IsDebug = true,
            SupportingContentType = SupportingContentType.TableResult,
        });
    }

    private async Task ProcessExistingQuery(string userPrompt, DataBaseSearchConfig config, List<SupportingContentDto> supportingContent, CancellationToken cancellationToken = default)
    {
        AddSqlQueryContent(supportingContent, config.Query);
        var schemas = await GetSchemas(userPrompt, config, cancellationToken);

        foreach (var schema in schemas)
        {
            AddSchemaContent(supportingContent, schema);
            var tableResult = await _sqlExecutorService.ExecuteCreateTableScript(schema.Key, config.Query, cancellationToken);
            AddTableResultContent(supportingContent, tableResult);
        }
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
            Title = "Create Table Script",
            Content = query,
            IsDebug = true,
            SupportingContentType = SupportingContentType.SqlQuery,
        });
    }

    public async Task<string> TranslatePromptToCreateTableScript(string prompt, string schema, string examples, CancellationToken cancellationToken)
    {
        var sqlTranslatorFunc = GetSqlTranslatorFunc();
        var arguments = new KernelArguments()
                        {
                            { "input", prompt },
                            { "schema", schema },
                            { "examples", examples },
                            { "dateTime", DateTimeOffset.UtcNow.ToString() },
                        };
        var result = await _kernel.InvokeAsync(sqlTranslatorFunc, arguments, cancellationToken);
        return result.ExtractSqlScript();
    }

    private KernelFunction GetSqlTranslatorFunc()
    {
        var func = _kernel.Plugins["DatabasePlugin"]["GenerateCreateTableScript"];
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

    public static string GetTableNameFromCreateTableScript(string createTableScript)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(createTableScript))
            {
                throw new ArgumentException("The create table script cannot be null or empty.", nameof(createTableScript));
            }

            // Regular expression to match the table name in a CREATE TABLE statement  
            string pattern = @"CREATE\s+TABLE\s+(\[?\w+\]?)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Match match = regex.Match(createTableScript);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return "Test";
            }
        }
        catch (Exception e)
        {
            return "Test";
        }
    }
}
