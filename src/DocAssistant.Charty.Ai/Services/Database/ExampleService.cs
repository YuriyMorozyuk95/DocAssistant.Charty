using System.Text;
using System.Xml.Linq;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IExampleService
{
    string GenerateXmlResponse(string userQuery, string sql);
    string TranslateXmlToMarkdown(string xml);
}
public class ExampleService : IExampleService
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

    public string TranslateXmlToMarkdown(string xml)
    {
        StringBuilder markdown = new StringBuilder();

        try
        {
            // Wrap the XML string in a single root element  
            string wrappedXml = $"<Examples>{xml}</Examples>";

            // Parse the wrapped XML string into an XElement  
            XElement rootElement = XElement.Parse(wrappedXml);

            // Initialize a StringBuilder to construct the Markdown output  

            // Iterate through each Example element in the wrapped XML  
            foreach (XElement exampleElement in rootElement.Elements("Example"))
            {
                // Extract the user prompt and the SQL query  
                string userPrompt = exampleElement.Element("UserPrompt")?.Value ?? string.Empty;
                string sql = exampleElement.Element("AssistantResponse")?.Element("TSQLQuery")?.Value ?? string.Empty;

                // Append the example to the Markdown structure  
                markdown.AppendLine("### Example");
                markdown.AppendLine();
                markdown.AppendLine("**User Prompt**");
                markdown.AppendLine();
                markdown.AppendLine("```");
                markdown.AppendLine(userPrompt);
                markdown.AppendLine("```");
                markdown.AppendLine();
                markdown.AppendLine("**Assistant Response**");
                markdown.AppendLine();
                markdown.AppendLine("```sql");
                markdown.AppendLine(sql);
                markdown.AppendLine("```");
                markdown.AppendLine(); // Add an extra line for separation between examples  
            }

            return markdown.ToString();
        }
        catch (Exception e)
        {
            markdown.AppendLine("While generating markdown issue happen");
            return markdown.ToString();
        }
    }
}
