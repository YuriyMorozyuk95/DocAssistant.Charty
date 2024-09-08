namespace DocAssistant.Charty.Ai.Extensions;

public static class StringExtensions
{
    public static string GetTagValue(this string[] tagsArray, string key)
    {
        return tagsArray.FirstOrDefault(x => x.StartsWith(key))?.Replace($"{key}:", string.Empty) ?? null;
    }

    public static IEnumerable<string> GetTagValues(this string[] tagsArray, string key)
    {
        var tagsValues = tagsArray.Where(x => x.StartsWith(key)).Select(x => x?.Replace($"{key}:", string.Empty) ?? null).ToList();

        return tagsValues;
    }
}
