using System.Text.Json.Serialization;
using HiveWays.Domain.Models;

namespace HiveWays.Domain.Items;

public abstract class BaseItem
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("externalId")]
    public int ExternalId { get; set; }

    [JsonPropertyName("fuelType")]
    public abstract ObjectType ObjectType { get; }
}
