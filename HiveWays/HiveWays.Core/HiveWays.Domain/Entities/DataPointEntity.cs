using Azure;
using Azure.Data.Tables;

namespace HiveWays.Domain.Entities;

public class DataPointEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public decimal SpeedMps { get; set; }
    public decimal SpeedKmph { get; set; }
    public decimal Heading { get; set; }
    public decimal AccelerationMps { get; set; }
    public decimal AccelerationKmph { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
