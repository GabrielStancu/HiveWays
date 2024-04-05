using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IVehicleClusterManager
{
    List<Cluster> ClusterVehicles(List<Vehicle> vehicles);
}