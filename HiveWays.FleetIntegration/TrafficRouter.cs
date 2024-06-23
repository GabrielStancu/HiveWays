using System.Text.Json;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class TrafficRouter
{
    private readonly IRoutingInfoTableClient _tableStorageClient;
    private readonly RouteConfiguration _routeConfiguration;
    private readonly ILogger<TrafficRouter> _logger;

    public TrafficRouter(IRoutingInfoTableClient tableStorageClient,
        RouteConfiguration routeConfiguration,
        ILogger<TrafficRouter> logger)
    {
        _tableStorageClient = tableStorageClient;
        _routeConfiguration = routeConfiguration;
        _logger = logger;
    }

    [Function(nameof(TrafficRouter))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("Received request for routing information: {RoutingInfoRequest}", requestBody);

        var routingInfoRequest = JsonSerializer.Deserialize<RoutingInfoRequest>(requestBody);
        var routingInfoEntity = await _tableStorageClient.GetEntityAsync(routingInfoRequest.MainRoadId, routingInfoRequest.SecondaryRoadId);

        if (routingInfoEntity is null)
        {
            _logger.LogError("Could not find route ratio for main road with id {MainRoadId} and secondary road with id {SecondaryRoadId}. Returning default ratio of {DefaultRatio}",
                routingInfoRequest.MainRoadId, routingInfoRequest.SecondaryRoadId, _routeConfiguration.DefaultMainRoadRatio);

            return new OkObjectResult(_routeConfiguration.DefaultMainRoadRatio);
        }

        _logger.LogInformation("Fetched ratio of {RoadRatio} main road with id {MainRoadId} and secondary road with id {SecondaryRoadId}",
            routingInfoEntity.Value, routingInfoRequest.MainRoadId, routingInfoRequest.SecondaryRoadId);

        return new OkObjectResult(routingInfoEntity.Value);
    }
}