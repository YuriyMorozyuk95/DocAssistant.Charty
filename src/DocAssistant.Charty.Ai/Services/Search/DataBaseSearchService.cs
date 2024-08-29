using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

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

		private readonly string _connectionString;

		public DataBaseSearchService(IMemorySearchService memorySearchService, Kernel kernel, IConfiguration configuration, ISqlExecutorService sqlExecutorService)
		{
			_memorySearchService = memorySearchService;
			_kernel = kernel;
			_sqlExecutorService = sqlExecutorService;
			_connectionString = configuration["DataBaseConnectionString"];
		}

		public async Task<List<SupportingContent>> Search(string userPrompt, DataBaseSearchConfig config, CancellationToken cancellationToken = default)
		{
			var supportingContent = new List<SupportingContent>();

			try
			{
				if (string.IsNullOrEmpty(config.Query))
				{
					var schema = await GetSchema(userPrompt, config.TableCount, cancellationToken);
					supportingContent.Add(new SupportingContent
					{
						Title = "Schema",
						Content = schema,
						IsDebug = true
					});

					config.Query = await TranslatePromptToQuery(userPrompt, schema, config.ResultsNumberLimit, cancellationToken);
				}

				supportingContent.Add(new SupportingContent
									{
										Title = "T-SQL query",
										Content = config.Query,
										IsDebug = true
									});

				var tableResult = await _sqlExecutorService.GetMarkdownTable(_connectionString, config.Query, config.ResultsNumberLimit, cancellationToken);
				supportingContent.Add(new SupportingContent
				{
					Title = "Database result reference:",
					Content = tableResult,
				});
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

		internal async Task<string> TranslatePromptToQuery(string prompt, string schema, int count, CancellationToken cancellationToken)
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

			return result.ToString();
		}

		private KernelFunction GetSqlTranslatorFunc()
		{
			var func = _kernel.Plugins["DatabasePlugin"]["DatawarehouseTSqlTranslator"];

			return func;
		}

		internal async Task<string> GetSchema(string userPrompt, int tableCount, CancellationToken cancellationToken = default)
		{
			var searchTablesSchemaPrompt = $"Please retrive all tables that will be helpful for writing sql-t query form request \"{userPrompt}\"";
			var tableSchemas = _memorySearchService.SearchDataBaseSchema(searchTablesSchemaPrompt, tableCount, TagsKeys.Dim, cancellationToken);

			var sb = new StringBuilder();
			await foreach (var dimTable in tableSchemas)
			{
				sb.AppendLine(dimTable);
			}

			return sb.ToString();
		}
	}

