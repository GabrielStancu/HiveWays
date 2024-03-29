namespace HiveWays.Domain.Documents;

public class CarDevice : BaseDevice
{
    public string Brand { get; set; }
    public string Model { get; set; }
    public int FabricationYear { get; set; }
    public FuelType FuelType { get; set; }
    public double Range { get; set; }
    public override ObjectType ObjectType => ObjectType.Car;
}
