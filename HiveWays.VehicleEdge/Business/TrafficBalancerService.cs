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
        int maxMainRoadVehicles = 200;
        double newRatio = 1;

        if (vehiclesOnMainRoad < maxMainRoadVehicles)
        {
            return newRatio;
        }

        double mainRoadCapacityWeight = 15.0;
        double secondaryRoadCapacityWeight = 1.01;

        double currentRatio = 1.0 * vehiclesOnMainRoad / (vehiclesOnMainRoad + vehiclesOnSecondaryRoad);
        double idealRatio = mainRoadCapacityWeight / (mainRoadCapacityWeight + secondaryRoadCapacityWeight);
        double idealRatioWeight = 9.0;
        double currentRatioWeight = 1.0;

        newRatio = (idealRatioWeight * idealRatio + currentRatioWeight * currentRatio) / (idealRatioWeight + currentRatioWeight);

        return Math.Round(Math.Max(0.84, Math.Min(1, newRatio)), 2);
    }
}
