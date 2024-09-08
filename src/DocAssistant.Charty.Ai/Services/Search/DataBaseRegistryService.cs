using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Search
{
    public interface IDataBaseRegistryService
    {
        Task<List<Server>> GetAllServers();
        Task<Server> AddNewServer(List<TableSchema> tables);
        //Task AddTableForServer(string serverName, string databaseName, string tableName);
        Task CleanUpContainer();
        Task<string?> GetConnectionStringByDatabaseName(string databaseName);
    }

    public class DataBaseRegistryService : IDataBaseRegistryService
    {
        private readonly CosmosClient _cosmosClient;
        private Container? _container;
        private Microsoft.Azure.Cosmos.Database? _database;

        private readonly string? _databaseId;

        private readonly string? _containerId;

        public DataBaseRegistryService(IConfiguration configuration)
        {
            var endpointUri = configuration["CosmosDb:EndpointUri"];
            var primaryKey = configuration["CosmosDb:PrimaryKey"];
            _databaseId = configuration["CosmosDb:DatabaseId"];
            _containerId = configuration["CosmosDb:ContainerId"];

            _cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            });

        }

        private async Task Initialize()
        {
            _database ??= (await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId))?.Database;
            _container ??= (await _database?.CreateContainerIfNotExistsAsync(new ContainerProperties(_containerId, "/serverName")))?.Container;
        }

        public async Task<List<Server>> GetAllServers()
        {
            await Initialize();
            var query = new QueryDefinition("SELECT * FROM c"); // Adjusted query to select all documents  
            var iterator = _container.GetItemQueryIterator<Server>(query);
            var servers = new List<Server>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                servers.AddRange(response);
            }
            return servers;
        }

        public async Task<string?> GetConnectionStringByDatabaseName(string databaseName)  
        {  
            await Initialize();  
      
            // Query to find the database by name  
            var query = new QueryDefinition("SELECT * FROM c");  
            var iterator = _container.GetItemQueryIterator<Server>(query);  
      
            while (iterator.HasMoreResults)  
            {  
                var response = await iterator.ReadNextAsync();  
                foreach (var server in response)  
                {  
                    var database = server.Databases.FirstOrDefault(db => db.DatabaseName == databaseName);  
                    if (database != null)  
                    {  
                        return database.ConnectionString;  
                    }  
                }  
            }  
      
            // Return null if the database is not found  
            return null;  
        }  


        public async Task<Server> AddNewServer(List<TableSchema> tables)
        {
            await Initialize();

            var tableInfo = tables.FirstOrDefault();
            var tableNames = tables.Select(x => x.TableName).ToList();

            // Check if the server already exists  
            var query = new QueryDefinition("SELECT * FROM c WHERE c.serverName = @serverName")
                .WithParameter("@serverName", tableInfo.ServerName);
            var iterator = _container.GetItemQueryIterator<Server>(query);
            var existingServer = (await iterator.ReadNextAsync()).FirstOrDefault();

            if (existingServer != null)
            {
                // Server exists, check if the database exists  
                var existingDatabase = existingServer.Databases
                    .FirstOrDefault(db => db.DatabaseName == tableInfo.DatabaseName);

                if (existingDatabase != null)
                {
                    // Database exists, update tables  
                    var updatedTables = existingDatabase.Tables.Concat(tableNames).Distinct().ToList();
                    existingDatabase.Tables = updatedTables;
                }
                else
                {
                    // Database does not exist, add new database  
                    var newDatabase = new Shared.Models.Database
                    {
                        DatabaseName = tableInfo.DatabaseName,
                        ConnectionString = tableInfo.ConnectionString,
                        Tables = tableNames,
                    };
                    existingServer.Databases = existingServer.Databases.Append(newDatabase);
                }

                // Update the server in the container  
                var updatedServer = await _container.ReplaceItemAsync(existingServer, existingServer.Id.ToString(), new PartitionKey(existingServer.ServerName));
                return updatedServer.Resource;
            }

            // Server does not exist, create new server  
            var newServer = new Server
                            {
                                Id = Guid.NewGuid(),
                                ServerName = tableInfo.ServerName,
                                Databases = new List<Shared.Models.Database>
                                            {
                                                new Shared.Models.Database
                                                {
                                                    DatabaseName = tableInfo.DatabaseName,
                                                    ConnectionString = tableInfo.ConnectionString,
                                                    Tables = tableNames,
                                                }
                                            }
                            };

            var createdServer = await _container.CreateItemAsync(newServer, new PartitionKey(newServer.ServerName));
            return createdServer.Resource;
        }


        public async Task CleanUpContainer()
        {
            await Initialize();

            // Define a query to retrieve all items from the container  
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = _container.GetItemQueryIterator<dynamic>(query);

            // Iterate through all items and delete them  
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    var id = item.id; // Assuming the item has an 'id' property  
                    var partitionKey = item.serverName; // Assuming the item has a 'serverName' property used as the partition key  
                    await _container.DeleteItemAsync<dynamic>(id.ToString(), new PartitionKey(partitionKey.ToString()));
                }
            }
        }
    }
}
