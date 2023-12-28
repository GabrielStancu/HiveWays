using System.Net;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.ItemsDataGenerator;
using HiveWays.Domain.Items;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HiveWays.RegisteredItemsApi;

public class InitializeDb
{
    private readonly ICosmosDbClient<BaseItem> _itemClient;
    private readonly ILogger _logger;

    public InitializeDb(ICosmosDbClient<BaseItem> itemClient,
        ILogger<InitializeDb> logger)
    {
        _itemClient = itemClient;
        _logger = logger;
    }

    [Function("InitializeDb")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation($"C# HTTP trigger {nameof(InitializeDb)} function processed a request.");

        var items = ItemsDataGenerator.Generate();
        var upsertTasks = new List<Task>();
        foreach (var item in items)
        {
            _logger.LogInformation("Inserting item of type {InsertObjectType}, with Id {InsertObjectId} " +
                                   "and External Id {InsertObjectExternalId}", item.ObjectType, item.Id, item.ExternalId);

            var upsertTask = _itemClient.UpsertItemAsync(item);
            upsertTasks.Add(upsertTask);
        }

        await Task.WhenAll(upsertTasks);

        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }
}