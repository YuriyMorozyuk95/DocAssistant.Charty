

using System.Text.Json.Serialization;

namespace Shared.Models;
public record RequestOverrides
{
    [JsonPropertyName("database_search_config")]
    public DataBaseSearchConfig DataBaseSearchConfig { get; set; } = new();
}
