

namespace SharedWebComponents.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<string?> UploadExampleAsync(Example example, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("api/upload-example", example, SerializerOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadExampleResponse>();
        return result?.DocumentId;
    }

    public async IAsyncEnumerable<Example> GetAllExamplesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/get-all-examples", cancellationToken);
        response.EnsureSuccessStatusCode();
        var options = SerializerOptions.Default;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var example in JsonSerializer.DeserializeAsyncEnumerable<Example>(stream, options, cancellationToken))
        {
            if (example is null)
            {
                continue;
            }
            await Task.Delay(1, cancellationToken);
            yield return example;
        }
    }

    public async Task<ImageResponse?> RequestImageAsync(PromptRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/images", request, SerializerOptions.Default);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImageResponse>();
    }

    public async Task<bool> ShowLogoutButtonAsync()
    {
        var response = await httpClient.GetAsync("api/enableLogout");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async IAsyncEnumerable<string> UploadDatabaseSchemaAsync(
        string connectionString,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new UploadDatabaseSchemaRequest { ConnectionString = connectionString };
        var response = await httpClient.PostAsJsonAsync("api/upload-database-schema", request, SerializerOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();

        var options = SerializerOptions.Default;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var logMessage in JsonSerializer.DeserializeAsyncEnumerable<string>(stream, options, cancellationToken))
        {
            if (logMessage is null)
            {
                continue;
            }

            await Task.Delay(1, cancellationToken);
            yield return logMessage;
        }
    }

    public async IAsyncEnumerable<TableSchema> GetAllDatabaseSchemasAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/get-all-database-schema", cancellationToken);
        response.EnsureSuccessStatusCode();

        var options = SerializerOptions.Default;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var schema in
                       JsonSerializer.DeserializeAsyncEnumerable<TableSchema>(stream, options, cancellationToken))
        {
            if (schema is null)
            {
                continue;
            }

            await Task.Delay(1, cancellationToken);
            yield return schema;
        }
    }

    public async Task<AnswerResult<ChatRequest>> ChatConversation(ChatRequest request)
    {
        var apiRoute = "api/chat";

        var result = new AnswerResult<ChatRequest>(
            IsSuccessful: false,
            Response: null,
            Approach: request.Approach,
            Request: request);

        var json = JsonSerializer.Serialize(
            request,
            SerializerOptions.Default);

        using var body = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(apiRoute, body);

        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadFromJsonAsync<ChatAppResponseOrError>(SerializerOptions.Default);

            //foreach(var supportingContent in answer.Choices.LastOrDefault()?.DataPoints)
            //{
            //    var content = Markdown.ToHtml(supportingContent.Content);
            //    supportingContent.Content = content;
            //}

            var tableResults = answer.Choices.LastOrDefault()?.DataPoints.Where(x => x.SupportingContentType == SupportingContentType.TableResult);
            foreach(var table in tableResults)
            {
                table.Content = table.Content?.Replace("<br>", Environment.NewLine) ?? string.Empty;
            }

            return result with
            {
                IsSuccessful = answer is not null,
                Response = answer,
            };
        }
        else
        {
            var errorTitle = $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}";
            var answer = new ChatAppResponseOrError(
                Array.Empty<ResponseChoice>(),
                errorTitle);

            return result with
            {
                IsSuccessful = false,
                Response = answer
            };
        }
    }

    public async Task<List<Server>> GetAllServersAsync(CancellationToken cancellationToken)  
    {  
        var response = await httpClient.GetAsync("api/get-all-servers", cancellationToken);  
        response.EnsureSuccessStatusCode();  
        var servers = await response.Content.ReadFromJsonAsync<List<Server>>(cancellationToken: cancellationToken);  
        return servers ?? new List<Server>();  
    }
}
