using Shared.Models;  
namespace SharedWebComponents.Pages;  
public sealed partial class ExamplePage : IDisposable  
{  
    private List<Server> _servers = new();  
    private Server? _selectedServer;  
    private List<string> _selectedDatabases = new();  
    private List<string> _selectedTables = new();  
    private readonly CancellationTokenSource _cancellationTokenSource = new();  
    private MudForm _form = null!;  
    private Task _getServerInfoTask = null!;  
    private Task _getExamplesTask = null!;  
    private bool _isLoadingServersInfo = false;  
    private bool _isLoadingExamples = false;  
    private bool _isOperationInProgress = false;  
    private string _filter = "";  
    private string _exampleFilter = "";  
    private Example Example { get; set; } = new Example();  
    private readonly List<string> _logs = new();  
    private readonly HashSet<Example> _examples = new();  
    [Inject] public required ApiClient Client { get; set; }  
    [Inject] public required ISnackbar Snackbar { get; set; }  
    [Inject] public required ILogger<Docs> Logger { get; set; }  
  
    protected override void OnInitialized()  
    {  
        _getServerInfoTask = GetServerInfoAsync();  
        _getExamplesTask = GetExamplesAsync();  
    }  

    //TODO filtering
    private bool OnExampleFilter(Example example) =>  
        example is not null &&  
        (string.IsNullOrWhiteSpace(_exampleFilter) ||  
         //example.DatabaseName.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
         example.ServerName.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
         example.SqlExample.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
         example.UserPromptExample.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase));  
  
    private async Task GetServerInfoAsync()  
    {  
        _isLoadingServersInfo = true;  
        try  
        {  
            _servers = await Client.GetAllServersAsync(CancellationToken.None);  
            StateHasChanged();  
        }  
        finally  
        {  
            _isLoadingServersInfo = false;  
            StateHasChanged();  
        }  
    }  
  
    private async Task GetExamplesAsync()  
    {  
        _isLoadingExamples = true;  
        try  
        {  
            var examples = await Client.GetAllExamplesAsync(CancellationToken.None).ToListAsync();  
            foreach (var example in examples)  
            {  
                _examples.Add(example);  
                StateHasChanged();  
            }  
        }  
        finally  
        {  
            _isLoadingExamples = false;  
            StateHasChanged();  
        }  
    }  
  
    private async Task SubmitExampleAsync()  
    {  
        if (Example != null)  
        {  
            _isOperationInProgress = true;  
            _logs.Clear();  
            Example.ServerName = _selectedServer?.ServerName ?? string.Empty;  
            Example.DatabaseNames = _selectedDatabases;  
            Example.TableNames = _selectedTables;  
            try  
            {  
                var documentId = await Client.UploadExampleAsync(Example, CancellationToken.None);  
                if (documentId != null)  
                {  
                    Snackbar.Add($"Uploaded example with DocumentId: {documentId}", Severity.Success, options =>  
                    {  
                        options.ShowCloseIcon = true;  
                        options.VisibleStateDuration = 10_000;  
                    });  
                    // Refresh the examples  
                    _examples.Clear();  
                    await GetExamplesAsync();  
                }  
                else  
                {  
                    Snackbar.Add("Failed to upload example.", Severity.Error, options =>  
                    {  
                        options.ShowCloseIcon = true;  
                        options.VisibleStateDuration = 10_000;  
                    });  
                }  
            }
            catch(Exception e)
            {
                Snackbar.Add($"Failed to upload example. {e}", Severity.Error, options =>  
                {  
                    options.ShowCloseIcon = true;  
                    options.VisibleStateDuration = 10_000;  
                });
            }
            finally  
            {  
                _isOperationInProgress = false;  
                StateHasChanged();  
            }  
        }  
    }  
  
    private void ClearExampleFields()  
    {  
        Example = new Example();  
    }  
  
    private void OnSelectedServerChanged(string newServerName)  
    {  
        _selectedServer = _servers.FirstOrDefault(s => s.ServerName == newServerName);  
        _selectedDatabases.Clear();  
        _selectedTables.Clear();  
        Example.ServerName = _selectedServer?.ServerName ?? string.Empty;  
        StateHasChanged();  
    }  
  
    private Task OnSelectedDatabasesChanged(IEnumerable<string> databases)  
    {  
        _selectedDatabases = databases.ToList();  
        _selectedTables.Clear();  
        StateHasChanged();  
        return Task.CompletedTask;  
    }  
  
    private Task OnSelectedTableChanged(IEnumerable<string> tables)  
    {  
        _selectedTables = tables.ToList();  
        Example.TableNames = _selectedTables;  
        StateHasChanged();  
        return Task.CompletedTask;  
    }  
  
    public void Dispose() => _cancellationTokenSource.Cancel();  
}  
