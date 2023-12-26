using Azure;
using Azure.Data.Tables;
using HiveWays.Domain.Models;

namespace HiveWays.Domain.Entities;

public class DataPointEntity : ITableEntity
{
    // id of the car here
    public string PartitionKey { get; set; }
    // the moment the values were generated
    public string RowKey { get; set; }
    // x[m]
    public decimal Longitude { get; set; }
    // y[m]
    public decimal Latitude { get; set; }
    public decimal Speed { get; set; }
    public decimal Heading { get; set; }
    public decimal Acceleration { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DataPointEntity(DataPoint dataPoint, DateTime timeReference)
    {
        PartitionKey = dataPoint.Id.ToString();
        RowKey = timeReference.AddSeconds(5 * dataPoint.TimeOffsetSeconds).ToString("o");
        Longitude = dataPoint.X;
        Latitude = dataPoint.Y;
        Speed = dataPoint.Speed;
        Heading = dataPoint.Heading;
        Acceleration = dataPoint.Acceleration;
    }
}
