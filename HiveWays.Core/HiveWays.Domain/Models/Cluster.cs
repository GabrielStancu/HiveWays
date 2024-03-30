namespace HiveWays.Domain.Models;

public class Cluster : IIdentifiable
{
    public int Id { get; set; }
    public List<Vehicle> Vehicles { get; } = new();
    public GeoPoint Center { get; private set; }

    public void AddVehicle(Vehicle vehicle)
    {
        Vehicles.Add(vehicle);

        var medianLocation = vehicle.MedianInfo.Location;
        
        if (Center is null)
        {
            Center = new GeoPoint
            {
                Latitude = medianLocation.Latitude,
                Longitude = medianLocation.Longitude
            };
        }
        else
        {
            Center.Latitude = RecomputeAverageAfterAdd(Center.Latitude, medianLocation.Latitude);
            Center.Longitude = RecomputeAverageAfterAdd(Center.Longitude, medianLocation.Longitude);
        }
    }

    public void RemoveVehicle(int vehicleId)
    {
        var vehicle = Vehicles.FirstOrDefault(v => v.Id == vehicleId);
        if (vehicle is null)
            return;

        Vehicles.Remove(vehicle);
        if (!Vehicles.Any())
        {
            Center = null;
            return;
        }

        var medianLocation = vehicle.MedianInfo.Location;
        Center.Latitude = RecomputeAverageAfterRemove(Center.Latitude, medianLocation.Latitude);
        Center.Longitude = RecomputeAverageAfterRemove(Center.Longitude, medianLocation.Longitude);
    }

    private double RecomputeAverageAfterAdd(double oldAverage, double addedValue)
    {
        return (oldAverage * (Vehicles.Count - 1) + addedValue) / Vehicles.Count;
    }

    private double RecomputeAverageAfterRemove(double oldAverage, double removedValue)
    {
        return (oldAverage * (Vehicles.Count + 1) - removedValue) / Vehicles.Count;
    }
}
