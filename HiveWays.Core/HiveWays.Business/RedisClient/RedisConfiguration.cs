﻿namespace HiveWays.Business.RedisClient;

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
    public int ExpirationTime { get; set; }
}
