using HiveWays.Business.CarDataCsvParser;
using HiveWays.Business.Extensions;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Domain.Models;
using HiveWays.Infrastructure.Clients;
using HiveWays.Infrastructure.Services;
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
        services.AddSingleton<IQueueSenderClient<DataPoint>, QueueSenderClient<DataPoint>>();
        services.AddConfiguration<ServiceBusConfiguration>("CarInfoServiceBus");
    })
    .Build();

host.Run();
