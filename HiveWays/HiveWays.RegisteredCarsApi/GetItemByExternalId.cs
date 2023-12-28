using System.Net;
using System.Text.Json;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Domain.Items;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HiveWays.RegisteredItemsApi;

public class GetItemByExternalId
{
    private readonly ICosmosDbClient<BaseItem> _itemClient;
    private readonly ILogger _logger;

    public GetItemByExternalId(ICosmosDbClient<BaseItem> itemClient,
        ILogger<GetItemByExternalId> logger)
    {
        _itemClient = itemClient;
        _logger = logger;
    }

    [Function("GetItemByExternalId")]
    public async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetItemByExternalId/{id}")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(GetItemByExternalId)} processed a request.");

        var item = (await _itemClient.GetItemsByQueryAsync(i => i.ExternalId == id))
            .FirstOrDefault();

        if (item is null)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        var itemJson = JsonSerializer.Serialize(item);
        _logger.LogInformation("Fetched the following item: {FetchedItemById}", itemJson);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(itemJson)
        };
    }
}