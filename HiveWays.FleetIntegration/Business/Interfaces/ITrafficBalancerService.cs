using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ITrafficBalancerService
{
    (double MainRoadRatio, double SecondaryRoadRatio) UpdateBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats);
}