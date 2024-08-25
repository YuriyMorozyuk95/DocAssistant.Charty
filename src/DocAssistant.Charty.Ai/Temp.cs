namespace DocAssistant.Charty.Ai;

public static class TagsKeys
{
    public const string File = "file";

       public const string DocumentId = "documentId";

    public const string BlobUrl = "blobUrl";

    public const string IsOriginFile = "isOriginFile";

    public const string Endpoint = "endpoint";

    public const string TableType = "tableType";
    public const string Dim = "dim";
    public const string Fact = "fact";
}

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

}