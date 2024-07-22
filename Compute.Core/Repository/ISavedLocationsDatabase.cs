namespace Compute.Core.Repository
{
    public interface ISavedLocationsDatabase
    {
        Task<IEnumerable<T>> GetItemsAsync<T>() where T : class, new();

        Task<IEnumerable<T>> GetItemsWithQueryAsync<T>(string query) where T : class, new();

        Task<int> SaveItemAsync<T>(T item) where T : class, new();

        Task<int> UpdateItemAsync<T>(T item) where T : class, new();

        Task<int> DeleteItemAsync<T>(T item) where T : class, new();

        Task<int> ExecuteAsync(string query);

        Task<IEnumerable<T>> QueryAsync<T>(string query) where T : class, new();
    }
}