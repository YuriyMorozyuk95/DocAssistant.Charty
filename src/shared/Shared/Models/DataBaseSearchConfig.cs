

namespace Shared.Models;

public class DataBaseSearchConfig
{
    public string Query { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int ResultsNumberLimit { get; set; } = 500;
    public int TableCount { get; set; } = 10;
    public int ExamplesCount { get; set; } = 10;
}
