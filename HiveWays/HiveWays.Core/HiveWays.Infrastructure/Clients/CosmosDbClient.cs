using Microsoft.Azure.Cosmos;
using HiveWays.Business.CosmosDbClient;

namespace HiveWays.Infrastructure.Clients;

public class CosmosDbClient
{
    private readonly CosmosDbConfiguration _configuration;

    public CosmosDbClient(CosmosDbConfiguration configuration)
    {
        _configuration = configuration;
    }

    private Container GetContainerClient()
    {
        var cosmosDbClient = new CosmosClient(_configuration.ConnectionString);
        var container = cosmosDbClient.GetContainer(_configuration.DatabaseName, _configuration.ContainerName);

        return container;
    }
}
