using System.Text.Json.Serialization;

namespace HiveWays.VehicleEdge.Models;

public class Value
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("dp")]
    public List<DataPoint> Dp { get; set; }
}
