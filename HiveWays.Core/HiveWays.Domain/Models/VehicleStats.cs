namespace HiveWays.Domain.Models;

public class VehicleStats : IIdentifiable
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double SpeedKmph { get; set; }
    public double Heading { get; set; }
    public double AccelerationKmph { get; set; }
}
