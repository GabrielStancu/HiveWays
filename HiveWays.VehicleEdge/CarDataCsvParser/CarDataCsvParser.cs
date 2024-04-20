using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace HiveWays.VehicleEdge.CarDataCsvParser;

public class CarDataCsvParser<T> : ICarDataCsvParser<T>
{
    public List<T> ParseCsv(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            HasHeaderRecord = true
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<T>();

        return records.ToList();
    }
}
