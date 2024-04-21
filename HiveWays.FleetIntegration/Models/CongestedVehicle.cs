using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Models;

public class CongestedVehicle
{
    public int Id { get; set; }
    public VehicleInfo VehicleInfo { get; set; }
}
