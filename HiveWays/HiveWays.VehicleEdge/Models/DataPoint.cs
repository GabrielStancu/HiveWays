using System.Text.Json.Serialization;

namespace HiveWays.VehicleEdge.Models;

public class DataPoint
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    [JsonPropertyName("ts")]
    public string Ts { get; set; }
}
