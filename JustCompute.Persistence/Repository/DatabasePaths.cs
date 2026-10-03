using JustCompute.Persistence.Repository.Constants;

namespace JustCompute.Persistence.Repository
{
    /// <summary>
    /// Where the three database files live.
    ///
    /// A value rather than static properties so a test can point everything at a scratch
    /// directory. The migration below had no test at all until this existed, and that is the
    /// code that lost every build-2 user's saved places.
    /// </summary>
    /// <param name="Catalogue">The shipped world-city list, replaced wholesale on each release.</param>
    /// <param name="UserData">The places the user saved.</param>
    /// <param name="LegacyUserData">
    /// Where a catalogue still holding a user's places from before the split is parked, so the
    /// release's fresh catalogue can take its name without destroying them.
    /// </param>
    public sealed record DatabasePaths(string Catalogue, string UserData, string LegacyUserData)
    {
        public static DatabasePaths InDirectory(string directory) => new(
            Path.Combine(directory, RepositoryConstants.CatalogueDatabaseFilename),
            Path.Combine(directory, RepositoryConstants.UserDatabaseFilename),
            Path.Combine(directory, RepositoryConstants.LegacyUserDatabaseFilename));

        public static DatabasePaths Default =>
            InDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}
