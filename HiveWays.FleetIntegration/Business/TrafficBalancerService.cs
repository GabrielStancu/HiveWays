﻿using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;

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

    public double RecomputeBalancingRatio(List<CongestedVehicle> congestedVehicles, List<VehicleStats> vehicleStats)
    {
        double mainRoadCongestionLevel = CalculateCongestionLevel(congestedVehicles, vehicleStats, _roadConfiguration.MainRoadId);
        double secondaryRoadCongestionLevel = CalculateCongestionLevel(congestedVehicles, vehicleStats, _roadConfiguration.SecondaryRoadId);

        if (mainRoadCongestionLevel >= _routeConfiguration.CongestionThreshold)
        {
            return _routeConfiguration.DefaultMainRoadRatio * (1 - mainRoadCongestionLevel / 100);
        }
        
        if (secondaryRoadCongestionLevel >= _routeConfiguration.CongestionThreshold)
        {
            return _routeConfiguration.DefaultMainRoadRatio * (1 + secondaryRoadCongestionLevel / 100);
        }

        return _routeConfiguration.DefaultMainRoadRatio;
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
