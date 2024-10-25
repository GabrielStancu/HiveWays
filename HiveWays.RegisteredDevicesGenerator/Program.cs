using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.Extensions;
using HiveWays.Domain.Documents;
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

        services.AddSingleton<ICosmosDbClient<BaseDevice>, CosmosDbClient<BaseDevice>>();
        services.AddConfiguration<CosmosDbConfiguration>("RegisteredItems");
    })
    .Build();

host.Run();
