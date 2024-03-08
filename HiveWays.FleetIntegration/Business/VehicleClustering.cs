using System.Text.Json;
using HiveWays.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClustering : IVehicleClustering
{
    private readonly ILogger<VehicleClustering> _logger;

    public VehicleClustering(ILogger<VehicleClustering> logger)
    {
        _logger = logger;
    }

    // TODO: add car length parameters, consider the orientation (same area but opposite directions => different clusters)
    public List<VehicleCluster> KMeans(List<VehicleStats> cars, int k)
    {
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
                Longitude = CalculateMean(cluster.VehicleStats, c => c.Longitude)
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
