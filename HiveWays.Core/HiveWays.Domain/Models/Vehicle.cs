using System.Text.Json.Serialization;

namespace HiveWays.Domain.Models;

public class Vehicle
{
    public int Id { get; set; }
    public List<VehicleInfo> Info => UnorderedInfo
        .OrderBy(i => i.Timestamp)
        .ToList();

    [JsonIgnore]
    public List<VehicleInfo> UnorderedInfo { get; set; }

    [JsonIgnore]
    public bool IsAssignedToCluster { get; set; }

    [JsonIgnore]
    public VehicleInfo MedianInfo => Info?[Info.Count/2];
}

public class VehicleInfo
{
    public DateTime Timestamp { get; set; }
    public GeoPoint Location { get; set; }
    public double SpeedKmph { get; set; }
    public double Heading { get; set; }
    public double AccelerationKmph { get; set; }
    public int RoadId { get; set; }
}
