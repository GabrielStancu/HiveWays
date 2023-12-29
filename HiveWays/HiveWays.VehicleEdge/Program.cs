using HiveWays.Business.Extensions;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Infrastructure.Factories;
using HiveWays.VehicleEdge.CarDataCsvParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<ICarDataCsvParser, CarDataCsvParser>();
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddConfiguration<ServiceBusConfiguration>("CarInfoServiceBus");
    })
    .Build();

host.Run();
