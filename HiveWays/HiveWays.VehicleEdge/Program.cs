using HiveWays.Business.Extensions;
using HiveWays.Infrastructure.Factories;
using HiveWays.VehicleEdge.CarDataCsvParser;
using HiveWays.VehicleEdge.Configuration;
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
        services.AddConfiguration<CarEventsServiceBusConfiguration>("CarEventsServiceBus");
    })
    .Build();

host.Run();
