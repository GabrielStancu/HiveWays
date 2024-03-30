using HiveWays.Domain.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IDirectionCalculator
{
    bool IsSameDirection(Vehicle clusterHead, Vehicle nearbyVehicle);
}