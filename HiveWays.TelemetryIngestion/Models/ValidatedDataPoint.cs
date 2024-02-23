using HiveWays.Domain.Models;

namespace HiveWays.TelemetryIngestion.Models;

public class ValidatedDataPoint
{
    public DataPoint DataPoint { get; set; }
    public bool IsValid { get; set; }

    public ValidatedDataPoint(DataPoint dataPoint, bool isValid = false)
    {
        DataPoint = dataPoint;
        IsValid = isValid;
    }
}
