using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IVehicleClusterService
{
    List<Cluster> ClusterVehicles(List<Vehicle> vehicles);
}