using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using HiveWays.Domain.Models;

namespace HiveWays.VehicleEdge.CarDataCsvParser;

public class CarDataCsvParser : ICarDataCsvParser
{
    public List<DataPoint> ParseCsv(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            HasHeaderRecord = true
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        var dataPoints = csv.GetRecords<DataPoint>();

        return dataPoints.ToList();
    }
}
