using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class CongestionCalculator : ICongestionCalculator
{
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly CongestionConfiguration _congestionConfiguration;
    private readonly ILogger<CongestionCalculator> _logger;

    public CongestionCalculator(IDistanceCalculator distanceCalculator,
        CongestionConfiguration congestionConfiguration,
        ILogger<CongestionCalculator> logger)
    {
        _distanceCalculator = distanceCalculator;
        _congestionConfiguration = congestionConfiguration;
        _logger = logger;
    }

    public IEnumerable<Cluster> ComputeCongestedClusters(IEnumerable<Cluster> clusters)
    {
        foreach (var cluster in clusters)
        {
            if (IsCongested(cluster))
            {
                _logger.LogInformation("Congestion detected for cluster {CongestedCluster}", cluster.Id);
                yield return cluster;
            }
        }
    }

    private bool IsCongested(Cluster cluster)
    {
        int vehiclesCount = cluster.Vehicles.Count;
        double averageSpeed = 0;
        double totalAcceleration = 0;
        double totalDistance = 0;

        foreach (var vehicle in cluster.Vehicles)
        {
            foreach (var info in vehicle.Info)
            {
                averageSpeed += info.SpeedKmph;
                totalAcceleration += Math.Abs(info.AccelerationKmph);

                if (info != vehicle.Info[0])
                {
                    totalDistance += _distanceCalculator.Distance(info.Location, vehicle.Info[vehicle.Info.IndexOf(info) - 1].Location);
                }
            }
        }

        int totalInfoPoints = cluster.Vehicles.Sum(v => v.Info.Count);
        averageSpeed /= totalInfoPoints;
        double density = vehiclesCount / totalDistance;

        return vehiclesCount >= _congestionConfiguration.MinVehicles &&
               averageSpeed < _congestionConfiguration.MinSpeed &&
               density > _congestionConfiguration.MaxDensity &&
               totalAcceleration / totalInfoPoints > _congestionConfiguration.MaxAcceleration;
    }
}
