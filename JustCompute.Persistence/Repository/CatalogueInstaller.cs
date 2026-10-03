using SQLite;

namespace JustCompute.Persistence.Repository
{
    /// <summary>What <see cref="CatalogueInstaller.Install"/> did.</summary>
    public enum CatalogueInstallResult
    {
        /// <summary>The installed catalogue is current; nothing was touched.</summary>
        NotNeeded,

        /// <summary>The shipped catalogue was written.</summary>
        Installed,

        /// <summary>The old file held saved places, so it was parked first and then replaced.</summary>
        InstalledAfterParkingUserData,

        /// <summary>
        /// The old file held saved places and a parked copy already existed, so the old file was
        /// left in place. An out-of-date city list is recoverable; a user's places are not.
        /// </summary>
        KeptExistingToProtectUserData,

        /// <summary>The app carries no catalogue to install.</summary>
        NoShippedCatalogue,
    }

    /// <summary>
    /// Puts the shipped world-city catalogue in place, without ever destroying a user's places.
    ///
    /// Until build 3 the catalogue file also held the places a user saved. Build 3 split them out
    /// and began reinstalling the catalogue on every new version — by truncating it, in the App
    /// constructor, before the migration that was meant to move those places out had run. Every
    /// user updating from build 2 lost their list that way. The rule here is the one that was
    /// missing: a file still holding places is moved aside before anything is written over it,
    /// and the migration reads them from where it was moved.
    /// </summary>
    public static class CatalogueInstaller
    {
        public static CatalogueInstallResult Install(DatabasePaths paths, Func<Stream?> openShipped, bool isNewVersion)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(openShipped);

            bool exists = File.Exists(paths.Catalogue);
            if (exists && !isNewVersion)
            {
                return CatalogueInstallResult.NotNeeded;
            }

            bool parked = false;
            if (exists && HoldsSavedPlaces(paths.Catalogue))
            {
                if (File.Exists(paths.LegacyUserData))
                {
                    return CatalogueInstallResult.KeptExistingToProtectUserData;
                }

                File.Move(paths.Catalogue, paths.LegacyUserData);
                parked = true;
            }

            using Stream? shipped = openShipped();
            if (shipped is null)
            {
                // Nothing to replace it with, so put back what was moved rather than leave the
                // app with no city list at all.
                if (parked) File.Move(paths.LegacyUserData, paths.Catalogue);
                return CatalogueInstallResult.NoShippedCatalogue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(paths.Catalogue)!);

            // Written beside the target and moved into place, so a process killed mid-copy
            // leaves the previous catalogue rather than a truncated one.
            string temporary = paths.Catalogue + ".installing";
            using (FileStream target = File.Create(temporary))
            {
                shipped.CopyTo(target);
            }
            File.Move(temporary, paths.Catalogue, overwrite: true);

            return parked ? CatalogueInstallResult.InstalledAfterParkingUserData : CatalogueInstallResult.Installed;
        }

        /// <summary>Whether the file has a locations table with at least one row in it.</summary>
        public static bool HoldsSavedPlaces(string path)
        {
            try
            {
                using var database = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);

                int tables = database.ExecuteScalar<int>(
                    "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='locations';");

                return tables > 0 && database.ExecuteScalar<int>("SELECT count(*) FROM locations;") > 0;
            }
            catch (SQLiteException)
            {
                // Unreadable as a database: there is nothing in it the migration could recover
                // either, and keeping it would leave the app on a broken city list for good.
                return false;
            }
        }
    }
}
