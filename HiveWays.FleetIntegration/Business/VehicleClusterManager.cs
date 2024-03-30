using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClusterManager : IVehicleClusterManager
{
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly IDirectionCalculator _directionCalculator;
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClusterManager> _logger;

    private List<Cluster> _clusters = new(); // We should write and get these from the cache; when writing to cache, map Vehicles to VehicleInfo, compute averages in new class for cached cluster
    private int _id;

    public VehicleClusterManager(IDistanceCalculator distanceCalculator,
        IDirectionCalculator directionCalculator,
        ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClusterManager> logger)
    {
        _distanceCalculator = distanceCalculator;
        _directionCalculator = directionCalculator;
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public async Task<List<Cluster>> ClusterVehiclesAsync(List<Vehicle> vehicles)
    {
        if (!_clusters.Any())
        {
            _logger.LogInformation("No clusters found, initializing the clusters...");
            _clusters = InitializeClusters(vehicles);

            return _clusters;
        }

        _logger.LogInformation("Clusters found, reorganizing the clusters...");
        ReorganizeClusters(vehicles);
        
        return _clusters;
    }

    private List<Cluster> InitializeClusters(List<Vehicle> vehicles)
    {
        var clusters = new List<Cluster>();

        foreach (var vehicle in vehicles)
        {
            if (vehicle.IsAssignedToCluster)
                continue;

            var cluster = CreateCluster(vehicle);
            AssignNearbyVehiclesToCluster(cluster, vehicle, vehicles);
            clusters.Add(cluster);
            vehicle.IsAssignedToCluster = true;
        }

        return clusters;
    }

    private Cluster CreateCluster(Vehicle vehicle)
    {
        var cluster = new Cluster
        {
            Id = ++_id
        };
        cluster.AddVehicle(vehicle);
        vehicle.IsAssignedToCluster = true;

        return cluster;
    }

    private void AssignNearbyVehiclesToCluster(Cluster cluster, Vehicle clusterHead, List<Vehicle> vehicles)
    {
        var nearbyVehicles = FindNearbyVehicles(clusterHead, vehicles);

        foreach (var nearbyVehicle in nearbyVehicles)
        {
            if (nearbyVehicle.IsAssignedToCluster)
                continue;

            if (!_directionCalculator.IsSameDirection(clusterHead, nearbyVehicle))
                continue;

            cluster.AddVehicle(nearbyVehicle);
            nearbyVehicle.IsAssignedToCluster = true;
            AssignNearbyVehiclesToCluster(cluster, nearbyVehicle, vehicles);
        }
    }

    private List<Vehicle> FindNearbyVehicles(Vehicle clusterHead, List<Vehicle> vehicles)
    {
        return vehicles
            .Where(v => !v.IsAssignedToCluster && _distanceCalculator.IsWithinDistanceToCluster(clusterHead, v))
            .ToList();
    }

    private void ReorganizeClusters(List<Vehicle> vehicles)
    {
        foreach (var cluster in _clusters.ToList())
        {
            foreach (var vehicle in cluster.Vehicles.ToList())
            {
                ProcessClusterVehicle(vehicles, vehicle, cluster);
            }
        }
    }

    private void ProcessClusterVehicle(List<Vehicle> vehicles, Vehicle vehicle, Cluster cluster)
    {
        var incomingVehicle = vehicles.FirstOrDefault(v => v.Id == vehicle.Id);
        if (incomingVehicle is null)
        {
            _logger.LogWarning("Vehicle {VehicleId} from {ClusterId} not found in incoming vehicles", vehicle.Id,
                cluster.Id);
            cluster.RemoveVehicle(vehicle.Id);
            return;
        }

        var distanceToCenter = _distanceCalculator.Distance(cluster.Center, incomingVehicle.MedianInfo.Location);

        if (distanceToCenter > _clusterConfiguration.ClusterRadius)
        {
            cluster.RemoveVehicle(incomingVehicle.Id);

            var suitableCluster = FindSuitableCluster(incomingVehicle, vehicles);
            if (suitableCluster != null)
            {
                suitableCluster.AddVehicle(incomingVehicle);
            }
            else
            {
                var newCluster = CreateCluster(incomingVehicle);
                _clusters.Add(newCluster);
            }
        }
        else
        {
            var clusterHead = vehicles
                .FirstOrDefault(v => cluster.Vehicles
                    .Any(cv => cv.Id == v.Id));

            if (clusterHead is null)
            {
                _logger.LogWarning("Could not find cluster head for cluster {ClusterId}", cluster.Id);
                return;
            }

            if (!_directionCalculator.IsSameDirection(clusterHead, incomingVehicle))
            {
                cluster.RemoveVehicle(incomingVehicle.Id);
                var suitableCluster = FindSuitableCluster(incomingVehicle, vehicles);

                if (suitableCluster != null)
                {
                    suitableCluster.AddVehicle(incomingVehicle);
                }
                else
                {
                    var newCluster = CreateCluster(incomingVehicle);
                    _clusters.Add(newCluster);
                }
            }
        }
    }

    private Cluster? FindSuitableCluster(Vehicle vehicle, List<Vehicle> vehicles)
    {
        Cluster? closestCluster = null;
        double minDistance = double.MaxValue;

        foreach (var cluster in _clusters)
        {
            var distanceToCenter = _distanceCalculator.Distance(cluster.Center, vehicle.MedianInfo.Location);
            var clusterHead = vehicles
                .FirstOrDefault(v => cluster.Vehicles
                    .Any(cv => cv.Id == v.Id));

            if (clusterHead is null)
            {
                _logger.LogWarning("Could not find cluster head for cluster {ClusterId}", cluster.Id);
                return closestCluster;
            }

            if (distanceToCenter <= _clusterConfiguration.ClusterRadius && 
                _directionCalculator.IsSameDirection(clusterHead, vehicle))
            {
                if (distanceToCenter < minDistance)
                {
                    minDistance = distanceToCenter;
                    closestCluster = cluster;
                }
            }
        }

        return closestCluster;
    }
}
