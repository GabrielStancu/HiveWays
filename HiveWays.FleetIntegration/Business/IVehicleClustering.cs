using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business;

public interface IVehicleClustering
{
    List<VehicleCluster> KMeans(List<VehicleStats> cars);
}