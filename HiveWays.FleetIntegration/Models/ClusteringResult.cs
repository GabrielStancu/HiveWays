using HiveWays.Domain.Documents;
using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Models;

public class ClusteringResult : BaseDocument
{
    public List<Cluster> Clusters { get; set; }
    public List<Cluster> CongestedClusters { get; set; }
}
