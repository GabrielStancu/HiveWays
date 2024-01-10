namespace HiveWays.Infrastructure.Utils;

public class ContinuationList<T> : List<T>
{
    public string ContinuationToken { get; set; }
}
