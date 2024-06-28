using HiveWays.Business.CosmosDbClient;
using HiveWays.FleetIntegration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace HiveWays.FleetIntegration
{
    public class ClustersReader
    {
        private readonly ICosmosDbClient<ClusteringResult> _cosmosClient;

        public ClustersReader(ICosmosDbClient<ClusteringResult> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        [Function("ClustersReader")]
        public async Task<ClusteringResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            var clusteringResults = await _cosmosClient.GetDocumentsAsync();
            return clusteringResults.FirstOrDefault();
        }
    }
}
