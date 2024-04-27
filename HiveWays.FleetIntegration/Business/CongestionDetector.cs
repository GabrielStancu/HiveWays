using System.Collections.Concurrent;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class CongestionDetector : ICongestionDetector
{
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly CongestionConfiguration _congestionConfiguration;
    private readonly ILogger<CongestionDetector> _logger;

    public CongestionDetector(IDistanceCalculator distanceCalculator,
        CongestionConfiguration congestionConfiguration,
        ILogger<CongestionDetector> logger)
    {
        _distanceCalculator = distanceCalculator;
        _congestionConfiguration = congestionConfiguration;
        _logger = logger;
    }

    public IEnumerable<Cluster> ComputeCongestedClusters(IEnumerable<Cluster> clusters)
    {
        var congestedClusters = new ConcurrentBag<Cluster>();

        Parallel.ForEach(clusters, cluster =>
        {
            if (!IsCongested(cluster)) 
                return;

            _logger.LogInformation("Congestion detected for cluster {CongestedCluster}", cluster.Id);
            congestedClusters.Add(cluster);
        });

        return congestedClusters;
    }

    private bool IsCongested(Cluster cluster)
    {
        int vehiclesCount = cluster.Vehicles.Count;
        double averageSpeed = 0;
        double totalAcceleration = 0;
        double totalDistance = 0;

        foreach (var vehicle in cluster.Vehicles)
        {
            foreach (var info in vehicle.Trajectory.OrderBy(i => i.Timestamp))
            {
                averageSpeed += info.SpeedKmph;
                totalAcceleration += Math.Abs(info.AccelerationKmph);

                if (info != vehicle.Trajectory[0])
                {
                    totalDistance += _distanceCalculator.Distance(info.Location, vehicle.Trajectory[vehicle.Trajectory.IndexOf(info) - 1].Location);
                }
            }
        }

        int totalInfoPoints = cluster.Vehicles.Sum(v => v.Trajectory.Count);
        averageSpeed /= totalInfoPoints;
        double density = vehiclesCount / totalDistance;

        return vehiclesCount >= _congestionConfiguration.MinVehicles &&
               (averageSpeed < _congestionConfiguration.MinSpeed ||
               density > _congestionConfiguration.MaxDensity ||
               totalAcceleration / totalInfoPoints < _congestionConfiguration.MinAcceleration);
    }
}
