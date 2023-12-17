using Azure;
using Azure.Data.Tables;

namespace HiveWays.VehicleEdge.Models;

public class ValueEntity : ITableEntity
{
    public decimal FuelLevel { get; set; }
    public decimal AmbientAirTemperature { get; set; }
    public decimal CoolantTemperature { get; set; }
    public decimal EngineSpeed { get; set; }
    public decimal TraveledDistance { get; set; }
    public decimal Speed { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal Altitude { get; set; }
    public decimal Heading { get; set; }
    public bool IsSent { get; set; }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public ValueEntity(string car, Batch batch)
    {
        PartitionKey = car;
        RowKey = batch.Values.First().Dp.First().Ts;
        MapBatch(batch);
    }

    private void MapBatch(Batch batch)
    {
        FuelLevel = MapDataPoint(batch, nameof(FuelLevel));
        AmbientAirTemperature = MapDataPoint(batch, nameof(AmbientAirTemperature));
        CoolantTemperature = MapDataPoint(batch, nameof(CoolantTemperature));
        EngineSpeed = MapDataPoint(batch, nameof(EngineSpeed));
        TraveledDistance = MapDataPoint(batch, nameof(TraveledDistance));
        Speed = MapDataPoint(batch, nameof(Speed));
        Latitude = MapDataPoint(batch, nameof(Latitude));
        Longitude = MapDataPoint(batch, nameof(Longitude));
        Latitude = MapDataPoint(batch, nameof(Latitude));
        Altitude = MapDataPoint(batch, nameof(Altitude));
        Heading = MapDataPoint(batch, nameof(Heading));
    }

    private decimal MapDataPoint(Batch batch, string path)
    {
        return batch.Values
            .FirstOrDefault(v => v.Path.Contains(path))?
            .Dp.FirstOrDefault()?
            .Value ?? decimal.Zero;
    }
}
