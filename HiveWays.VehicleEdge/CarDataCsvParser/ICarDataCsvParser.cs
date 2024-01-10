using HiveWays.Domain.Models;

namespace HiveWays.VehicleEdge.CarDataCsvParser;

public interface ICarDataCsvParser
{
    List<DataPoint> ParseCsv(Stream stream);
}