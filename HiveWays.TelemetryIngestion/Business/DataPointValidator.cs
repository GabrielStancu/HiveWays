using HiveWays.Domain.Models;
using HiveWays.TelemetryIngestion.Configuration;
using System.Collections.Concurrent;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Domain.Documents;
using HiveWays.TelemetryIngestion.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion.Business;

public class DataPointValidator : IDataPointValidator
{
    private readonly IngestionConfiguration _ingestionConfiguration;
    private readonly ICosmosDbClient<BaseDevice> _cosmosClient;
    private readonly ILogger<DataPointValidator> _logger;

    public DataPointValidator(IngestionConfiguration ingestionConfiguration,
        ICosmosDbClient<BaseDevice> cosmosClient,
        ILogger<DataPointValidator> logger)
    {
        _ingestionConfiguration = ingestionConfiguration;
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    public ValidatedDataPoint ValidateDataPointRange(DataPoint dataPoint)
    {
        if (dataPoint.Id < _ingestionConfiguration.MinId || dataPoint.Id > _ingestionConfiguration.MaxId)
        {
            _logger.LogError("Item is not registered, id: {UnregisteredItemId}", dataPoint.Id);
            return new ValidatedDataPoint(dataPoint);
        }

        if (dataPoint.Speed < _ingestionConfiguration.MinSpeed || dataPoint.Speed > _ingestionConfiguration.MaxSpeed)
        {
            _logger.LogError("Invalid speed indicating error data: {InvalidSpeed} m/s", dataPoint.Speed);
            return new ValidatedDataPoint(dataPoint);
        }

        return new ValidatedDataPoint(dataPoint, true);
    }

    public async Task CheckDevicesRegistrationAsync(ConcurrentBag<ValidatedDataPoint> validationResults)
    {
        var validIds = validationResults
            .Where(dp => dp.IsValid)
            .Select(dp => dp.DataPoint.Id)
            .Distinct()
            .ToList();
        var registrationResults = await AreIdsRegisteredAsync(validIds);

        Parallel.ForEach(validationResults.Where(dp => dp.IsValid), validationResult =>
        {
            var id = validationResult.DataPoint.Id;
            if (registrationResults.Keys.Contains(id))
            {
                validationResult.IsValid = registrationResults[id];
            }
        });
    }

    private async Task<Dictionary<int, bool>> AreIdsRegisteredAsync(List<int> ids)
    {
        var registrationResults = new Dictionary<int, bool>();
        foreach (var id in ids)
        {
            registrationResults.Add(id, false);
        }

        try
        {
            var registeredDevices = (await _cosmosClient.GetDocumentsByQueryAsync(devices =>
            {
                var filteredDevices = devices.Where(d => ids.Contains(d.ExternalId));
                return filteredDevices as IOrderedQueryable<BaseDevice>;
            })).ToList();

            foreach (var id in ids)
            {
                var isRegisteredDevice = registeredDevices.FirstOrDefault(rd => rd.ExternalId == id) != null;
                if (!isRegisteredDevice)
                {
                    _logger.LogError("Item with id could not be found as registered item: {UnregisteredItemId}", id);
                }
                registrationResults[id] = isRegisteredDevice;
            }

            return registrationResults;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered exception while validating devices: {ValidationExceptionMessage} @ {ValidationExceptionStackTrace}", ex.Message, ex.StackTrace);
            return registrationResults;
        }
    }
}
