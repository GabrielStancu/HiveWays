using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.Extensions;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business;
using HiveWays.FleetIntegration.Business.Clients;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using HiveWays.Infrastructure.Clients;
using HiveWays.Infrastructure.Factories;
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
        services.AddSingleton<IRedisClient<VehicleStats>, RedisClient<VehicleStats>>();
        services.AddSingleton<IRoutingInfoTableClient, RoutingInfoTableClient>();
        services.AddSingleton<IDeviceInfoTableClient, DeviceInfoTableClient>();
        services.AddSingleton<IVehicleClusterService, VehicleClusterService>();
        services.AddSingleton<IDirectionCalculator, DirectionCalculator>();
        services.AddSingleton<IDistanceCalculator, DistanceCalculator>();
        services.AddSingleton<ICongestionDetectionService, CongestionDetectionService>();
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddSingleton<ITrafficBalancerService, TrafficBalancerService>();
        services.AddSingleton<ICosmosDbClient<VehicleData>, CosmosDbClient<VehicleData>>();
        services.AddSingleton<ITableStorageClient<RoutingInfoEntity>, TableStorageClient<RoutingInfoEntity>>();
        services.AddConfiguration<RedisConfiguration>("VehicleStats");
        services.AddConfiguration<ClusterConfiguration>("Cluster");
        services.AddConfiguration<CongestionConfiguration>("Congestion");
        services.AddConfiguration<RoutingInfoConfiguration>("RoutingInfo");
        services.AddConfiguration<DeviceInfoConfiguration>("DeviceInfo");
        services.AddConfiguration<CleanupConfiguration>("Cleanup");
        services.AddConfiguration<CongestionQueueConfiguration>("CongestionQueue");
        services.AddConfiguration<CosmosDbConfiguration>("ClusteringResults");
        services.AddConfiguration<RouteConfiguration>("Route");
        services.AddConfiguration<RoadConfiguration>("Road");
    })
    .Build();

host.Run();
