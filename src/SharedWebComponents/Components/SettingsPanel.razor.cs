namespace SharedWebComponents.Components;  
public sealed partial class SettingsPanel : IDisposable  
{  
    private bool _open;  
    private SupportedSettings _supportedSettings;  
    private List<Server> _servers = new();  
    private Server? _selectedServer = new Server();  
    private IEnumerable<string> _selectedDatabases = new List<string>();  
    private IEnumerable<string> _selectedTables = new List<string>();  
  
    [Inject] public required NavigationManager Nav { get; set; }  
    [Inject] public required ApiClient ApiClient { get; set; }  
    public RequestSettingsOverrides Settings { get; } = new();  
  
    [Parameter]  
    #pragma warning disable BL0007 // Component parameters should be auto properties  
    public bool Open  
    #pragma warning restore BL0007 // This is required for proper event propagation  
    {  
        get => _open;  
        set  
        {  
            if (_open == value)  
            {  
                return;  
            }  
            _open = value;  
            OpenChanged.InvokeAsync(value);  
        }  
    }  
  
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }  
  
    protected override async Task OnInitializedAsync()  
    {  
        Nav.LocationChanged += HandleLocationChanged;  
        _servers = await ApiClient.GetDataBaseRegistryAsync(CancellationToken.None);  
    }  
  
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)  
    {  
        var url = new Uri(e.Location);  
        var route = url.Segments.LastOrDefault();
        switch (route)
        {
            case "ask":
                _supportedSettings = SupportedSettings.Ask;
                break;
            case "chat":
                _supportedSettings = SupportedSettings.Chat;
                Task.Run(async () => _servers = await ApiClient.GetDataBaseRegistryAsync(CancellationToken.None));  
                break;
            default:
                _supportedSettings = SupportedSettings.All;
                break;
        }
    }  
  
    public void Dispose() => Nav.LocationChanged -= HandleLocationChanged;  
  
    private Task OnSelectedTableChanged(IEnumerable<string> tables)  
    {  
        _selectedTables = tables;  
        StateHasChanged();  
        if (Settings?.Overrides?.DataBaseSearchConfig?.TableFilter != null)  
        {  
            Settings.Overrides.DataBaseSearchConfig.TableFilter = tables.ToList();  
        }  
        return Task.CompletedTask;  
    }  
  
    private Task OnSelectedServerChanged(Server server)  
    {  
        _selectedServer = server;  
        _selectedDatabases = new List<string>();  
        _selectedTables = new List<string>();  
        StateHasChanged();  
        if (Settings?.Overrides?.DataBaseSearchConfig?.ServerFilter != null)  
        {  
            if (server != null)  
            {  
                Settings.Overrides.DataBaseSearchConfig.ServerFilter = server;  
            }  
        }  
        return Task.CompletedTask;  
    }  
  
    private Task OnSelectedDatabasesChanged(IEnumerable<string> databases)  
    {  
        _selectedDatabases = databases;  
        _selectedTables = new List<string>();  
        StateHasChanged();  
        if (Settings?.Overrides?.DataBaseSearchConfig?.DatabaseFilter != null)  
        {  
            Settings.Overrides.DataBaseSearchConfig.DatabaseFilter = databases.ToList();  
        }  
        return Task.CompletedTask;  
    }  
}  
  
public enum SupportedSettings  
{  
    All,  
    Chat,  
    Ask  
}  
