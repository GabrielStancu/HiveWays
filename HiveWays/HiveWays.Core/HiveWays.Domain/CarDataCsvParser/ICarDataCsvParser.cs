using HiveWays.Domain.Models;

namespace HiveWays.Business.CarDataCsvParser;

public interface ICarDataCsvParser
{
    List<DataPoint> ParseCsv(Stream stream);
}