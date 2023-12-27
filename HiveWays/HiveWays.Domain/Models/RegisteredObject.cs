using System.Text.Json.Serialization;

namespace HiveWays.Domain.Models;

public class RegisteredObject
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("carExternalId")]
    public int CarExternalId { get; set; }

    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("fabricationYear")]
    public int FabricationYear { get; set; }

    [JsonPropertyName("fuelType")]
    public FuelType FuelType { get; set; }

    [JsonPropertyName("fuelType")]
    public ObjectType ObjectType { get; set; }

    [JsonPropertyName("range")]
    public decimal Range { get; set; }
}
