namespace JustCompute.Persistence.Repository.Constants
{
    public class RepositoryConstants
    {
        public const string PreinstalledDatabasePath = "JustCompute.Database.geo_world.db";

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

        private static string BasePath =>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>The shipped catalogue, safe to replace wholesale.</summary>
        public static string CataloguePath => Path.Combine(BasePath, CatalogueDatabaseFilename);

        /// <summary>The user's saved places, never overwritten by an install.</summary>
        public static string UserDatabasePath => Path.Combine(BasePath, UserDatabaseFilename);

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

        public const string CopyLegacyLocationsStatement =
            $"INSERT INTO {LocationsTable} (Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset, TimeZoneId) " +
            $"SELECT Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset, TimeZoneId FROM legacy.{LocationsTable};";

        public const string DropLegacyLocationsStatement = $"DROP TABLE legacy.{LocationsTable};";
        public const string DropLegacyCitiesStatement = $"DROP TABLE legacy.{CitiesTable};";
    }
}