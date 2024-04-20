using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;

namespace HiveWays.FleetIntegration.Business;

public class DirectionCalculator : IDirectionCalculator
{
    private readonly ClusterConfiguration _clusterConfiguration;

    public DirectionCalculator(ClusterConfiguration clusterConfiguration)
    {
        _clusterConfiguration = clusterConfiguration;
    }

    public bool IsSameDirection(Vehicle clusterHead, Vehicle nearbyVehicle)
    {
        var clusterHeadMedian = clusterHead.MedianInfo;
        var nearbyVehicleMedian = nearbyVehicle.MedianInfo;

        if (clusterHeadMedian == null || nearbyVehicleMedian == null ||
            clusterHeadMedian.RoadId != nearbyVehicleMedian.RoadId)
        {
            return false;
        }

        var clusterHeadDirection = ComputeDirectionVector(clusterHead.Info);
        var nearbyVehicleDirection = ComputeDirectionVector(nearbyVehicle.Info);
        double dotProduct = DotProduct(clusterHeadDirection, nearbyVehicleDirection);
        double magnitudesProduct = Magnitude(clusterHeadDirection) * Magnitude(nearbyVehicleDirection);
        double angle = Math.Acos(dotProduct / magnitudesProduct) * (180 / Math.PI);

        return angle <= _clusterConfiguration.DirectionToleranceDegrees;
    }

    private double[] ComputeDirectionVector(List<VehicleInfo> info)
    {
        var directionVector = new double[2]; // [latitude difference, longitude difference]

        if (info.Count < 2)
            return directionVector;

        double latitudeDiff = info[0].Location.Latitude - info[1].Location.Latitude;
        double longitudeDiff = info[0].Location.Longitude - info[1].Location.Longitude;

        directionVector[0] = latitudeDiff;
        directionVector[1] = longitudeDiff;

        return directionVector;
    }

    private double DotProduct(double[] vector1, double[] vector2)
    {
        return vector1[0] * vector2[0] + vector1[1] * vector2[1];
    }

    private double Magnitude(double[] vector)
    {
        return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1]);
    }
}
