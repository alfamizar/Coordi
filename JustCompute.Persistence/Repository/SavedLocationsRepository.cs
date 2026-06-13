using Compute.Core.Repository;
using JustCompute.Persistence.Repository.Constants;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    public class SavedLocationsRepository : ILocationsRepository
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly Lazy<Task> _initializeDatabase;

        public SavedLocationsRepository()
        {
            _database = new SQLiteAsyncConnection(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
            _initializeDatabase = new Lazy<Task>(InitializeDatabaseAsync);
        }

        private async Task InitializeDatabaseAsync()
        {
            await _database.ExecuteAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateCitiesTableStatement).ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateLocationsTableStatement).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>() where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.Table<T>().ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> GetItemsWithQueryAsync<T>(string query) where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.QueryAsync<T>(query).ConfigureAwait(false);
        }

        public async Task<int> SaveItemAsync<T>(T item) where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.InsertAsync(item).ConfigureAwait(false);
        }

        public async Task<int> UpdateItemAsync<T>(T item) where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.UpdateAsync(item).ConfigureAwait(false);
        }

        public async Task<int> DeleteItemAsync<T>(T item) where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.DeleteAsync(item).ConfigureAwait(false);
        }

        public async Task<int> ExecuteAsync(string query)
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.ExecuteAsync(query).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query) where T : class, new()
        {
            await _initializeDatabase.Value.ConfigureAwait(false);
            return await _database.QueryAsync<T>(query).ConfigureAwait(false);
        }
    }
}
