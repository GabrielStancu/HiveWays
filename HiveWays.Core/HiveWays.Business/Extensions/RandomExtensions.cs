﻿namespace HiveWays.Business.Extensions;

public static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean, double stdDev)
    {
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)

        return mean + stdDev * randStdNormal; // random normal(mean, stdDev^2)
    }

    public static int NextGaussian(this Random random, int mean, int stdDev)
    {
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)

        return (int)(mean + stdDev * randStdNormal); // random normal(mean, stdDev^2)
    }
}
