using CsvHelper;
using CsvHelper.Configuration;
using HiveWays.Business.CarDataCsvParser;
using HiveWays.Domain.Models;
using System.Globalization;

namespace HiveWays.Infrastructure.Services;

public class CarDataCsvParser : ICarDataCsvParser
{
    public List<DataPoint> ParseCsv(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        var dataPoints = csv.GetRecords<DataPoint>();

        return dataPoints.ToList();
    }
}
