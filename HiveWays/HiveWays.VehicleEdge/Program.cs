using HiveWays.VehicleEdge.Business;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<ICarDataTableClient, CarDataTableClient>();
        services.AddConfiguration<TableClientConfiguration>("StorageAccount");
    })
    .Build();

host.Run();
