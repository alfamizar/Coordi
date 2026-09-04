using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Repository
{
    /// <summary>
    /// The shipped, read-only city catalogue.
    ///
    /// It returns domain models. The interface used to be generic over the row type so that the
    /// core need not reference the persistence assembly, which achieved the opposite of what it
    /// looked like: the row type simply travelled through the type parameter into business logic
    /// instead, and every consumer named a SQLite table class.
    /// </summary>
    public interface IWorldCitiesRepository
    {
        /// <summary>The catalogue city closest to a point.</summary>
        Task<City> GetNearestCityAsync(double latitude, double longitude);

        /// <summary>
        /// Cities matching <paramref name="term"/>, at most <paramref name="limit"/> of them,
        /// largest first — as places the user could choose.
        /// </summary>
        Task<List<Location>> SearchAsync(string term, int limit);
    }
}
