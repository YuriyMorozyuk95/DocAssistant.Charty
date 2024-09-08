

using System.Text.Json.Serialization;

using DocAssistant.Charty.Ai;

namespace Shared.Models;

public record SupportingContentRecord(string Title, string Content);

public record SupportingImageRecord(string Title, string Url);

public record DataPoints(
    [property: JsonPropertyName("text")] string[] Text)
{}

public record Thoughts(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("props")] (string, string)[]? Props = null)
{ }

public record ResponseContext(
    [property: JsonPropertyName("dataPointsContent")] SupportingContentRecord[]? DataPointsContent,
    [property: JsonPropertyName("dataPointsImages")] SupportingImageRecord[]? DataPointsImages)
{
    [JsonPropertyName("data_points")]
    public DataPoints DataPoints { get => new DataPoints(DataPointsContent?.Select(x => $"{x.Title}: {x.Content}").ToArray() ?? Array.Empty<string>()); }
}


public record ResponseMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content)
{
}

public record ResponseChoice(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("message")] ResponseMessage Message,
    [property: JsonPropertyName("context")] ResponseContext Context,
    [property: JsonPropertyName("supportingContent")] SupportingContentDto[] DataPoints)
{
    public SupportingContentDto[] TableResult => DataPoints.Where(x => x.SupportingContentType == SupportingContentType.TableResult).ToArray();
    public SupportingContentDto[] SqlQuery => DataPoints.Where(x => x.SupportingContentType == SupportingContentType.SqlQuery)?.ToArray();
    public SupportingContentDto[] Examples => DataPoints.Where(x => x.SupportingContentType == SupportingContentType.Examples)?.ToArray();
    public SupportingContentDto[] Schema => DataPoints.Where(x => x.SupportingContentType == SupportingContentType.Schema)?.ToArray();
}

public record ChatAppResponse(ResponseChoice[] Choices);

public record ChatAppResponseOrError(
    ResponseChoice[] Choices,
    string? Error = null);
