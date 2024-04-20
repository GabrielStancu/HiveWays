using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;

namespace HiveWays.FleetIntegration.Business;

public class DistanceCalculator : IDistanceCalculator
{
    private readonly ClusterConfiguration _clusterConfiguration;
    private const double EarthRadiusMeters = 6371000.0;

    public DistanceCalculator(ClusterConfiguration clusterConfiguration)
    {
        _clusterConfiguration = clusterConfiguration;
    }

    public bool IsWithinDistanceToCluster(Vehicle clusterHead, Vehicle nearbyVehicle)
        => Distance(clusterHead.MedianInfo.Location, nearbyVehicle.MedianInfo.Location) <=
           _clusterConfiguration.ClusterRadius;

    public double Distance(GeoPoint point1, GeoPoint point2)
    {
        double distanceLat = ToRadians(point2.Latitude - point1.Latitude);
        double distanceLon = ToRadians(point2.Longitude - point1.Longitude);

        double a = Math.Sin(distanceLat / 2) * Math.Sin(distanceLat / 2) +
                   Math.Cos(ToRadians(point1.Latitude)) * Math.Cos(ToRadians(point2.Latitude)) *
                   Math.Sin(distanceLon / 2) * Math.Sin(distanceLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = EarthRadiusMeters * c;

        return distance;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
