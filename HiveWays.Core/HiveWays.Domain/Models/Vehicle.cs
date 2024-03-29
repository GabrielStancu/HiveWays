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
    public double SpeedKmph { get; set; }
    public double Heading { get; set; }
    public double AccelerationKmph { get; set; }
}
