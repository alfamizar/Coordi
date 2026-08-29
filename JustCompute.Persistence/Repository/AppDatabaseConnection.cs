using JustCompute.Persistence.Repository.Constants;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    /// <summary>
    /// The single connection to the one database file the app owns.
    ///
    /// Both repositories read <c>geo_world.db</c> — saved locations and the world-city catalogue
    /// live side by side in it. Opening a connection each meant two of them over the same file
    /// with <see cref="SQLiteOpenFlags.SharedCache"/>, where SQLite takes table-level locks: a
    /// city search running while the Locations screen queried its own tables could fail with
    /// SQLITE_LOCKED. Sharing one connection removes the contention entirely.
    /// </summary>
    public sealed class AppDatabaseConnection
    {
        public SQLiteAsyncConnection Database { get; } =
            new(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
    }
}
