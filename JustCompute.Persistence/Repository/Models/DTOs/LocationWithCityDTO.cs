namespace JustCompute.Persistence.Repository.Models.DTOs
{
    public class LocationWithCityDTO
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        public string CityName { get; set; } = string.Empty;

        public string CountryName { get; set; }  = string.Empty;

        public int Population { get; set; }

        public int TimeZoneOffset { get; set; }

        public string? TimeZoneId { get; set; }

        /// <summary>Aliased in the query so it does not collide with the location's own CityId.</summary>
        public int CityRowId { get; set; }
    }
}
