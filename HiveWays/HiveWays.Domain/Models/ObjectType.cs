using System.Runtime.Serialization;

namespace HiveWays.Domain.Models;

public enum ObjectType
{
    [EnumMember(Value = "Car")]
    Car,
    [EnumMember(Value = "Obstacle")]
    Obstacle,
    [EnumMember(Value = "Traffic Light")]
    TrafficLight
}
