using System.Text;

using Microsoft.Data.SqlClient;

namespace DocAssistant.Charty.Ai.Services.Search;

public interface ISqlExecutorService
{
    Task<string> GetMarkdownTable(string connectionString, string sqlQuery, int count, CancellationToken cancellationToken = default);
    Task<string> GetHtmlTable(string connectionString, string sqlQuery, int count, CancellationToken cancellationToken = default);
    Task<string> ExecuteInsertScript(string connectionString, string insertScript, CancellationToken cancellationToken = default);
    Task<string> ExecuteCreateTableScript(string connectionString, string createTableScript, CancellationToken cancellationToken = default);
}

public class SqlExecutorService : ISqlExecutorService
{
    public async Task<string> GetMarkdownTable(string connectionString, string sqlQuery, int count, CancellationToken cancellationToken = default)
    {
        var markdownTable = new StringBuilder();
        var rowCount = 0;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sqlQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var headerWritten = false;

            while (await reader.ReadAsync(cancellationToken) && rowCount < count)
            {
                if (!headerWritten)
                {
                    markdownTable.AppendLine("| " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)) + " |");
                    markdownTable.AppendLine("| " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(_ => "---")) + " |");
                    headerWritten = true;
                }

                markdownTable.AppendLine("| " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)) + " |");
                rowCount++;
            }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return markdownTable.ToString();
    }

    public async Task<string> GetHtmlTable(string connectionString, string sqlQuery, int count, CancellationToken cancellationToken = default)  
    {  
        var htmlTable = new StringBuilder();  
        var rowCount = 0;  
  
        try  
        {  
            await using var connection = new SqlConnection(connectionString);  
            await connection.OpenAsync(cancellationToken);  
  
            await using var command = new SqlCommand(sqlQuery, connection);  
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);  
            var headerWritten = false;  
  
            htmlTable.AppendLine("<table>");  
  
            while (await reader.ReadAsync(cancellationToken) && rowCount < count)  
            {  
                if (!headerWritten)  
                {  
                    htmlTable.AppendLine("<tr>");  
                    for (int i = 0; i < reader.FieldCount; i++)  
                    {  
                        htmlTable.AppendLine($"<th>{reader.GetName(i)}</th>");  
                    }  
                    htmlTable.AppendLine("</tr>");  
                    headerWritten = true;  
                }  
  
                htmlTable.AppendLine("<tr>");  
                for (int i = 0; i < reader.FieldCount; i++)  
                {  
                    htmlTable.AppendLine($"<td>{reader.GetValue(i)}</td>");  
                }  
                htmlTable.AppendLine("</tr>");  
                rowCount++;  
            }  
  
            htmlTable.AppendLine("</table>");  
        }  
        catch (Exception ex)  
        {  
            return ex.Message;  
        }  
  
        return htmlTable.ToString();  
    }

    public async Task<string> ExecuteInsertScript(string connectionString, string insertScript, CancellationToken cancellationToken = default)  
    {  
        try  
        {  
            await using var connection = new SqlConnection(connectionString);  
            await connection.OpenAsync(cancellationToken);  
  
            await using var command = new SqlCommand(insertScript, connection);  
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);  
            return $"Insert script executed successfully. Rows affected: {rowsAffected}";  
        }  
        catch (Exception ex)  
        {  
            return ex.Message;  
        }  
    }

    public async Task<string> ExecuteCreateTableScript(string connectionString, string createTableScript, CancellationToken cancellationToken = default)  
    {  
        try  
        {  
            await using var connection = new SqlConnection(connectionString);  
            await connection.OpenAsync(cancellationToken);  
  
            await using var command = new SqlCommand(createTableScript, connection);  
            await command.ExecuteNonQueryAsync(cancellationToken);  
            return "Create table script executed successfully.";  
        }  
        catch (Exception ex)  
        {  
            return ex.Message;  
        }  
    } 
}
