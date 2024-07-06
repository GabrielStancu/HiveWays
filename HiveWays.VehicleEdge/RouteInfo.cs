using System.Text.Json;
using HiveWays.Business.TableStorageClient;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge
{
    public class RouteInfo
    {
        private readonly ITableStorageClient<RoutingInfoEntity> _tableStorageClient;
        private readonly RouteConfiguration _routeConfiguration;
        private readonly ILogger<RouteInfo> _logger;

        public RouteInfo(ITableStorageClient<RoutingInfoEntity> tableStorageClient,
            RouteConfiguration routeConfiguration,
            ILogger<RouteInfo> logger)
        {
            _tableStorageClient = tableStorageClient;
            _routeConfiguration = routeConfiguration;
            _logger = logger;
        }

        [Function("RouteInfo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
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
}
