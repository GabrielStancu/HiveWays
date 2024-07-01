using HiveWays.Domain.Documents;
using HiveWays.Domain.Models;

namespace HiveWays.VehicleEdge.Models;

public class VehicleData : BaseDocument
{
    public DataPoint DataPoint { get; set; }
    public DateTime Timestamp { get; set; }
    public string RoadId { get; set; }
}
