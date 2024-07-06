using HiveWays.FleetIntegration.Models;
using HiveWays.VehicleEdge.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ITrafficBalancerService
{
    double RecomputeBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleData> vehicleData, double previousRatio);
}