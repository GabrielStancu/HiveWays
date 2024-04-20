namespace HiveWays.VehicleEdge.CarDataCsvParser;

public interface ICarDataCsvParser<T>
{
    List<T> ParseCsv(Stream stream);
}