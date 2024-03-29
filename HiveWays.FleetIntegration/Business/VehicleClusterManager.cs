using HiveWays.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClusterManager : IVehicleClusterManager
{
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClusterManager> _logger;
    private List<Cluster> _clusters = new(); // We should write and get these from the cache
    private const double EarthRadiusKm = 6371.0;

    public VehicleClusterManager(ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClusterManager> logger)
    {
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public async Task<List<Cluster>> ClusterVehiclesAsync(List<Vehicle> vehicles)
    {
        if (!_clusters.Any())
        {
            InitializeClusters(vehicles);
            return _clusters;
        }

        await ReorganizeClustersAsync(vehicles);
        return _clusters;
    }

    private void InitializeClusters(List<Vehicle> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            if (vehicle.IsAssignedToCluster) 
                continue;

            vehicle.IsAssignedToCluster = true;
            var cluster = CreateCluster(vehicle);
            AssignNearbyVehiclesToCluster(cluster, vehicle, vehicles);
            _clusters.Add(cluster);
        }
    }

    private async Task ReorganizeClustersAsync(List<Vehicle> vehicles)
    {
        await Task.Delay(10);
    }

    private Cluster CreateCluster(Vehicle vehicle)
    {
        var cluster = new Cluster
        {
            Id = _clusters.Max(c => c.Id) + 1
        };
        cluster.AddVehicle(vehicle);

        return cluster;
    }

    private void AssignNearbyVehiclesToCluster(Cluster cluster, Vehicle clusterHead, List<Vehicle> vehicles)
    {
        var nearbyVehicles = FindNearbyVehicles(clusterHead, vehicles);

        foreach (var nearbyVehicle in nearbyVehicles)
        {
            if (nearbyVehicle.IsAssignedToCluster) 
                continue;

            if (!IsSameDirection(clusterHead, nearbyVehicle)) 
                continue;

            cluster.AddVehicle(nearbyVehicle);
            nearbyVehicle.IsAssignedToCluster = true;
            AssignNearbyVehiclesToCluster(cluster, nearbyVehicle, vehicles);
        }
    }

    private List<Vehicle> FindNearbyVehicles(Vehicle clusterHead, List<Vehicle> vehicles)
    {
        return vehicles
            .Where(v => !v.IsAssignedToCluster && IsWithinDistanceToCluster(clusterHead, v))
            .ToList();
    }

    private bool IsSameDirection(Vehicle clusterHead, Vehicle nearbyVehicle)
    {
        // Logic to compute direction and compare with cluster's direction
        // You need to implement your own logic here to compare directions
        return true; // For demonstration purposes, assuming direction is same
    }

    private bool IsWithinDistanceToCluster(Vehicle clusterHead, Vehicle nearbyVehicle)
        => Distance(clusterHead.MedianInfo.Location, nearbyVehicle.MedianInfo.Location) <=
           _clusterConfiguration.ClusterRadius;

    private double Distance(GeoPoint point1, GeoPoint point2)
    {
        double distanceLat = ToRadians(point2.Latitude - point1.Latitude);
        double distanceLon = ToRadians(point2.Longitude - point1.Longitude);

        double a = Math.Sin(distanceLat / 2) * Math.Sin(distanceLat / 2) +
                   Math.Cos(ToRadians(point1.Latitude)) * Math.Cos(ToRadians(point2.Latitude)) *
                   Math.Sin(distanceLon / 2) * Math.Sin(distanceLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
