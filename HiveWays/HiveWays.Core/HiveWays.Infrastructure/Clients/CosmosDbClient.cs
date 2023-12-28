using System.Net;
using Microsoft.Azure.Cosmos;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Domain.Items;
using Microsoft.Extensions.Logging;

namespace HiveWays.Infrastructure.Clients;

public class CosmosDbClient<T> : ICosmosDbClient<T> where T : BaseItem
{
    private readonly CosmosDbConfiguration _configuration;
    private readonly ILogger<CosmosDbClient<T>> _logger;
    private CosmosClient _client;

    public CosmosDbClient(CosmosDbConfiguration configuration, ILogger<CosmosDbClient<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<T> GetItemByIdAsync(string id, string partitionKey)
    {
        try
        {
            var container = GetContainerClient();
            var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered error while fetching entity by id. Id {CosmosByIdIdParam} from partition {CosmosByIdPartitionParam}. " +
                             "Exception: {CosmosByIdException} @ {CosmosByIdStackTrace}", id, partitionKey, ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetItemsAsync()
    {
        var entities = new List<T>();

        try
        {
            var container = GetContainerClient();
            var sqlQuery = "SELECT * FROM c";
            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                entities.AddRange(currentResultSet);
            }

            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered error while fetching entities. " +
                             "Exception: {CosmosAllException} @ {CosmosAllStackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetItemsByQueryAsync(Func<T, bool> query)
    {
        try
        {
            var container = GetContainerClient();
            var results = await Task.Run(() =>
            {
                var queryable = container.GetItemLinqQueryable<T>();
                return queryable.Where(query);
            });

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered error while fetching entities by query. " +
                             "Exception: {CosmosAllByQueryException} @ {CosmosAllByQueryStackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task UpsertItemAsync(T entity)
    {
        try
        {
            entity.Id ??= Guid.NewGuid();

            var container = GetContainerClient();
            var response = await container.UpsertItemAsync(entity, new PartitionKey(entity.Id.ToString()));

            if (response != null && response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Received unsuccessful response while upserting entity");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered error while upserting entity. " +
                             "Exception: {CosmosUpsertException} @ {CosmosUpsertStackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    private Container GetContainerClient()
    {
        _client ??= new CosmosClient(_configuration.ConnectionString);

        var container = _client.GetContainer(_configuration.DatabaseId, _configuration.ContainerName);

        return container;
    }
}
