namespace HiveWays.Domain.Documents;

public class BaseDevice
{
    public string Id { get; set; }
    public int ExternalId { get; set; }
    public virtual ObjectType ObjectType => ObjectType.Unknown;
}
