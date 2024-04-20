namespace HiveWays.Business.TableStorageClient;

public class TableStorageConfiguration
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; }
    public int BatchSize { get; set; }
    public int Ttl { get; set; }
}
