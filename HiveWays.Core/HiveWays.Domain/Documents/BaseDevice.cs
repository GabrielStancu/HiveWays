namespace HiveWays.Domain.Documents;

public class BaseDevice : BaseDocument
{
    public int ExternalId { get; set; }
    public virtual ObjectType ObjectType => ObjectType.Unknown;
}

public class BaseDocument
{
    public string Id { get; set; }
}
