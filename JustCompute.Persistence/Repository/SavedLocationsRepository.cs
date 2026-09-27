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
        private readonly DatabasePaths _paths;
        private readonly SemaphoreSlim _initializeGate = new(1, 1);
        private Task? _initialized;

        public SavedLocationsRepository(AppDatabaseConnection connection)
        {
            _database = connection.UserData;
            _paths = connection.Paths;
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
            await MigrateLegacyPlacesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Moves places saved by a version from before the split into the user's own database.
        ///
        /// They are read from the parked copy the installer moved aside when it replaced the
        /// catalogue, or from the catalogue itself if it was never replaced. Ids are carried
        /// across verbatim so the persisted "current location" preference still points at the
        /// right row. Anything that goes wrong leaves the source untouched — the user keeps
        /// their data on the old schema rather than losing it to a half-finished move — and the
        /// next launch tries again.
        /// </summary>
        private async Task MigrateLegacyPlacesAsync()
        {
            string? source = File.Exists(_paths.LegacyUserData) ? _paths.LegacyUserData
                : File.Exists(_paths.Catalogue) ? _paths.Catalogue
                : null;
            if (source is null)
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
                await _database.ExecuteAsync(RepositoryConstants.AttachCatalogueStatement, source).ConfigureAwait(false);
            }
            catch (SQLiteException)
            {
                return;
            }

            bool copied = false;
            try
            {
                var legacyTables = await _database
                    .ExecuteScalarAsync<int>(RepositoryConstants.LegacyTablesPresentQuery)
                    .ConfigureAwait(false);

                if (legacyTables == 2)
                {
                    var columns = await _database
                        .QueryAsync<TableColumnInfo>(RepositoryConstants.LegacyLocationsColumnsQuery)
                        .ConfigureAwait(false);
                    bool hasTimeZoneId = columns.Any(column =>
                        string.Equals(column.Name, "TimeZoneId", StringComparison.OrdinalIgnoreCase));

                    // One transaction, so a failure part-way leaves neither a half-copied list
                    // here nor a source already emptied of it.
                    await _database.RunInTransactionAsync(connection =>
                    {
                        connection.Execute(RepositoryConstants.CopyLegacyCitiesStatement);
                        connection.Execute(RepositoryConstants.CopyLegacyLocationsStatement(hasTimeZoneId));

                        // Only the catalogue keeps living after this; its copies are dropped so a
                        // second run cannot bring the same rows across twice. The parked file is
                        // removed whole, below, once it is detached.
                        if (source == _paths.Catalogue)
                        {
                            connection.Execute(RepositoryConstants.DropLegacyLocationsStatement);
                            connection.Execute(RepositoryConstants.DropLegacyCitiesStatement);
                        }
                    }).ConfigureAwait(false);

                    copied = true;
                }
            }
            catch (SQLiteException)
            {
                // Leave the source as it was; the next launch can try again.
            }
            finally
            {
                try
                {
                    await _database.ExecuteAsync(RepositoryConstants.DetachCatalogueStatement).ConfigureAwait(false);
                }
                catch (SQLiteException)
                {
                    // Detaching a database that never attached is not worth reporting.
                }
            }

            if (copied && source == _paths.LegacyUserData)
            {
                File.Delete(_paths.LegacyUserData);
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
