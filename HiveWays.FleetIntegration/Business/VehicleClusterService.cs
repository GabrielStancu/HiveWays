using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;

public class VehicleClusterService : IVehicleClusterService
{
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly IDirectionCalculator _directionCalculator;
    private readonly ClusterConfiguration _clusterConfiguration;
    private readonly ILogger<VehicleClusterService> _logger;

    private List<Cluster> _clusters = new();
    private int _id;

    public VehicleClusterService(IDistanceCalculator distanceCalculator,
        IDirectionCalculator directionCalculator,
        ClusterConfiguration clusterConfiguration,
        ILogger<VehicleClusterService> logger)
    {
        _distanceCalculator = distanceCalculator;
        _directionCalculator = directionCalculator;
        _clusterConfiguration = clusterConfiguration;
        _logger = logger;
    }

    public List<Cluster> ClusterVehicles(List<Vehicle> vehicles)
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
        if (cluster.Vehicles.Count > _clusterConfiguration.MaxVehicles)
            return;

        var nearbyVehicles = FindNearbyVehicles(clusterHead, vehicles);

        foreach (var nearbyVehicle in nearbyVehicles)
        {
            TryAddNearbyVehicleToCluster(cluster, clusterHead, vehicles, nearbyVehicle);
        }
    }

    private List<Vehicle> FindNearbyVehicles(Vehicle clusterHead, List<Vehicle> vehicles)
    {
        return vehicles
            .Where(v => !v.IsAssignedToCluster && _distanceCalculator.IsWithinDistanceToCluster(clusterHead, v))
            .ToList();
    }

    private void TryAddNearbyVehicleToCluster(Cluster cluster, Vehicle clusterHead, List<Vehicle> vehicles, Vehicle nearbyVehicle)
    {
        if (nearbyVehicle.IsAssignedToCluster)
            return;

        if (!_directionCalculator.IsSameDirection(clusterHead, nearbyVehicle))
            return;

        cluster.AddVehicle(nearbyVehicle);
        nearbyVehicle.IsAssignedToCluster = true;
        AssignNearbyVehiclesToCluster(cluster, nearbyVehicle, vehicles);
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

        AssignIncomingVehiclesToClusters(vehicles);
    }

    private void AssignIncomingVehiclesToClusters(List<Vehicle> vehicles)
    {
        var incomingVehicles = vehicles.Where(v => !v.IsAssignedToCluster);
        var initialClusters = new List<Cluster>(_clusters);

        foreach (var vehicle in incomingVehicles)
        {
            foreach (var cluster in initialClusters)
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
            RemoveStoppedVehicle(vehicle, cluster);
            return;
        }

        var distanceToCenter = _distanceCalculator.Distance(cluster.Center, incomingVehicle.MedianLocation.Location);
        if (distanceToCenter > _clusterConfiguration.ClusterRadius)
        {
            FindNewSuitableCluster(vehicles, cluster, incomingVehicle);
        }
        else
        {
            AdjustClusterOnDirectionChange(vehicles, cluster, incomingVehicle);
        }

        vehicle.IsAssignedToCluster = true;
    }

    private void RemoveStoppedVehicle(Vehicle vehicle, Cluster cluster)
    {
        _logger.LogWarning("Vehicle {VehicleId} from {ClusterId} not found in incoming vehicles", vehicle.Id,
            cluster.Id);

        cluster.RemoveVehicle(vehicle.Id);
        if (cluster.Center is null || !cluster.Vehicles.Any())
        {
            _clusters.Remove(cluster);
        }
    }

    private void FindNewSuitableCluster(List<Vehicle> vehicles, Cluster cluster, Vehicle incomingVehicle)
    {
        cluster.RemoveVehicle(incomingVehicle.Id);
        if (cluster.Center is null || !cluster.Vehicles.Any())
        {
            _clusters.Remove(cluster);
        }

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

    private void AdjustClusterOnDirectionChange(List<Vehicle> vehicles, Cluster cluster, Vehicle incomingVehicle)
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
            RemoveVehicleWithDifferentDirection(vehicles, cluster, incomingVehicle);
        }
    }

    private void RemoveVehicleWithDifferentDirection(List<Vehicle> vehicles, Cluster cluster, Vehicle incomingVehicle)
    {
        cluster.RemoveVehicle(incomingVehicle.Id);
        if (cluster.Center is null || !cluster.Vehicles.Any())
        {
            _clusters.Remove(cluster);
        }

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

    private Cluster FindSuitableCluster(Vehicle vehicle, List<Vehicle> vehicles)
    {
        Cluster closestCluster = null;
        double minDistance = double.MaxValue;

        foreach (var cluster in _clusters)
        {
            if (cluster.Vehicles.Count >= _clusterConfiguration.MaxVehicles)
                continue;

            var distanceToCenter = _distanceCalculator.Distance(cluster.Center, vehicle.MedianLocation.Location);
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
