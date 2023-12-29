using HiveWays.Domain.Documents;

namespace HiveWays.Business.CosmosDbClient;

public interface ICosmosDbClient<T> where T : BaseDevice
{
    Task<T> GetDocumentByIdAsync(string id, string partitionKey);
    Task<IEnumerable<T>> GetDocumentsAsync();
    Task<IEnumerable<T>> GetDocumentsByQueryAsync(Func<T, bool> query);
    Task UpsertDocumentAsync(T entity);
}