using System.Text.Json.Serialization;

namespace HiveWays.Domain.Models;

public class Vehicle
{
    public int Id { get; set; }
    public List<VehicleLocation> Trajectory { get; set; }

    [JsonIgnore]
    public bool IsAssignedToCluster { get; set; }

    [JsonIgnore]
    public VehicleLocation MedianLocation => Trajectory?
        .OrderBy(i => i.Timestamp)
        .ToList()[Trajectory.Count/2];
}

public class VehicleLocation
{
    public DateTime Timestamp { get; set; }
    public GeoPoint Location { get; set; }
    public double SpeedKmph { get; set; }
    public double Heading { get; set; }
    public double AccelerationKmph { get; set; }
    public int RoadId { get; set; }
}
