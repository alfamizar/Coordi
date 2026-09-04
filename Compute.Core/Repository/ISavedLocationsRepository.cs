using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Repository
{
    /// <summary>
    /// The user's saved places, in domain terms.
    ///
    /// Deliberately not a generic query runner. The previous interface handed out
    /// <c>GetItemsAsync&lt;T&gt;</c> and <c>ExecuteAsync(sql)</c>, which meant its one consumer
    /// had to know the table types, the join, and the fact that a location and its city are two
    /// rows with independent id sequences. All of that is storage's business, and it lives behind
    /// these four methods now.
    /// </summary>
    public interface ISavedLocationsRepository
    {
        Task<List<Location>> GetAllAsync();

        /// <summary>
        /// Stores a new place. The generated ids are written back onto <paramref name="location"/>
        /// and its city, because a later update or delete has to address the right rows.
        /// </summary>
        Task AddAsync(Location location);

        Task UpdateAsync(Location location);

        Task DeleteAsync(Location location);
    }
}
