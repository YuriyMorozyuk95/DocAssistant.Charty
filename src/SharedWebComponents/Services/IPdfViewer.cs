

namespace SharedWebComponents.Services;

public interface IPdfViewer
{
    ValueTask ShowDocument(string name, string baseUrl);
}
