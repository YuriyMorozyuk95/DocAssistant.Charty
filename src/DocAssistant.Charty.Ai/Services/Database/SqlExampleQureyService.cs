using System.Xml.Linq;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface ISqlExampleService
{
    string GenerateXmlResponse(string userQuery, string sql);
}
public class SqlExampleService : ISqlExampleService
{
    public string GenerateXmlResponse(string userQuery, string sql)
    {
        // Create the XML structure  
        XElement exampleElement = new XElement("Example",
            new XElement("UserPrompt", userQuery),
            new XElement("AssistantResponse",
                new XElement("TSQLQuery", sql)
            )
        );

        // Convert the XElement to a string  
        string xmlString = exampleElement.ToString();
        return xmlString;
    }
}
