using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ICongestionDetector
{
    IEnumerable<Cluster> ComputeCongestedClusters(IEnumerable<Cluster> clusters);
}