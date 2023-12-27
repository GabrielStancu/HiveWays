using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion
{
    public class DataIngestionOrchestrator
    {
        private readonly ILogger<DataIngestionOrchestrator> _logger;
        private readonly DateTime _timeReference;

        public DataIngestionOrchestrator(ILogger<DataIngestionOrchestrator> logger)
        {
            _logger = logger;
            _timeReference = DateTime.UtcNow;
        }

        [Function(nameof(DataIngestionOrchestrator))]
        public async Task<IEnumerable<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger.LogInformation("Starting validation stage...");
            

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [Function(nameof(ValidateDataPoint))]
        public bool ValidateDataPoint([ActivityTrigger] DataPoint dataPoint, FunctionContext executionContext)
        {
            // Check incoming values are in range
            // Check vehicle id is ok (taken from Cosmos db)

            return true;
        }

        [Function(nameof(EnrichDataPoint))]
        public DataPointEntity EnrichDataPoint([ActivityTrigger] DataPoint dataPoint, FunctionContext executionContext)
        {
            // Compute new properties
            // convert to DataPointEntity

            return new DataPointEntity(dataPoint, _timeReference);
        }

        [Function(nameof(StoreEnrichedDataPoint))]
        public void StoreEnrichedDataPoint([ActivityTrigger] DataPointEntity dataPoint, FunctionContext executionContext)
        {
            // Compute new properties
            // convert to DataPointEntity

            //return new DataPointEntity(dataPoint, _timeReference);
        }

        [Function(nameof(StoreEnrichedDataPoint))]
        public void RouteMessage([ActivityTrigger] DataPointEntity dataPoint, FunctionContext executionContext)
        {
            // Compute new properties
            // convert to DataPointEntity

            //return new DataPointEntity(dataPoint, _timeReference);
        }
    }
}
