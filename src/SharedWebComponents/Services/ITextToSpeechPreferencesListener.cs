

namespace SharedWebComponents.Services;

public interface ITextToSpeechPreferencesListener
{
    void OnAvailableVoicesChanged(Func<Task> onVoicesChanged);

    void UnsubscribeFromAvailableVoicesChanged();
}
