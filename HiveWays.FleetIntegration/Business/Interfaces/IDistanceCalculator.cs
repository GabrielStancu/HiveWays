using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IDistanceCalculator
{
    bool IsWithinDistanceToCluster(Vehicle clusterHead, Vehicle nearbyVehicle);
    double Distance(GeoPoint point1, GeoPoint point2);
}