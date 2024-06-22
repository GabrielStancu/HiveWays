using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ITrafficBalancerService
{
    double RecomputeBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats, double previousRatio);
}