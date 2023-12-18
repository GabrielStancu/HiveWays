using CsvHelper.Configuration.Attributes;

namespace HiveWays.VehicleEdge.Models;

public class DataPoint
{
    [Name("#time")]
    public int TimeOffsetSeconds { get; set; }

    [Name("id")]
    public int Id { get; set; }

    [Name("x[m]")]
    public decimal X { get; set; }

    [Name("y[m]")]
    public decimal Y { get; set; }

    [Name("speed[m/s]")]
    public decimal Speed { get; set; }

    [Name("heading")]
    public decimal Heading { get; set; }

    [Name("acc[m/s^2]")]
    public decimal Acceleration { get; set; }
}
