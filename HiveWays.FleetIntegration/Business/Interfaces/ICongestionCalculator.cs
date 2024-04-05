using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface ICongestionCalculator
{
    IEnumerable<Cluster> ComputeCongestedClusters(IEnumerable<Cluster> clusters);
}