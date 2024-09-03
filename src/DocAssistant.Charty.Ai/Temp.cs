namespace DocAssistant.Charty.Ai;

public static class GlobalStatus
{
    public static string LastIndexErrorMessage { get; set; }
    public static string CurrentOrLastDocumentId { get; set; }
}

public class SupportingContent
{
    public SupportingContent()
    {
    }

    public SupportingContent(string title, string content, string originUri = null)
    {
        Title = title;
        Content = content;
        OriginUri = originUri;
    }

    public bool IsDebug { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public string OriginUri { get; set; }

    public string DocumentId { get; set; }

    public int Rank {get;set;}

    public SupportingContentType SupportingContentType { get; set; }

}

public enum SupportingContentType
{
    Schema = 0,
    SqlQuery = 1,
    TableResult = 2,
    Examples = 3, 
}
