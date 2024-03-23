using HiveWays.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClustering
{
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClustering> _logger;

    private List<Cluster> _clusters = new(); // We should write and get these from the cache

    public VehicleClustering(ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClustering> logger)
    {
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public void InitializeClusters(List<Vehicle> vehicles)
    {
        if (_clusters.Any())
            return;

        foreach (var vehicle in vehicles)
        {
            if (vehicle.IsAssignedToCluster) 
                continue;

            var cluster = CreateCluster(vehicle);
            AssignNearbyVehiclesToCluster(cluster, vehicles);
        }
    }

    private Cluster CreateCluster(Vehicle vehicle)
    {
        var cluster = new Cluster();
        cluster.AddVehicle(vehicle);
        return cluster;
    }

    private void AssignNearbyVehiclesToCluster(Cluster cluster, List<Vehicle> vehicles)
    {
        var nearbyVehicles = FindNearbyVehicles(cluster, vehicles);

        foreach (var nearbyVehicle in nearbyVehicles)
        {
            if (!nearbyVehicle.IsAssignedToCluster)
            {
                // Check if nearbyVehicle's direction matches cluster's direction
                if (IsSameDirection(cluster, nearbyVehicle))
                {
                    cluster.AddVehicle(nearbyVehicle);
                    nearbyVehicle.IsAssignedToCluster = true;
                    AssignNearbyVehiclesToCluster(cluster, vehicles); // Recursively assign nearby vehicles
                }
            }
        }
    }

    private List<Vehicle> FindNearbyVehicles(Cluster cluster, List<Vehicle> vehicles)
    {
        return vehicles.Where(v => !v.IsAssignedToCluster && 
                                   Distance(cluster.Center, v.MedianInfo.Location) <= _clusterConfiguration.ClusterRadius)
            .ToList();
    }

    private bool IsSameDirection(Cluster cluster, Vehicle vehicle)
    {
        // Logic to compute direction and compare with cluster's direction
        // You need to implement your own logic here to compare directions
        return true; // For demonstration purposes, assuming direction is same
    }

    private decimal Distance(GeoPoint point1, GeoPoint point2)
    {
        // Implementation of Haversine formula or any other suitable method
        // to compute distance between two points
        // This method would depend on the geographic coordinates of the vehicles
        // and the formula you choose to calculate distance
        return 0; // Placeholder, implement your distance calculation logic
    }
}
