﻿namespace HiveWays.Business.RedisClient;

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
    public string Key { get; set; }
    public int ListLength { get; set; }
}