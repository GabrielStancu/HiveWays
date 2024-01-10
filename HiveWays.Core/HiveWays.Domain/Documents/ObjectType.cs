using System.Runtime.Serialization;

namespace HiveWays.Domain.Documents;

public enum ObjectType
{
    [EnumMember(Value = "Unknown")]
    Unknown,
    [EnumMember(Value = "Car")]
    Car,
    [EnumMember(Value = "Obstacle")]
    Obstacle,
    [EnumMember(Value = "Traffic Light")]
    TrafficLight
}
