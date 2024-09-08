

using System.Text.Json.Serialization;

namespace Shared.Models;

public record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    string Content)
{
    public bool IsUser => Role == "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = Content;
}
