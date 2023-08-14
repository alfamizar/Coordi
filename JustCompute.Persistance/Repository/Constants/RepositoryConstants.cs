namespace JustCompute.Persistance.Repository.Constants
{
    public class RepositoryConstants
    {
        public const string PreinstalledDatabasePath = "JustCompute.Database.geo_world.db";
        public const string DatabaseFilename = "geo_world.db";

        public const string LocationsTable = "locations";
        public const string CitiesTable = "cities";

        public const string LocationWithCityQuery =
            $"SELECT l.*, c.CityName as CityName, c.CountryName, c.Population " +
            $"FROM {LocationsTable} l JOIN {CitiesTable} c ON l.CityId = c.Id";

        public const string CreateCitiesTableStatement =
            $"CREATE TABLE IF NOT EXISTS {CitiesTable} " +
            $"(Id INTEGER PRIMARY KEY AUTOINCREMENT, CityName VARCHAR(255), CountryName VARCHAR(255), Population INTEGER);";

        public const string CreateLocationsTableStatement =
            $"CREATE TABLE IF NOT EXISTS {LocationsTable} " +
            $"(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name VARCHAR(255), Latitude REAL, Longitude REAL, " +
            $"CityId INTEGER, IsActive INTEGER, IsCurrent INTEGER, TimeZoneOffset INTEGER, " +
            $"FOREIGN KEY(CityId) REFERENCES cities(Id) ON DELETE CASCADE);";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
    }
}