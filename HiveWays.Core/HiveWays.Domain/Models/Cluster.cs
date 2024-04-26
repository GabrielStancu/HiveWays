namespace HiveWays.Domain.Models;

public class Cluster : IIdentifiable
{
    public int Id { get; set; }
    public GeoPoint Center { get; private set; }
    public double AverageSpeed { get; private set; }
    public double AverageAcceleration { get; private set; }
    public double AverageOrientation { get; private set; }
    public int VehiclesCount => Vehicles.Count;
    public List<Vehicle> Vehicles { get; } = new();

    public void AddVehicle(Vehicle vehicle)
    {
        Vehicles.Add(vehicle);

        var medianLocation = vehicle.MedianLocation.Location;
        
        if (Center is null)
        {
            Center = new GeoPoint
            {
                Latitude = medianLocation.Latitude,
                Longitude = medianLocation.Longitude
            };
            AverageSpeed = vehicle.MedianLocation.SpeedKmph;
            AverageAcceleration = vehicle.MedianLocation.AccelerationKmph;
            AverageOrientation = vehicle.MedianLocation.Heading;
        }
        else
        {
            Center.Latitude = RecomputeAverageAfterAdd(Center.Latitude, medianLocation.Latitude);
            Center.Longitude = RecomputeAverageAfterAdd(Center.Longitude, medianLocation.Longitude);
            AverageSpeed = RecomputeAverageAfterAdd(AverageSpeed, vehicle.MedianLocation.SpeedKmph);
            AverageAcceleration = RecomputeAverageAfterAdd(AverageAcceleration, vehicle.MedianLocation.AccelerationKmph);
            AverageOrientation = RecomputeAverageAfterAdd(AverageOrientation, vehicle.MedianLocation.Heading);
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

        var medianLocation = vehicle.MedianLocation.Location;
        Center.Latitude = RecomputeAverageAfterRemove(Center.Latitude, medianLocation.Latitude);
        Center.Longitude = RecomputeAverageAfterRemove(Center.Longitude, medianLocation.Longitude);
        AverageSpeed = RecomputeAverageAfterRemove(AverageSpeed, vehicle.MedianLocation.SpeedKmph);
        AverageAcceleration = RecomputeAverageAfterRemove(AverageAcceleration, vehicle.MedianLocation.AccelerationKmph);
        AverageOrientation = RecomputeAverageAfterRemove(AverageOrientation, vehicle.MedianLocation.Heading);
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
