

namespace SharedWebComponents.Pages;

public sealed partial class Docs : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private MudForm _form = null!;  
    private Task _getSchemasTask = null!;  
    private bool _isLoadingSchemas = false;  
    private bool _isOperationInProgress = false;  
    private string _filter = "";  
    private string ConnectionString { get; set; } = string.Empty;  
    private readonly List<string> _logs = new();  
    private readonly HashSet<TableSchema> _tableSchemas = new();  
  
    [Inject] public required ApiClient Client { get; set; }  
    [Inject] public required ISnackbar Snackbar { get; set; }  
    [Inject] public required ILogger<Docs> Logger { get; set; }  
  
    protected override void OnInitialized() =>  
        _getSchemasTask = GetTableSchemasAsync();  
  
    private bool OnFilter(TableSchema schema) =>  
        schema is not null &&  
        (string.IsNullOrWhiteSpace(_filter) ||  
         schema.TableName.Contains(_filter, StringComparison.OrdinalIgnoreCase));  
  
    private async Task GetTableSchemasAsync()  
    {  
        _isLoadingSchemas = true;  
        try  
        {  
            var schemas = await Client.GetAllDatabaseSchemasAsync(CancellationToken.None).ToListAsync();  
            foreach (var schema in schemas)  
            {  
                _tableSchemas.Add(schema);
                StateHasChanged();
            }  
        }  
        finally  
        {  
            _isLoadingSchemas = false;  
            StateHasChanged();  
        }  
    }  
  
    private async Task SubmitConnectionStringForUploadAsync()  
    {  
        if (!string.IsNullOrWhiteSpace(ConnectionString))  
        {  
            _isOperationInProgress = true;  
            _logs.Clear();  
            try  
            {  
                await foreach (var logMessage in Client.UploadDatabaseSchemaAsync(ConnectionString, CancellationToken.None))  
                {  
                    _logs.Add(logMessage);
                    StateHasChanged();
                }  
  
                Logger.LogInformation("Log Messages: {x}", _logs);  
                Snackbar.Add($"Uploaded database schema. Check logs for details.", Severity.Success, options =>  
                {  
                    options.ShowCloseIcon = true;  
                    options.VisibleStateDuration = 10_000;  
                });  
  
                // Refresh the table schemas  
                _tableSchemas.Clear();  
                await GetTableSchemasAsync();  
            }  
            finally  
            {  
                _isOperationInProgress = false;  
                StateHasChanged();  
            }  
        }  
    }  
  
    private void ClearConnectionString()  
    {  
        ConnectionString = string.Empty;  
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}
