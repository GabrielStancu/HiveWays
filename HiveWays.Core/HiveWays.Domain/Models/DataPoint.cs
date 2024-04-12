using CsvHelper.Configuration.Attributes;

namespace HiveWays.Domain.Models;

public class DataPoint : IIdentifiable
{
    [Name("#time")]
    public double TimeOffsetSeconds { get; set; }

    [Name("id")]
    public int Id { get; set; }

    [Name("x[m]")]
    public double X { get; set; }

    [Name("y[m]")]
    public double Y { get; set; }

    [Name("speed[m/s]")]
    public double Speed { get; set; }

    [Name("heading")]
    public double Heading { get; set; }

    [Name("acc[m/s^2]")]
    public double Acceleration { get; set; }
}
