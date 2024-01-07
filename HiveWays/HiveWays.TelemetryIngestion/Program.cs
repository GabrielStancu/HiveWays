using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.Extensions;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Documents;
using HiveWays.Domain.Entities;
using HiveWays.Infrastructure.Clients;
using HiveWays.Infrastructure.Factories;
using HiveWays.TelemetryIngestion.Business;
using HiveWays.TelemetryIngestion.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<ICosmosDbClient<BaseDevice>, CosmosDbClient<BaseDevice>>();
        services.AddSingleton<ITableStorageClient<DataPointEntity>, TableStorageClient<DataPointEntity>>();
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddScoped<IDataPointValidator, DataPointValidator>();
        services.AddConfiguration<TableStorageConfiguration>("StorageAccount");
        services.AddConfiguration<IngestionConfiguration>("IngestionValidation");
        services.AddConfiguration<CosmosDbConfiguration>("RegisteredDevices");
        services.AddConfiguration<RoutingServiceBusConfiguration>("RoutingServiceBus");
    })
    .Build();

host.Run();
