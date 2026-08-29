namespace Compute.Core.Repository
{
    public interface IWorldCitiesRepository<T>
    {
        Task<T> GetTheNearestCityAsync(double lat, double lng);

        Task<T> GetTheNearestCityByQueryAsync(double lat, double lng);

        Task<List<T>> GetCitiesInCountryAsync(string name);

        /// <summary>Matching cities, at most <paramref name="limit"/> of them, largest first.</summary>
        Task<IEnumerable<T>> FilterByCity(string searchParam, int limit);
    }
}