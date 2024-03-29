namespace HiveWays.TelemetryIngestion.Configuration;

public class IngestionConfiguration
{
    public int MinId { get; set; }
    public int MaxId { get; set; }
    public int MinSpeed { get; set; }
    public int MaxSpeed { get; set; }
    public double ReferenceLatitude { get; set; }
    public double ReferenceLongitude { get; set; }
}
