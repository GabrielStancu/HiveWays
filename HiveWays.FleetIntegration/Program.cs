using HiveWays.Business.Extensions;
using HiveWays.Business.RedisClient;
using HiveWays.FleetIntegration.Business;
using HiveWays.Infrastructure.Clients;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton(typeof(IRedisClient<>), typeof(RedisClient<>));
        services.AddScoped<IVehicleClustering, VehicleClustering>();
        services.AddConfiguration<RedisConfiguration>("VehicleStats");
        services.AddConfiguration<ClusterConfiguration>("Cluster");
    })
    .Build();

host.Run();
