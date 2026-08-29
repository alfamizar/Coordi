using Compute.Core.Repository;
using JustCompute.Persistence.Repository.Constants;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    public class SavedLocationsRepository : ILocationsRepository
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly Lazy<Task> _initializeDatabase;

        public SavedLocationsRepository(AppDatabaseConnection connection)
        {
            _database = connection.Database;
            _initializeDatabase = new Lazy<Task>(InitializeDatabaseAsync);
        }

        private async Task InitializeDatabaseAsync()
        {
            await _database.ExecuteAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateCitiesTableStatement).ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateLocationsTableStatement).ConfigureAwait(false);
            await AddTimeZoneIdColumnIfMissingAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// CREATE TABLE IF NOT EXISTS leaves an already-created table alone, so a column added
        /// after the fact has to be applied on its own.
        /// </summary>
        private async Task AddTimeZoneIdColumnIfMissingAsync()
        {
            var columns = await _database
                .QueryAsync<TableColumnInfo>(RepositoryConstants.LocationsTableColumnsQuery)
                .ConfigureAwait(false);

            if (columns.Any(column => string.Equals(column.Name, "TimeZoneId", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await _database.ExecuteAsync(RepositoryConstants.AddTimeZoneIdColumnStatement).ConfigureAwait(false);
        }

        /// <summary>A row of <c>PRAGMA table_info</c>; only the column name is of interest.</summary>
        private sealed class TableColumnInfo
        {
            public string Name { get; set; } = string.Empty;
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
