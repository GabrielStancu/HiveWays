﻿using HiveWays.Domain.Models;
using HiveWays.Business.Extensions;

namespace HiveWays.Business.ObjectDataGenerator;

public static class ObjectsDataGenerator
{
    public static List<RegisteredObject> Generate()
    {
        List<RegisteredObject> objects = new List<RegisteredObject>();

        var carBrands = GenerateCarBrands();
        var carModels = GenerateCarModels();

        var random = new Random();

        for (int i = 1; i <= 3000; i++)
        {
            RegisteredObject obj = new RegisteredObject
            {
                Id = Guid.NewGuid(),
                CarExternalId = i,
                ObjectType = GetObjectType(i),
                FuelType = GetFuelType(i)
            };

            if (obj.ObjectType == ObjectType.Car)
            {
                obj.Brand = carBrands[random.Next(carBrands.Length)];
                obj.Model = carModels[obj.Brand][random.Next(carModels[obj.Brand].Length)];

                // Generate realistic fabrication year and range
                obj.FabricationYear = random.NextGaussian(2011, 4);
                obj.Range = Math.Max(0, (decimal)random.NextGaussian(175000, 30000));
            }

            objects.Add(obj);
        }

        return objects;
    }

    static ObjectType GetObjectType(int carExternalId)
    {
        if (carExternalId is >= 50 and <= 99)
        {
            return ObjectType.Obstacle;
        }
        else if (carExternalId is >= 100 and <= 149)
        {
            return ObjectType.TrafficLight;
        }
        else
        {
            return ObjectType.Car;
        }
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
