using JustCompute.Persistence.Repository;
using SQLite;

namespace Compute.Core.Tests.Persistence
{
    /// <summary>
    /// The upgrade path that lost every build-2 user's saved places, run against a database laid
    /// out exactly as build 2 left it. Two bugs stood in the way, and each would have been enough
    /// on its own: the catalogue was truncated before the migration read it, and the migration
    /// named a TimeZoneId column that builds 1 and 2 never had.
    /// </summary>
    public sealed class LegacyPlacesMigrationTests : IDisposable
    {
        private readonly string _directory = Directory.CreateTempSubdirectory("coordi-migration-").FullName;
        private readonly DatabasePaths _paths;
        private readonly string _shipped;

        public LegacyPlacesMigrationTests()
        {
            _paths = DatabasePaths.InDirectory(_directory);
            _shipped = Path.Combine(_directory, "shipped.db");
            CreateCatalogue(_shipped, withPlaces: false);
        }

        public void Dispose()
        {
            SQLiteAsyncConnection.ResetPool();
            try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
        }

        [Fact]
        public async Task ABuild2UserKeepsTheirPlacesThroughAnUpgrade()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: true, withTimeZoneId: false);

            var result = CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true);

            Assert.Equal(CatalogueInstallResult.InstalledAfterParkingUserData, result);
            Assert.False(CatalogueInstaller.HoldsSavedPlaces(_paths.Catalogue));

            var places = await new SavedLocationsRepository(new AppDatabaseConnection(_paths)).GetAllAsync();

            Assert.Equal(["Home", "Observatory"], places.OrderBy(p => p.Id).Select(p => p.Name));
            // Ids carried across verbatim, so the remembered selection still points at the right row.
            Assert.Equal([3, 8], places.OrderBy(p => p.Id).Select(p => p.Id));
            Assert.Equal("Kraków", places.Single(p => p.Id == 8).City.CityName);
            Assert.False(File.Exists(_paths.LegacyUserData), "the parked copy is removed once migrated");
        }

        [Fact]
        public async Task APlaceSavedWithATimeZoneIdKeepsItsZone()
        {
            // The layout from just before the split, which did have the column.
            CreateCatalogue(_paths.Catalogue, withPlaces: true, withTimeZoneId: true);

            CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true);
            var places = await new SavedLocationsRepository(new AppDatabaseConnection(_paths)).GetAllAsync();

            Assert.Equal("Europe/Warsaw", places.Single(p => p.Id == 8).TimeZoneId);
        }

        [Fact]
        public async Task PlacesStillInAnUnreplacedCatalogueAreMovedAndDroppedThere()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: true, withTimeZoneId: false);

            var places = await new SavedLocationsRepository(new AppDatabaseConnection(_paths)).GetAllAsync();

            Assert.Equal(2, places.Count);
            SQLiteAsyncConnection.ResetPool();
            Assert.False(CatalogueInstaller.HoldsSavedPlaces(_paths.Catalogue), "copies are dropped so a rerun cannot duplicate them");
        }

        [Fact]
        public void AFreshInstallWritesTheCatalogue()
        {
            Assert.Equal(CatalogueInstallResult.Installed, CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true));
            Assert.True(File.Exists(_paths.Catalogue));
            Assert.False(File.Exists(_paths.Catalogue + ".installing"));
        }

        [Fact]
        public void ACurrentCatalogueIsLeftAlone()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: true);
            var before = File.ReadAllBytes(_paths.Catalogue);

            Assert.Equal(CatalogueInstallResult.NotNeeded, CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: false));
            Assert.Equal(before, File.ReadAllBytes(_paths.Catalogue));
        }

        [Fact]
        public void ACatalogueWithNoPlacesIsReplacedWithoutParking()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: false);

            Assert.Equal(CatalogueInstallResult.Installed, CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true));
            Assert.False(File.Exists(_paths.LegacyUserData));
        }

        [Fact]
        public void AnEmptyPlacesTableIsNotWorthParking()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: false, withEmptyPlacesTable: true);

            Assert.Equal(CatalogueInstallResult.Installed, CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true));
            Assert.False(File.Exists(_paths.LegacyUserData));
        }

        [Fact]
        public void WhenThePlaceToParkIsTakenTheOldCatalogueStays()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: true);
            File.WriteAllText(_paths.LegacyUserData, "an earlier parked copy, not yet migrated");
            var before = File.ReadAllBytes(_paths.Catalogue);

            Assert.Equal(CatalogueInstallResult.KeptExistingToProtectUserData,
                CatalogueInstaller.Install(_paths, OpenShipped, isNewVersion: true));
            Assert.Equal(before, File.ReadAllBytes(_paths.Catalogue));
        }

        [Fact]
        public void WithNothingToInstallTheMovedFileIsPutBack()
        {
            CreateCatalogue(_paths.Catalogue, withPlaces: true);

            Assert.Equal(CatalogueInstallResult.NoShippedCatalogue,
                CatalogueInstaller.Install(_paths, () => null, isNewVersion: true));
            Assert.True(CatalogueInstaller.HoldsSavedPlaces(_paths.Catalogue));
            Assert.False(File.Exists(_paths.LegacyUserData));
        }

        private Stream? OpenShipped() => File.OpenRead(_shipped);

        /// <summary>
        /// A catalogue as a given build left it. The places tables use build 2's own statements,
        /// with TimeZoneId only when asked for, because its absence is what broke the old copy.
        /// </summary>
        private static void CreateCatalogue(
            string path, bool withPlaces, bool withTimeZoneId = false, bool withEmptyPlacesTable = false)
        {
            using var db = new SQLiteConnection(path);
            db.Execute("CREATE TABLE worldcities (Id INTEGER PRIMARY KEY, CityName TEXT, Lat REAL, Lng REAL);");
            db.Execute("INSERT INTO worldcities (CityName, Lat, Lng) VALUES ('Paris', 48.85, 2.35);");

            if (!withPlaces && !withEmptyPlacesTable) return;

            db.Execute("CREATE TABLE IF NOT EXISTS cities (Id INTEGER PRIMARY KEY AUTOINCREMENT, CityName VARCHAR(255), CountryName VARCHAR(255), Population INTEGER);");
            db.Execute("CREATE TABLE IF NOT EXISTS locations (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name VARCHAR(255), Latitude REAL, Longitude REAL, " +
                       "CityId INTEGER, IsActive INTEGER, IsCurrent INTEGER, TimeZoneOffset INTEGER, " +
                       (withTimeZoneId ? "TimeZoneId VARCHAR(64), " : "") +
                       "FOREIGN KEY(CityId) REFERENCES cities(Id) ON DELETE CASCADE);");

            if (!withPlaces) return;

            db.Execute("INSERT INTO cities (Id, CityName, CountryName, Population) VALUES (4, 'Paris', 'France', 2100000), (5, 'Kraków', 'Poland', 800000);");
            if (withTimeZoneId)
            {
                db.Execute("INSERT INTO locations (Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset, TimeZoneId) VALUES " +
                           "(3, 'Home', 48.85, 2.35, 4, 0, 0, 1, 'Europe/Paris'), (8, 'Observatory', 50.05, 19.94, 5, 0, 0, 1, 'Europe/Warsaw');");
            }
            else
            {
                db.Execute("INSERT INTO locations (Id, Name, Latitude, Longitude, CityId, IsActive, IsCurrent, TimeZoneOffset) VALUES " +
                           "(3, 'Home', 48.85, 2.35, 4, 0, 0, 1), (8, 'Observatory', 50.05, 19.94, 5, 0, 0, 1);");
            }
        }
    }
}
