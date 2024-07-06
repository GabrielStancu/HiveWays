using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;

namespace HiveWays.VehicleEdge.Business;

public class TrafficBalancerService : ITrafficBalancerService
{
    private readonly RoadConfiguration _roadConfiguration;

    public TrafficBalancerService(RoadConfiguration roadConfiguration)
    {
        _roadConfiguration = roadConfiguration;
    }

    public double RecomputeBalancingRatio(List<VehicleData> vehiclesData)
    {
        int vehiclesOnMainRoad = vehiclesData.Count(v => v.RoadId == _roadConfiguration.MainRoadId);
        int vehiclesOnSecondaryRoad = vehiclesData.Count(v => v.RoadId == _roadConfiguration.SecondaryRoadId);
        double newRatio = 1;

        if (vehiclesOnMainRoad == 0)
            return newRatio;

        double mainRoadCapacityWeight = 15.0;
        double secondaryRoadCapacityWeight = 1.01;
        double smoothingFactor = 0.1;

        double currentRatio = 1.0 * vehiclesOnMainRoad / (vehiclesOnMainRoad + vehiclesOnSecondaryRoad);
        double idealRatio = mainRoadCapacityWeight / (mainRoadCapacityWeight + secondaryRoadCapacityWeight);
        

        if (currentRatio > idealRatio)
        {
            newRatio = currentRatio - smoothingFactor * currentRatio;
        }
        else if (currentRatio < idealRatio)
        {
            newRatio = currentRatio + smoothingFactor * currentRatio;
        }

        return Math.Round(Math.Max(0.78, Math.Min(1, newRatio)), 2);
    }
}
