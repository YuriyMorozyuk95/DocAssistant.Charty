

namespace DocAssistant.Charty.Ai.Services.Search;

public class DataBaseSearchConfig
{
    public string Query { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public int ResultsNumberLimit { get; set; } = 500;
    public int TableCount {get;set; } = 10;
}
