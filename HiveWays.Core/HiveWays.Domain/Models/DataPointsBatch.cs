namespace HiveWays.Domain.Models;

public class DataPointsBatch
{
    public string BatchDescriptor { get; set; }
    public IEnumerable<DataPoint> DataPoints { get; set; }
}
