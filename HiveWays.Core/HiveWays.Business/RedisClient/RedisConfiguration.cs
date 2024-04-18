namespace HiveWays.Business.RedisClient;

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
    public int ListLength { get; set; }
    public int Ttl { get; set; }
}
