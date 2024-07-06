using HiveWays.VehicleEdge.Models;

namespace HiveWays.VehicleEdge.Business;

public interface ITrafficBalancerService
{
    double RecomputeBalancingRatio(List<VehicleData> vehiclesData);
}