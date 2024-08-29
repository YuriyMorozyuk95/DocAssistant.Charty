

namespace ClientApp.Interop;

internal sealed partial class JavaScriptModule
{
    [JSImport("listenForIFrameLoaded", nameof(JavaScriptModule))]
    // ReSharper disable once InconsistentNaming
    public static partial Task RegisterIFrameLoadedAsync(
        string selector,
        [JSMarshalAs<JSType.Function>] Action onLoaded);
}
