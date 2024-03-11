namespace HiveWays.Domain.Models;

public class VehicleStats : IIdentifiable
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public decimal SpeedKmph { get; set; }
    public decimal Heading { get; set; }
    public decimal AccelerationKmph { get; set; }
    public bool IsComputedClusterCenter { get; set; }
}
