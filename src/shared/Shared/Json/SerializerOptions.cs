

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Json;

public static class SerializerOptions
{
    public static JsonSerializerOptions Default { get; } =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
}

public class PreserveNewlinesConverter : JsonConverter<string>  
{  
    private const string s_newlinePlaceholder = "<br/>";  
  
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)  
    {  
        var value = reader.GetString();  
        return value?.Replace(s_newlinePlaceholder, "\n");  
    }  
  
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)  
    {  
        var modifiedValue = value?.Replace("\n", s_newlinePlaceholder);  
        writer.WriteStringValue(modifiedValue);  
    }  
}  
