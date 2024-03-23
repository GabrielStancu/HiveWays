namespace HiveWays.Domain.Models;

public class Cluster : IIdentifiable
{
    public int Id { get; set; }
    public List<int> Vehicles { get; } = new();
    public GeoPoint Center { get; private set; }

    public void AddVehicle(Vehicle vehicle)
    {
        var medianInfo = vehicle.Info[vehicle.Info.Count / 2];

        if (Center is null)
        {
            Center = new GeoPoint
            {
                Latitude = medianInfo.Latitude,
                Longitude = medianInfo.Longitude
            };
        }
        else
        {
            var newLatitude = (Center.Latitude * Vehicles.Count + medianInfo.Latitude) / (Vehicles.Count + 1);
            var newLongitude = (Center.Longitude * Vehicles.Count + medianInfo.Longitude) / (Vehicles.Count + 1);

            Center.Latitude = newLatitude;
            Center.Longitude = newLongitude;
        }

        Vehicles.Add(vehicle.Id);
    }
}
