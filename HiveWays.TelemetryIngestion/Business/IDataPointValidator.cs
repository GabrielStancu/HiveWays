using System.Collections.Concurrent;
using HiveWays.Domain.Models;

namespace HiveWays.TelemetryIngestion.Business;

public interface IDataPointValidator
{
    KeyValuePair<DataPoint, bool> ValidateDataPointRange(DataPoint dataPoint);
    Task CheckDevicesRegistrationAsync(ConcurrentDictionary<DataPoint, bool> validationResults);
}