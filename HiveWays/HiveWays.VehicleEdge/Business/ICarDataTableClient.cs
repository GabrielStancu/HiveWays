using Azure;
using HiveWays.VehicleEdge.Models;

namespace HiveWays.VehicleEdge.Business;

public interface ICarDataTableClient
{
    Task WriteCarDataAsync(IEnumerable<ValueEntity> entities);
}