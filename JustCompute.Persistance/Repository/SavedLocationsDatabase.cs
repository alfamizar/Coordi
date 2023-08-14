using Compute.Core.Helpers;
using Compute.Core.Repository;
using JustCompute.Persistance.Repository.Constants;
using SQLite;

namespace JustCompute.Persistance.Repository
{
    public class SavedLocationsDatabase : ISavedLocationsDatabase
    {
        static SQLiteAsyncConnection Database;

        public static readonly AsyncLazy<SavedLocationsDatabase> Instance = new(async () =>
        {
            var instance = new SavedLocationsDatabase();

            await Database.ExecuteAsync(RepositoryConstants.CreateCitiesTableStatement);
            await Database.ExecuteAsync(RepositoryConstants.CreateLocationsTableStatement);

            return instance;
        });

        public SavedLocationsDatabase()
        {
            Database = new SQLiteAsyncConnection(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>() where T : class, new()
        {
            return await Database.Table<T>().ToListAsync();
        }

        public async Task<IEnumerable<T>> GetItemsWithQueryAsync<T>(string query) where T : class, new()
        {
            return await Database.QueryAsync<T>(query);
        }

        public Task<int> SaveItemAsync<T>(T item) where T : class, new()
        {
            return Database.InsertAsync(item);
        }

        public Task<int> DeleteItemAsync<T>(T item) where T : class, new()
        {
            return Database.DeleteAsync(item);
        }

        public async Task<int> ExecuteAsync(string query)
        {
            return await Database.ExecuteAsync(query);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query) where T : class, new()
        {
            return await Database.QueryAsync<T>(query);
        }
    }
}
