using System.Text.Json;
using HiveWays.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClustering : IVehicleClustering
{
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClustering> _logger;

    public VehicleClustering(ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClustering> logger)
    {
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public List<VehicleCluster> KMeans(List<VehicleStats> cars)
    {
        int k = ComputeClustersCount(cars.Count);

        // Initialize cluster centers (randomly or using a strategy)
        List<VehicleCluster> clusters = InitializeClusters(cars, k);

        while (true)
        {
            // Assign each car to the nearest cluster
            AssignToClusters(cars, clusters);

            // Save current cluster centers for convergence check
            List<VehicleCluster> oldClusters = new List<VehicleCluster>(clusters);

            // Recalculate cluster centers
            RecalculateCenters(clusters);

            // Check for convergence
            if (ClustersConverged(oldClusters, clusters))
            {
                break;
            }
        }

        return clusters;
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
            clusters.Add(new VehicleCluster { VehicleStats = new List<VehicleStats> { cars[randomIndex] } });
        }

        return clusters;
    }

    private void AssignToClusters(List<VehicleStats> vehicles, List<VehicleCluster> clusters)
    {
        foreach (VehicleStats vehicle in vehicles)
        {
            // Find the nearest cluster based on the distance metric
            var nearestCluster = clusters.MinBy(cluster => CalculateDistance(vehicle, cluster));

            if (nearestCluster is null)
            {
                _logger.LogError("Could not find nearest cluster for point {NotAssignedPoint}", JsonSerializer.Serialize(vehicle));
                return;
            }

            // Assign the car to the nearest cluster
            nearestCluster.VehicleStats.Add(vehicle);
        }
    }

    private double CalculateDistance(VehicleStats vehicle, VehicleCluster cluster)
    {
        var clusterOrientation = CalculateMean(cluster.VehicleStats, c => c.Heading);
        var hasClusterOrientation = clusterOrientation - _clusterConfiguration.OrientationLimit <= vehicle.Heading
                                    && clusterOrientation + _clusterConfiguration.OrientationLimit >= vehicle.Heading;

        if (!hasClusterOrientation)
            return double.PositiveInfinity;

        // Euclidean distance
        var latitudeDeviation = vehicle.Latitude - CalculateMean(cluster.VehicleStats, c => c.Latitude);
        var longitudeDeviation = vehicle.Longitude - CalculateMean(cluster.VehicleStats, c => c.Longitude);
        var latitudeDistance = Math.Pow((double)latitudeDeviation, 2);
        var longitudeDistance = Math.Pow((double)longitudeDeviation, 2);
        
        return Math.Sqrt(latitudeDistance + longitudeDistance);
    }

    private decimal CalculateMean(List<VehicleStats> vehicles, Func<VehicleStats, decimal> propertySelector)
    {
        // Helper method to calculate the mean of a property for a list of cars
        return vehicles.Average(propertySelector);
    }

    private void RecalculateCenters(List<VehicleCluster> clusters)
    {
        foreach (VehicleCluster cluster in clusters)
        {
            // Calculate the mean for each property in the cluster and set it as the new center
            cluster.VehicleStats = new List<VehicleStats> { new()
            {
                Latitude = CalculateMean(cluster.VehicleStats, c => c.Latitude),
                Longitude = CalculateMean(cluster.VehicleStats, c => c.Longitude),
                AccelerationKmph = CalculateMean(cluster.VehicleStats, c => c.AccelerationKmph),
                Heading = CalculateMean(cluster.VehicleStats, c => c.Heading),
                SpeedKmph = CalculateMean(cluster.VehicleStats, c => c.SpeedKmph),
                Timestamp = DateTime.UtcNow,
                Id = clusters.IndexOf(cluster)
            }};
        }
    }

    private bool ClustersConverged(List<VehicleCluster> oldClusters, List<VehicleCluster> newClusters)
    {
        // Check for convergence by comparing old and new cluster centers
        return oldClusters.SelectMany(oldCluster => oldCluster.VehicleStats)
            .SequenceEqual(newClusters.SelectMany(newCluster => newCluster.VehicleStats));
    }
}
