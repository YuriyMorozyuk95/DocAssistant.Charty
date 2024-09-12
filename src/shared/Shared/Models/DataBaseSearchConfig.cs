

namespace Shared.Models;

public class DataBaseSearchConfig
{
    public string Query { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int RowLimit { get; set; } = 500;
    public int TableCount { get; set; } = 10;
    public int ExamplesCount { get; set; } = 10;

    public Server ServerFilter { get; set; } = new();
    public List<string> TableFilter { get; set; } = new();

    public List<string> DatabaseFilter { get; set; } = new();
}
