

using MudBlazor.Utilities;

namespace SharedWebComponents.Shared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new();
    private bool _drawerOpen = true;
    private bool _settingsOpen = false;
    private SettingsPanel? _settingsPanel;

    public MainLayout()
    {
        _theme.Palette.Primary = DocAssistantColors.Primary;
        _theme.PaletteDark.Primary = DocAssistantColors.Primary;

        _theme.Palette.Secondary = DocAssistantColors.Secondary;
        _theme.PaletteDark.Secondary = DocAssistantColors.Secondary;

        _theme.Typography = new MudBlazor.Typography()
                            {
                                Default = new Default()
                                          {
                                              FontFamily = new[] { "Segoe UI", "-apple-system", "BlinkMacSystemFont", "Roboto", "sans-serif" }
                                          }
                            };
    }

    public Color NavColor => IsDarkTheme ? Color.Dark : Color.Primary;

    private bool IsDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    private bool IsReversed
    {
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? false;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool IsRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }

    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => false,
        _ => true
    };

    private bool SortDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "voicechat" or "chat" => false,
        _ => true
    };

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;

    private void OnThemeChanged() => IsDarkTheme = !IsDarkTheme;

    private void OnIsReversedChanged() => IsReversed = !IsReversed;

    public static class DocAssistantColors
    {
        public static MudColor Primary => new MudColor("#005f9f");

        public static MudColor Secondary => new MudColor("#005a72");
    }
}
