using System.Text.Json;
using HiveWays.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClustering : IVehicleClustering
{
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClustering> _logger;

    private List<VehicleCluster> _vehicleClusters = new();

    public VehicleClustering(ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClustering> logger)
    {
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public List<VehicleCluster> KMeans(List<VehicleStats> cars)
    {
        if (!_vehicleClusters.Any())
        {
            int k = ComputeClustersCount(cars.Count);
            // Initialize cluster centers (randomly or using a strategy)
            _vehicleClusters = InitializeClusters(cars, k);
        }

        while (true)
        {
            // Assign each car to the nearest cluster
            AssignToClusters(cars);

            // Save current cluster centers for convergence check
            List<VehicleCluster> oldClusters = new List<VehicleCluster>(_vehicleClusters);

            // Recalculate cluster centers
            RecalculateCenters();

            // Check for convergence
            if (ClustersConverged(oldClusters, _vehicleClusters))
            {
                break;
            }
        }

        return _vehicleClusters;
    }

    private int ComputeClustersCount(int carsCount)
    {
        var area = _clusterConfiguration.ClusterRadius * _clusterConfiguration.ClusterRadius * double.Pi;
        var carArea = (_clusterConfiguration.VehicleLength + _clusterConfiguration.VehicleDistance) *
                      (_clusterConfiguration.VehicleWidth + _clusterConfiguration.VehicleDistance);
        var carsPerCluster = Math.Ceiling(area / carArea);

        return 1 + carsCount / (int)carsPerCluster;
    }

    private List<VehicleCluster> InitializeClusters(List<VehicleStats> cars, int k)
    {
        Random random = new Random();
        List<VehicleCluster> clusters = new List<VehicleCluster>();

        for (int i = 0; i < k; i++)
        {
            // Randomly select initial cluster centers from the cars
            int randomIndex = random.Next(cars.Count);
            var randomVehicle = cars[randomIndex];
            var cluster = new VehicleCluster { VehicleStats = new List<VehicleStats> { randomVehicle } };
            
            clusters.Add(cluster);
            AdjustClusterAverages(cluster, randomVehicle);
        }

        return clusters;
    }

    private void AssignToClusters(List<VehicleStats> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            if (_vehicleClusters.Any(c => c.VehicleStats.Any(v => v.Id == vehicle.Id)))
                continue;

            // Find the nearest cluster based on the distance metric
            var nearestCluster = _vehicleClusters.MinBy(cluster => CalculateDistance(vehicle, cluster));

            if (nearestCluster is null)
            {
                _logger.LogError("Could not find nearest cluster for point {NotAssignedPoint}", JsonSerializer.Serialize(vehicle));
                return;
            }

            // Assign the car to the nearest cluster
            nearestCluster.VehicleStats.Add(vehicle);

            // Recompute the averages for the cluster
            AdjustClusterAverages(nearestCluster, vehicle);
        }
    }

    private double CalculateDistance(VehicleStats vehicle, VehicleCluster cluster)
    {
        cluster.AverageOrientation ??= ComputeMean(cluster.VehicleStats, c => c.Heading);
        var hasClusterOrientation = cluster.AverageOrientation - _clusterConfiguration.OrientationLimit <= vehicle.Heading
                                    && cluster.AverageOrientation + _clusterConfiguration.OrientationLimit >= vehicle.Heading;

        if (!hasClusterOrientation)
            return double.PositiveInfinity;

        // Euclidean distance
        cluster.CenterLatitude ??= ComputeMean(cluster.VehicleStats, c => c.Latitude);
        cluster.CenterLongitude ??= ComputeMean(cluster.VehicleStats, c => c.Longitude);

        var latitudeDeviation = vehicle.Latitude - cluster.CenterLatitude;
        var longitudeDeviation = vehicle.Longitude - cluster.CenterLongitude;
        var latitudeDistance = Math.Pow((double)latitudeDeviation, 2);
        var longitudeDistance = Math.Pow((double)longitudeDeviation, 2);
        
        return Math.Sqrt(latitudeDistance + longitudeDistance);
    }

    private decimal ComputeMean(List<VehicleStats> vehicles, Func<VehicleStats, decimal> propertySelector)
    {
        // Helper method to calculate the mean of a property for a list of cars
        return vehicles.Average(propertySelector);
    }

    private void RecalculateCenters()
    {
        foreach (VehicleCluster cluster in _vehicleClusters)
        {
            // Assign the vehicle ids to the clusters before recomputing the centers
            cluster.VehicleIds = cluster.VehicleStats
                .Where(v => !v.IsComputedClusterCenter)
                .Select(v => v.Id)
                .ToList();

            // Calculate the mean for each property in the cluster and set it as the new center
            cluster.VehicleStats = new List<VehicleStats> { new()
            {
                Latitude = ComputeMean(cluster.VehicleStats, c => c.Latitude),
                Longitude = ComputeMean(cluster.VehicleStats, c => c.Longitude),
                AccelerationKmph = ComputeMean(cluster.VehicleStats, c => c.AccelerationKmph),
                Heading = ComputeMean(cluster.VehicleStats, c => c.Heading),
                SpeedKmph = ComputeMean(cluster.VehicleStats, c => c.SpeedKmph),
                Timestamp = DateTime.UtcNow,
                Id = _vehicleClusters.IndexOf(cluster),
                IsComputedClusterCenter = true
            }};
        }
    }

    private void AdjustClusterAverages(VehicleCluster cluster, VehicleStats vehicle)
    {
        cluster.AverageOrientation ??= decimal.Zero;
        cluster.AverageOrientation =
            (cluster.AverageOrientation * (cluster.VehicleStats.Count - 1) + vehicle.Heading) /
            cluster.VehicleStats.Count;

        cluster.CenterLatitude ??= decimal.Zero;
        cluster.CenterLatitude =
            (cluster.CenterLatitude * (cluster.VehicleStats.Count - 1) + vehicle.Latitude) /
            cluster.VehicleStats.Count;

        cluster.CenterLongitude ??= decimal.Zero;
        cluster.CenterLongitude =
            (cluster.CenterLongitude * (cluster.VehicleStats.Count - 1) + vehicle.Longitude) /
            cluster.VehicleStats.Count;
    }

    private bool ClustersConverged(List<VehicleCluster> oldClusters, List<VehicleCluster> newClusters)
    {
        // Check for convergence by comparing old and new cluster centers
        return oldClusters.SelectMany(oldCluster => oldCluster.VehicleStats)
            .SequenceEqual(newClusters.SelectMany(newCluster => newCluster.VehicleStats));
    }
}
