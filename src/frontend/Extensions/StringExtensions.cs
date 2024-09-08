

namespace ClientApp.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Converts the given <paramref name="fileName"/> to a citation URL,
    /// using the given <paramref name="baseUrl"/>.
    /// </summary>
    internal static string ToCitationUrl(this string fileName, string baseUrl)
    {
        var builder = new UriBuilder(baseUrl);
        builder.Path += $"/{fileName}";
        builder.Fragment = "view-fitV";

        return builder.Uri.AbsoluteUri;
    }

    public static string GetTagValue(this string[] tagsArray, string key)
    {
        return tagsArray.FirstOrDefault(x => x.StartsWith(key)).Replace($"{key}:", string.Empty);
    }
}
