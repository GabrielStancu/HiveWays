using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ICongestionDetectionService
{
    IEnumerable<Cluster> ComputeCongestedClusters(IEnumerable<Cluster> clusters);
}