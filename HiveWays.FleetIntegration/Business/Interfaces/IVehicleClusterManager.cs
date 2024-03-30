using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IVehicleClusterManager
{
    Task<List<Cluster>> ClusterVehiclesAsync(List<Vehicle> vehicles);
}