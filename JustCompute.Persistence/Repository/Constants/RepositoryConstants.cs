namespace JustCompute.Persistence.Repository.Constants
{
    public class RepositoryConstants
    {
        /// <summary>The catalogue as packaged with the app, by its asset name.</summary>
        public const string PackagedCatalogueAsset = "geo_world.db";

        /// <summary>The shipped, read-only world-city catalogue. Replaceable on any release.</summary>
        public const string CatalogueDatabaseFilename = "geo_world.db";

        /// <summary>
        /// The user's own places, kept in their own file.
        ///
        /// They used to live in the catalogue file alongside the shipped cities, which made the
        /// two impossible to separate: refreshing the catalogue means overwriting that file, and
        /// overwriting it would have taken every saved location with it.
        /// </summary>
        public const string UserDatabaseFilename = "user_locations.db";

        /// <summary>Where a pre-split catalogue holding saved places is parked before a release replaces it.</summary>
        public const string LegacyUserDatabaseFilename = "geo_world.legacy.db";

        public const string LocationsTable = "locations";
        public const string CitiesTable = "cities";

        public const string LocationWithCityQuery =
            $"SELECT l.*, c.Id as CityRowId, c.CityName as CityName, c.CountryName, c.Population " +
            $"FROM {LocationsTable} l JOIN {CitiesTable} c ON l.CityId = c.Id";

        public const string CreateCitiesTableStatement =
            $"CREATE TABLE IF NOT EXISTS {CitiesTable} " +
            $"(Id INTEGER PRIMARY KEY AUTOINCREMENT, CityName VARCHAR(255), CountryName VARCHAR(255), Population INTEGER);";

        public const string CreateLocationsTableStatement =
            $"CREATE TABLE IF NOT EXISTS {LocationsTable} " +
            $"(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name VARCHAR(255), Latitude REAL, Longitude REAL, " +
            $"CityId INTEGER, IsActive INTEGER, IsCurrent INTEGER, TimeZoneOffset INTEGER, " +
            $"TimeZoneId VARCHAR(64), " +
            $"FOREIGN KEY(CityId) REFERENCES cities(Id) ON DELETE CASCADE);";

        /// <summary>
        /// Adds the zone column to databases created before locations stored one. Rows left with
        /// a NULL id fall back to resolving the zone from their coordinates, which is how they
        /// pick up the daylight saving the old whole-hour <c>TimeZoneOffset</c> column never had.
        /// </summary>
        public const string AddTimeZoneIdColumnStatement =
            $"ALTER TABLE {LocationsTable} ADD COLUMN TimeZoneId VARCHAR(64);";

        public const string LocationsTableColumnsQuery =
            $"PRAGMA table_info({LocationsTable});";

        public const SQLite.SQLiteOpenFlags Flags =
            SQLite.SQLiteOpenFlags.ReadWrite |
            SQLite.SQLiteOpenFlags.Create |
            SQLite.SQLiteOpenFlags.SharedCache;

        /// <summary>
        /// Copies any locations still living in the catalogue file into the user file. Runs once:
        /// after it, the catalogue's copies are dropped so the same rows cannot arrive twice.
        /// </summary>
        public const string AttachCatalogueStatement = "ATTACH DATABASE ? AS legacy;";
        public const string DetachCatalogueStatement = "DETACH DATABASE legacy;";

        public const string LegacyTablesPresentQuery =
            "SELECT count(*) FROM legacy.sqlite_master WHERE type='table' AND name IN ('locations','cities');";

        public const string CopyLegacyCitiesStatement =
            $"INSERT INTO {CitiesTable} (Id, CityName, CountryName, Population) " +
            $"SELECT Id, CityName, CountryName, Population FROM legacy.{CitiesTable};";

        /// <summary>
        /// Copies the legacy saved places across.
        ///
        /// A method, not a constant, because the source schema varies. Builds 1 and 2 — the ones
        /// that shipped with places inside the catalogue — never had a TimeZoneId column; it was
        /// added just before the split. The old fixed statement named it regardless, failed with
        /// "no such column" on exactly the databases it existed to rescue, and a catch swallowed
        /// the error. Those rows fall back to resolving their zone from their coordinates.
        /// </summary>
        public static string CopyLegacyLocationsStatement(bool legacyHasTimeZoneId) =>
            $"INSERT INTO {LocationsTable} (Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset, TimeZoneId) " +
            $"SELECT Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset, " +
            (legacyHasTimeZoneId ? "TimeZoneId" : "NULL") +
            $" FROM legacy.{LocationsTable};";

        public const string LegacyLocationsColumnsQuery = $"PRAGMA legacy.table_info({LocationsTable});";

        public const string DropLegacyLocationsStatement = $"DROP TABLE legacy.{LocationsTable};";
        public const string DropLegacyCitiesStatement = $"DROP TABLE legacy.{CitiesTable};";
    }
}