using HiveWays.Business.Extensions;
using HiveWays.Domain.Documents;

namespace HiveWays.RegisteredDevicesGenerator;

public static class ItemsDataGenerator
{
    public static List<BaseDevice> Generate()
    {
        List<BaseDevice> objects = new List<BaseDevice>();

        var carBrands = GenerateCarBrands();
        var carModels = GenerateCarModels();

        var random = new Random();

        for (int i = 1; i <= 50000; i++)
        {
            var objectType = GetObjectType(i);
            BaseDevice obj;

            switch (objectType)
            {
                case ObjectType.Car:
                    var brand = carBrands[random.Next(carBrands.Length)];
                    obj = new CarDevice
                    {
                        Brand = brand,
                        Model = carModels[brand][random.Next(carModels[brand].Length)],
                        FabricationYear = Math.Min(random.NextGaussian(2011, 4), DateTime.UtcNow.Year),
                        Range = Math.Max(0, random.NextGaussian(175000, 30000)),
                        FuelType = GetFuelType(i)
                    };
                    break;
                case ObjectType.TrafficLight:
                    obj = new TrafficLightDevice();
                    break;
                default:
                    obj = new ObstacleDevice();
                    break;
            }

            obj.Id = Guid.NewGuid().ToString();
            obj.ExternalId = i;

            objects.Add(obj);
        }

        return objects;
    }

    static ObjectType GetObjectType(int carExternalId)
    {
        return carExternalId switch
        {
            >= 50 and <= 99 => ObjectType.Obstacle,
            >= 100 and <= 149 => ObjectType.TrafficLight,
            _ => ObjectType.Car
        };
    }

    static FuelType GetFuelType(int carExternalId)
    {
        return GetObjectType(carExternalId) == ObjectType.Car ? (FuelType)(carExternalId % 6) : FuelType.NoFuel;
    }

    static string[] GenerateCarBrands()
    {
        return new[]
        {
            "Toyota", "Ford", "Honda", "Chevrolet", "Volkswagen",
            "BMW", "Mercedes-Benz", "Audi", "Nissan", "Hyundai",
            "Subaru", "Kia", "Mazda", "Lexus", "Jeep",
            "Volvo", "Tesla", "Porsche", "Jaguar", "Land Rover",
            "Opel", "Peugeot", "Mitsubishi", "Renault"
        };
    }

    static Dictionary<string, string[]> GenerateCarModels()
    {
        return new Dictionary<string, string[]>
        {
            { "Toyota", new[] { "Camry", "Corolla", "Rav4", "Highlander", "Tacoma" } },
            { "Ford", new[] { "F-150", "Escape", "Mustang", "Focus", "Puma" } },
            { "Honda", new[] { "Civic", "Accord", "CR-V", "Pilot", "Odyssey" } },
            { "Chevrolet", new[] { "Silverado", "Equinox", "Malibu", "Traverse", "Camaro" } },
            { "Volkswagen", new[] { "Golf", "Passat", "Tiguan", "Atlas", "Jetta" } },
            { "BMW", new[] { "3 Series", "5 Series", "X5", "7 Series", "X3" } },
            { "Mercedes-Benz", new[] { "E-Class", "C-Class", "S-Class", "GLC", "GLE" } },
            { "Audi", new[] { "A4", "A6", "Q5", "Q7", "TT" } },
            { "Nissan", new[] { "Altima", "Rogue", "Sentra", "Titan", "Pathfinder" } },
            { "Hyundai", new[] { "Sonata", "Tucson", "Santa Fe", "Kona", "Palisade" } },
            { "Subaru", new[] { "Outback", "Forester", "Impreza", "Legacy", "Crosstrek" } },
            { "Kia", new[] { "Sorento", "Sportage", "Telluride", "Forte", "Soul" } },
            { "Mazda", new[] { "CX-5", "Mazda3", "Mazda6", "MX-5", "CX-9" } },
            { "Lexus", new[] { "RX", "ES", "GX", "LX", "UX" } },
            { "Jeep", new[] { "Grand Cherokee", "Wrangler", "Cherokee", "Renegade", "Compass" } },
            { "Volvo", new[] { "XC90", "S60", "XC40", "V60", "XC60" } },
            { "Tesla", new[] { "Model 3", "Model S", "Model X", "Model Y" } },
            { "Porsche", new[] { "911", "Cayenne", "Panamera", "Macan", "Taycan" } },
            { "Jaguar", new[] { "F-PACE", "XE", "XF", "I-PACE", "E-PACE" } },
            { "Land Rover", new[] { "Range Rover", "Discovery", "Defender", "Range Rover Sport", "Discovery Sport" } },
            { "Opel", new[] { "Astra", "Corsa", "Insignia", "Crossland X", "Grandland X" } },
            { "Peugeot", new[] { "208", "308", "3008", "5008", "Partner" } },
            { "Mitsubishi", new[] { "Outlander", "Eclipse Cross", "ASX", "Pajero", "L200" } },
            { "Renault", new[] { "Clio", "Megane", "Captur", "Kadjar", "Talisman" } }
        };
    }
}
