using System.Text.Json.Serialization;

namespace HiveWays.VehicleEdge.Models;
public class Batch
{
    [JsonPropertyName("values")]
    public List<Value> Values { get; set; }
}
