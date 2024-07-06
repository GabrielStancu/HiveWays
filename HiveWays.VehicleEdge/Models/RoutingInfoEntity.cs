using Azure;
using Azure.Data.Tables;

namespace HiveWays.VehicleEdge.Models;

public class RoutingInfoEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public double Value { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
