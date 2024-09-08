

using System.Text.Json.Serialization;

namespace Shared.Models;

public record class ChatRequest(
    [property: JsonPropertyName("messages")] ChatMessage[] History,
    [property: JsonPropertyName("overrides")] RequestOverrides? Overrides
    ) : ApproachRequest(Approach.RetrieveThenRead)
{
    private const string s_newlinePlaceholder = "<br/>";

    public string? LastUserQuestion => History?.Last(m => m.Role == "user")?.Content;

    //public void PreserveNewlinesConverter()
    //{
    //    foreach (var historyItem in History)
    //    {
    //        historyItem?.Content = historyItem?.Content?.Replace(s_newlinePlaceholder, "\n");
    //    }
    //}

    //private void PreventNewLine()
}
