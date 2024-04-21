using HiveWays.Business.Extensions;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.Infrastructure.Clients;
using HiveWays.Infrastructure.Factories;
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
        services.AddSingleton<ITableStorageClient<DataPointEntity>, TableStorageClient<DataPointEntity>>();
        services.AddSingleton<IVehicleClusterManager, VehicleClusterManager>();
        services.AddSingleton<IDirectionCalculator, DirectionCalculator>();
        services.AddSingleton<IDistanceCalculator, DistanceCalculator>();
        services.AddSingleton<ICongestionCalculator, CongestionCalculator>();
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddConfiguration<RedisConfiguration>("VehicleStats");
        services.AddConfiguration<ClusterConfiguration>("Cluster");
        services.AddConfiguration<CongestionConfiguration>("Congestion");
        services.AddConfiguration<TableStorageConfiguration>("StorageAccount");
        services.AddConfiguration<CleanupConfiguration>("Cleanup");
        services.AddConfiguration<CongestionQueueConfiguration>("CongestionQueue");
    })
    .Build();

host.Run();
