using HiveWays.Domain.Models;
using System.Text.Json.Serialization;

namespace HiveWays.Domain.Items;

public class CarItem : BaseItem
{
    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("fabricationYear")]
    public int FabricationYear { get; set; }

    [JsonPropertyName("fuelType")]
    public FuelType FuelType { get; set; }

    [JsonPropertyName("range")]
    public decimal Range { get; set; }

    [JsonPropertyName("objectType")]
    public override ObjectType ObjectType => ObjectType.Car;
}
