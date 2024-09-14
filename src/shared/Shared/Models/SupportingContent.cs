namespace Shared.Models;

public class SupportingContentDto
{
    public SupportingContentDto()
    {
    }

    public SupportingContentDto(string title, string content, string originUri = null)
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

    public bool IsSuccessful { get; set; }
}
