namespace Shared.Models;

public class TableSchema  
{
    public string DocumentId { get; set; }
    public string ServerName { get; set; }  
    public string DatabaseName { get; set; }  
    public string TableName { get; set; }  
    public string Schema { get; set; }
    public string ConnectionString { get; set; }
}

public class Example
{
    public string DocumentId { get; set; }
    public string UserPromptExample { get; set; }  
    public string SqlExample { get; set; }  
    public string ServerName { get; set; }  
    public string DatabaseName { get; set; }  
    public string TableName { get; set; }  
    public string ConnectionString { get; set; }
}

public class UploadExampleResponse  
{  
    public string? DocumentId { get; set; }  
}
