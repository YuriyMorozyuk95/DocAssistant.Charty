namespace DocAssistant.Charty.Ai.Extensions;

public static class StringExtensions
{
    public static string GetTagValue(this string[] tagsArray, string key)
    {
        return tagsArray.FirstOrDefault(x => x.StartsWith(key))?.Replace($"{key}:", string.Empty) ?? null;
    }
}
