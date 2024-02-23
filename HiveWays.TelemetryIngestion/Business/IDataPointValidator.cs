using System.Collections.Concurrent;
using HiveWays.Domain.Models;
using HiveWays.TelemetryIngestion.Models;

namespace HiveWays.TelemetryIngestion.Business;

public interface IDataPointValidator
{
    ValidatedDataPoint ValidateDataPointRange(DataPoint dataPoint);
    Task CheckDevicesRegistrationAsync(ConcurrentBag<ValidatedDataPoint> validationResults);
}