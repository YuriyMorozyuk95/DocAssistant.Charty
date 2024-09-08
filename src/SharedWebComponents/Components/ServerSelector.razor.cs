namespace SharedWebComponents.Components;

public sealed partial class ServerSelector
{
	[Inject]
    public ApiClient ApiClient { get; set; }

	[Parameter]
	public IEnumerable<string> SelectedItems { get; set; } = new List<string>();

	[Parameter]
	public string Label { get; set; }

	[Parameter]
	public bool IsEnabled { get; set; }

	[Parameter]
	public EventCallback<IEnumerable<string>> SelectedItemsChanged { get; set; }

	[Parameter]
	public EventCallback<IEnumerable<string>> ItemsChanged { get; set; }

	[Parameter]
	public IEnumerable<string> Items { get; set; } = new List<string>();

	protected override async Task OnInitializedAsync()
	{
		//Items = (await AuthenticatedUserService.GetAuthenticatedUserAsync())?.UserRoles ?? new List<string>();
	}

	private async Task OnSelectedItemsChanged(IEnumerable<string> values)
	{
		SelectedItems = values;
		await SelectedItemsChanged.InvokeAsync(values);
	}
}
