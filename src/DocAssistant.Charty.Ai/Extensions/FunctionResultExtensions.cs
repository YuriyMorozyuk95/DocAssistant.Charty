using Microsoft.SemanticKernel;

namespace DocAssistant.Charty.Ai.Extensions;

using System.Text.RegularExpressions;
public static class FunctionResultExtensions  
{  
    public static string ExtractTagContent(this FunctionResult functionResult, string tagName)  
    {
        var responseString = functionResult.ToString();

        if (string.IsNullOrEmpty(responseString) || string.IsNullOrEmpty(tagName))  
        {  
            return string.Empty;  
        }  
  
        string pattern = $@"<{tagName}>([\s\S]*?)</{tagName}>";  
        var match = Regex.Match(responseString, pattern, RegexOptions.IgnoreCase);  
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;  
    }

    public static string ExtractSqlScript(this FunctionResult functionResult)  
    {
        var responseString = functionResult.ToString();
        // Regular expression to match the SQL script within the markdown string  
        string pattern = @"```sql\s*(.*?)\s*```";  
        Match match = Regex.Match(responseString, pattern, RegexOptions.Singleline);  
  
        if (match.Success)  
        {  
            return match.Groups[1].Value;  
        }  
  
        return null;  
    }
}
