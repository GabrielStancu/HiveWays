using System.Runtime.Serialization;

namespace HiveWays.Domain.Documents;

public enum FuelType
{
    [EnumMember(Value = "Diesel")]
    Diesel,
    [EnumMember(Value = "Petrol")]
    Petrol,
    [EnumMember(Value = "Gas")]
    Gas,
    [EnumMember(Value = "Electric")]
    Electric,
    [EnumMember(Value = "Mixed")]
    Mixed,
    [EnumMember(Value = "No Fuel")]
    NoFuel
}
