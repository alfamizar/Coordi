using Compute.Core.Domain.Entities.Models;
using Compute.Core.Repository;
using JustCompute.Persistence.Mapping;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Persistence.Repository.Models;
using JustCompute.Persistence.Repository.Models.DTOs;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    public class SavedLocationsRepository : ISavedLocationsRepository
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly SemaphoreSlim _initializeGate = new(1, 1);
        private Task? _initialized;

        public SavedLocationsRepository(AppDatabaseConnection connection)
        {
            _database = connection.UserData;
        }

        /// <summary>
        /// Runs the one-time setup, and only remembers it once it has actually succeeded.
        ///
        /// This used to be a <see cref="Lazy{T}"/> of the task, which caches the failure too: a
        /// single transient SQLite lock at startup left every later call awaiting the same
        /// faulted task, so the app kept throwing until the process was restarted. Retrying on
        /// the next call costs nothing when setup works, and recovers when it does not.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_initialized is { IsCompletedSuccessfully: true })
            {
                return;
            }

            await _initializeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized is { IsCompletedSuccessfully: true })
                {
                    return;
                }

                Task attempt = InitializeDatabaseAsync();
                await attempt.ConfigureAwait(false);
                _initialized = attempt;
            }
            finally
            {
                _initializeGate.Release();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            await _database.ExecuteAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateCitiesTableStatement).ConfigureAwait(false);
            await _database.ExecuteAsync(RepositoryConstants.CreateLocationsTableStatement).ConfigureAwait(false);
            await AddTimeZoneIdColumnIfMissingAsync().ConfigureAwait(false);
            await MigrateFromCatalogueAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Moves locations saved by an earlier version, which kept them inside the shipped
        /// catalogue file, into the user's own database.
        ///
        /// Ids are carried across verbatim so the persisted "current location" preference still
        /// points at the right row, and the originals are dropped once copied so a second run
        /// cannot duplicate them. Anything that goes wrong leaves the old file untouched — the
        /// user keeps their data on the old schema rather than losing it to a half-finished move.
        /// </summary>
        private async Task MigrateFromCatalogueAsync()
        {
            if (!File.Exists(RepositoryConstants.CataloguePath))
            {
                return;
            }

            // Nothing to carry across once this database already holds places.
            var existing = await _database
                .ExecuteScalarAsync<int>($"SELECT count(*) FROM {RepositoryConstants.LocationsTable};")
                .ConfigureAwait(false);
            if (existing > 0)
            {
                return;
            }

            try
            {
                await _database.ExecuteAsync(RepositoryConstants.AttachCatalogueStatement,
                    RepositoryConstants.CataloguePath).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var legacyTables = await _database
                    .ExecuteScalarAsync<int>(RepositoryConstants.LegacyTablesPresentQuery)
                    .ConfigureAwait(false);

                if (legacyTables == 2)
                {
                    await _database.ExecuteAsync(RepositoryConstants.CopyLegacyCitiesStatement).ConfigureAwait(false);
                    await _database.ExecuteAsync(RepositoryConstants.CopyLegacyLocationsStatement).ConfigureAwait(false);
                    await _database.ExecuteAsync(RepositoryConstants.DropLegacyLocationsStatement).ConfigureAwait(false);
                    await _database.ExecuteAsync(RepositoryConstants.DropLegacyCitiesStatement).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // Leave the old file as it was; the next launch can try again.
            }
            finally
            {
                try
                {
                    await _database.ExecuteAsync(RepositoryConstants.DetachCatalogueStatement).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Detaching a database that never attached is not worth reporting.
                }
            }
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

        public async Task<List<Location>> GetAllAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var rows = await _database
                .QueryAsync<LocationWithCityDTO>(RepositoryConstants.LocationWithCityQuery)
                .ConfigureAwait(false);

            return rows.ToDomain();
        }

        /// <summary>
        /// Writes the city first, then the location pointing at it.
        ///
        /// The order matters and the two ids are not interchangeable: the tables autoincrement
        /// independently, so the location's own id says nothing about which city row belongs to
        /// it. Both generated ids go back onto the domain object, because an update or a delete
        /// later has to address the right rows.
        /// </summary>
        public async Task AddAsync(Location location)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            CityTable city = location.ToCityTable();
            await _database.InsertAsync(city).ConfigureAwait(false);

            LocationTable row = location.ToLocationTable();
            row.CityId = city.Id;
            await _database.InsertAsync(row).ConfigureAwait(false);

            location.Id = row.Id;
            location.City.Id = city.Id;
        }

        public async Task UpdateAsync(Location location)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            CityTable city = location.ToCityTable();
            RequireCityRow(city, location);

            await _database.UpdateAsync(city).ConfigureAwait(false);

            LocationTable row = location.ToLocationTable();
            row.CityId = city.Id;
            await _database.UpdateAsync(row).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Location location)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            CityTable city = location.ToCityTable();
            RequireCityRow(city, location);

            // The location row goes with it: the foreign key is declared ON DELETE CASCADE.
            await _database.DeleteAsync(city).ConfigureAwait(false);
        }

        /// <summary>
        /// A city id of zero means the row was never persisted. Updating or deleting on it would
        /// match nothing — or, worse, whatever row happens to hold that id.
        /// </summary>
        private static void RequireCityRow(CityTable city, Location location)
        {
            if (city.Id <= 0)
            {
                throw new InvalidOperationException(
                    $"Location '{location.Name}' has no city row to address.");
            }
        }
    }
}
