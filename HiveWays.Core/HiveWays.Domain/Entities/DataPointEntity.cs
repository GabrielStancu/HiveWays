using Azure;
using Azure.Data.Tables;

namespace HiveWays.Domain.Entities;

public class DataPointEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public int RoadId { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double SpeedMps { get; set; }
    public double SpeedKmph { get; set; }
    public double Heading { get; set; }
    public double AccelerationMps { get; set; }
    public double AccelerationKmph { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
