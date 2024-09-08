using DocAssistant.Charty.Ai.Services.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalApi.Tests.Playground
{
    public class DataBaseRegistryServiceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly IDataBaseRegistryService _dataBaseRegistryService;
        private readonly ITestOutputHelper _output;
        private readonly string _testServerId = "test-server-id";
        private readonly IConfiguration _configuration;

        public DataBaseRegistryServiceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _output = output;
            using var scope = factory.Services.CreateScope();
            _dataBaseRegistryService = scope.ServiceProvider.GetRequiredService<IDataBaseRegistryService>();
            _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        [Fact]
        public async Task AddNewServerAsync_ShouldAddOrUpdateServerSuccessfully()
        {
            // Arrange  
            var tables = new List<TableSchema>
            {
                new TableSchema
                {
                    ServerName = _testServerId,
                    DatabaseName = "test-database",
                    ConnectionString = "test-connection-string",
                    TableName = "test-table"
                }
            };

            // Act  
            var server = await _dataBaseRegistryService.AddNewServer(tables);
            _output.WriteLine($"Added or updated server with ID: {server.Id}");

            // Fetch all servers to log their details  
            var servers = await _dataBaseRegistryService.GetAllServers();
            foreach (var srv in servers)
            {
                _output.WriteLine($"ServerName: {srv.ServerName}, DatabaseName: {string.Join(", ", srv.Databases.Select(x => x.DatabaseName))} , Tables: {string.Join(", ", srv.Databases.SelectMany(x => x.Tables))}");
            }

            // Assert  
            var fetchedServer = servers.FirstOrDefault(s => s.ServerName == _testServerId);
            Assert.NotNull(fetchedServer);
            Assert.Contains(fetchedServer.Databases, db => db.DatabaseName == "test-database");
            Assert.Contains(fetchedServer.Databases.SelectMany(db => db.Tables), table => table == "test-table");
        }

        [Fact]
        public async Task GetAllServersAsync_ShouldReturnServers()
        {
            // Act  
            var servers = await _dataBaseRegistryService.GetAllServers();

            // Log the details of the servers  
            _output.WriteLine("Fetched all servers:");
            foreach (var server in servers)
            {
                _output.WriteLine($"ServerName: {server.ServerName}, DatabaseName: {string.Join(", ", server.Databases.Select(x => x.DatabaseName))} , Tables: {string.Join(", ", server.Databases.SelectMany(x => x.Tables))}");
            }

            // Assert  
            Assert.NotEmpty(servers);
        }

        [Fact]
        public async Task CleanUpContainer_ShouldRemoveAllItems()
        {
            // Arrange  
            var tables = new List<TableSchema>
            {
                new TableSchema
                {
                    ServerName = _testServerId,
                    DatabaseName = "test-database",
                    ConnectionString = "test-connection-string",
                    TableName = "test-table"
                }
            };

            // Add a server to ensure there is data to clean up  
            await _dataBaseRegistryService.AddNewServer(tables);

            // Act  
            await _dataBaseRegistryService.CleanUpContainer();

            // Assert  
            var servers = await _dataBaseRegistryService.GetAllServers();
            Assert.Empty(servers);
            _output.WriteLine("Cleaned up all items from the container successfully.");
        }

        [Fact]
        public async Task AddNewServerAsync_ShouldUpdateExistingServerAndDatabase()
        {
            // Arrange  
            var initialTables = new List<TableSchema>
            {
                new TableSchema
                {
                    ServerName = _testServerId,
                    DatabaseName = "test-database",
                    ConnectionString = "test-connection-string",
                    TableName = "initial-table"
                }
            };

            var additionalTables = new List<TableSchema>
            {
                new TableSchema
                {
                    ServerName = _testServerId,
                    DatabaseName = "test-database2",
                    ConnectionString = "test-connection-string",
                    TableName = "additional-table"
                }
            };

            var additionalTables2 = new List<TableSchema>
                                   {
                                       new TableSchema
                                       {
                                           ServerName = _testServerId,
                                           DatabaseName = "test-database2",
                                           ConnectionString = "test-connection-string",
                                           TableName = "additional-table2"
                                       }
                                   };


            // Act  
            await _dataBaseRegistryService.AddNewServer(initialTables);
            var updatedServer = await _dataBaseRegistryService.AddNewServer(additionalTables);
            updatedServer = await _dataBaseRegistryService.AddNewServer(additionalTables2);

            // Fetch all servers to log their details  
            var servers = await _dataBaseRegistryService.GetAllServers();
            foreach (var srv in servers)
            {
                _output.WriteLine($"ServerName: {srv.ServerName}, DatabaseName: {string.Join(", ", srv.Databases.Select(x => x.DatabaseName))} , Tables: {string.Join(", ", srv.Databases.SelectMany(x => x.Tables))}");
            }

            // Assert  
            var fetchedServer = servers.FirstOrDefault(s => s.ServerName == _testServerId);
            Assert.NotNull(fetchedServer);
            Assert.Contains(fetchedServer.Databases, db => db.DatabaseName == "test-database");
            Assert.Contains(fetchedServer.Databases.SelectMany(db => db.Tables), table => table == "initial-table");
            Assert.Contains(fetchedServer.Databases.SelectMany(db => db.Tables), table => table == "additional-table");
        }
    }
}
