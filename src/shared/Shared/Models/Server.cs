using System.Text.Json.Serialization;  
  
namespace Shared.Models  
{  
    public class Server  
    {  
        [JsonPropertyName("id")]  
        public Guid Id { get; set; }
  
        [JsonPropertyName("ServerName")]  
        public string ServerName { get; set; }
  
        [JsonPropertyName("databases")]  
        public IEnumerable<Database> Databases { get; set; } = new List<Database>();
    }

    public class Database
    {
        [JsonPropertyName("databaseName")]  
        public string DatabaseName { get; set; }

        [JsonPropertyName("connectionString")]  
        public string ConnectionString { get; set; }

        [JsonPropertyName("tables")]  
        public IEnumerable<string> Tables { get; set; } = new List<string>();
    }
}
