using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration.Business;

public class TrafficBalancerService : ITrafficBalancerService
{
    private const double DefaultMainRoadRatio = 0.8;
    private const double DefaultSecondaryRoadRatio = 0.2;
    private const double CongestionThreshold = 50;

    private double _mainRoadRatio = DefaultMainRoadRatio;
    private double _secondaryRoadRatio = DefaultSecondaryRoadRatio;

    public (double MainRoadRatio, double SecondaryRoadRatio) UpdateBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats)
    {
        double mainRoadCongestionLevel = CalculateCongestionLevel(congestedVehicles, vehicleStats, roadId: 1) / 2;
        double secondaryRoadCongestionLevel = CalculateCongestionLevel(congestedVehicles, vehicleStats, roadId: 2);

        if (mainRoadCongestionLevel >= CongestionThreshold)
        {
            _mainRoadRatio = DefaultMainRoadRatio * (1 - mainRoadCongestionLevel / 100);
            _secondaryRoadRatio = DefaultSecondaryRoadRatio * (1 + mainRoadCongestionLevel / 100);
        }
        else if (secondaryRoadCongestionLevel >= CongestionThreshold)
        {
            _mainRoadRatio = DefaultMainRoadRatio * (1 + secondaryRoadCongestionLevel / 100);
            _secondaryRoadRatio = DefaultSecondaryRoadRatio * (1 - secondaryRoadCongestionLevel / 100);
        }
        else
        {
            _mainRoadRatio = DefaultMainRoadRatio;
            _secondaryRoadRatio = DefaultSecondaryRoadRatio;
        }

        return (_mainRoadRatio, _secondaryRoadRatio);
    }

    private double CalculateCongestionLevel(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats, int roadId)
    {
        var congestedVehiclesOnRoad = congestedVehicles.Where(cv => cv.VehicleInfo.RoadId == roadId).ToList();
        var vehicleStatsOnRoad = vehicleStats.Where(vs => vs.RoadId == roadId).ToList();

        double totalVehiclesCount = vehicleStatsOnRoad.Count;
        double congestedVehiclesCount = congestedVehiclesOnRoad.Count;

        if (totalVehiclesCount == 0)
            return 0;

        return (congestedVehiclesCount / totalVehiclesCount) * 100;
    }
}
