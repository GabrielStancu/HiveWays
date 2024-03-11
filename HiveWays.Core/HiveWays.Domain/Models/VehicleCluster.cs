namespace HiveWays.Domain.Models;

public class VehicleCluster
{
    public List<int> VehicleIds { get; set; }
    public List<VehicleStats> VehicleStats { get; set; }
    public decimal? CenterLongitude { get; set; }
    public decimal? CenterLatitude { get; set; }
    public decimal? AverageOrientation { get; set; }
}
