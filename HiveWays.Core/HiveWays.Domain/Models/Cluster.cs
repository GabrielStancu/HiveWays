namespace HiveWays.Domain.Models;

public class Cluster : IIdentifiable
{
    public int Id { get; set; }
    public List<int> Vehicles { get; } = new();
    public GeoPoint Center { get; private set; }

    public void AddVehicle(Vehicle vehicle)
    {
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
            var newLatitude = (Center.Latitude * Vehicles.Count + medianLocation.Latitude) / (Vehicles.Count + 1);
            var newLongitude = (Center.Longitude * Vehicles.Count + medianLocation.Longitude) / (Vehicles.Count + 1);

            Center.Latitude = newLatitude;
            Center.Longitude = newLongitude;
        }

        Vehicles.Add(vehicle.Id);
    }
}
