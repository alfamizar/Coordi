using JustCompute.Persistence.Repository.Constants;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    /// <summary>
    /// The app's two databases, kept apart on purpose.
    ///
    /// <see cref="Catalogue"/> is the shipped world-city list: read-only, and replaceable wholesale
    /// on any release. <see cref="UserData"/> holds the places the user saved. They used to share
    /// one file, which meant the catalogue could never be refreshed without destroying the user's
    /// locations along with it. Separate files also mean the two connections no longer contend for
    /// the same shared-cache locks.
    /// </summary>
    public sealed class AppDatabaseConnection
    {
        public SQLiteAsyncConnection UserData { get; } =
            new(RepositoryConstants.UserDatabasePath, RepositoryConstants.Flags);

        public SQLiteAsyncConnection Catalogue { get; } =
            new(RepositoryConstants.CataloguePath, RepositoryConstants.Flags);
    }
}
