namespace HiveWays.Domain.Models;

public class Vehicle
{
    public int Id { get; set; }
    public List<VehicleInfo> Info { get; set; }
    public bool IsAssignedToCluster { get; set; }

    public VehicleInfo MedianInfo => Info?[Info.Count/2];
}

public class VehicleInfo
{
    public DateTime Timestamp { get; set; }
    public GeoPoint Location { get; set; }
    public decimal SpeedKmph { get; set; }
    public decimal Heading { get; set; }
    public decimal AccelerationKmph { get; set; }
}
