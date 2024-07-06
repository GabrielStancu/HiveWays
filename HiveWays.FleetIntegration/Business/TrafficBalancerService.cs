using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using HiveWays.VehicleEdge.Models;

namespace HiveWays.FleetIntegration.Business;

public class TrafficBalancerService : ITrafficBalancerService
{
    private readonly RouteConfiguration _routeConfiguration;
    private readonly RoadConfiguration _roadConfiguration;

    public TrafficBalancerService(RouteConfiguration routeConfiguration,
        RoadConfiguration roadConfiguration)
    {
        _routeConfiguration = routeConfiguration;
        _roadConfiguration = roadConfiguration;
    }

    public double RecomputeBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleData> vehiclesData, double previousRatio)
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

        return Math.Max(0, Math.Min(1, newRatio));
    }

    private double CalculateCongestionLevel(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats, int roadId)
    {
        var congestedVehiclesOnRoad = congestedVehicles.Where(cv => cv.VehicleLocation.RoadId == roadId).ToList();
        var vehicleStatsOnRoad = vehicleStats.Where(vs => vs.RoadId == roadId).ToList();

        double totalVehiclesCount = vehicleStatsOnRoad.Count;
        double congestedVehiclesCount = congestedVehiclesOnRoad.Count;

        if (totalVehiclesCount == 0)
            return 0;

        return (congestedVehiclesCount / totalVehiclesCount) * 100;
    }
}
