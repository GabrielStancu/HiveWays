import { Cluster } from "./cluster.model";

export class ClusteringResult {
  constructor(public Clusters: Cluster[],
    public CongestedClusters: Cluster[]
  ) {}
}
