namespace Compute.Core.Repository
{
    public interface IWorldCitiesDatabase<T>
    {
        Task<T> GetTheNearestCityAsync(double lat, double lng);

        Task<T> GetTheNearestCityByQueryAsync(double lat, double lng);

        Task<List<T>> GetCitiesInCountryAsync(string name);

        Task<IEnumerable<T>> FilterByCity(string searchParam);
    }
}