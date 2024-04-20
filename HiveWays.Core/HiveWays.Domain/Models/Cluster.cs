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

        var medianLocation = vehicle.MedianInfo.Location;
        
        if (Center is null)
        {
            Center = new GeoPoint
            {
                Latitude = medianLocation.Latitude,
                Longitude = medianLocation.Longitude
            };
            AverageSpeed = vehicle.MedianInfo.SpeedKmph;
            AverageAcceleration = vehicle.MedianInfo.AccelerationKmph;
            AverageOrientation = vehicle.MedianInfo.Heading;
        }
        else
        {
            Center.Latitude = RecomputeAverageAfterAdd(Center.Latitude, medianLocation.Latitude);
            Center.Longitude = RecomputeAverageAfterAdd(Center.Longitude, medianLocation.Longitude);
            AverageSpeed = RecomputeAverageAfterAdd(AverageSpeed, vehicle.MedianInfo.SpeedKmph);
            AverageAcceleration = RecomputeAverageAfterAdd(AverageAcceleration, vehicle.MedianInfo.AccelerationKmph);
            AverageOrientation = RecomputeAverageAfterAdd(AverageOrientation, vehicle.MedianInfo.Heading);
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
        AverageSpeed = RecomputeAverageAfterRemove(AverageSpeed, vehicle.MedianInfo.SpeedKmph);
        AverageAcceleration = RecomputeAverageAfterRemove(AverageAcceleration, vehicle.MedianInfo.AccelerationKmph);
        AverageOrientation = RecomputeAverageAfterRemove(AverageOrientation, vehicle.MedianInfo.Heading);
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
