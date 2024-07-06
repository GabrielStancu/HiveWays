using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.Extensions;
using HiveWays.Business.TableStorageClient;
using HiveWays.Infrastructure.Clients;
using HiveWays.Infrastructure.Factories;
using HiveWays.VehicleEdge.Business;
using HiveWays.VehicleEdge.CarDataCsvParser;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped(typeof(ICarDataCsvParser<>), typeof(CarDataCsvParser<>));
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddSingleton<ICosmosDbClient<VehicleData>, CosmosDbClient<VehicleData>>();
        services.AddSingleton<ITableStorageClient<RoutingInfoEntity>, TableStorageClient<RoutingInfoEntity>>();
        services.AddSingleton<ITrafficBalancerService, TrafficBalancerService>();
        services.AddConfiguration<CarEventsServiceBusConfiguration>("CarEventsServiceBus");
        services.AddConfiguration<DeviceInfoConfiguration>("DeviceInfoBatching");
        services.AddConfiguration<TableStorageConfiguration>("StorageAccount");
        services.AddConfiguration<BlobConfiguration>("BlobAccount");
        services.AddConfiguration<CosmosDbConfiguration>("VehicleData");
        services.AddConfiguration<RoadConfiguration>("Road");
        services.AddConfiguration<RouteConfiguration>("Route");
    })
    .Build();

host.Run();
