namespace Compute.Core.Domain.Entities.Models
{
    public class Location
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public City? City { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        public int TimeZoneOffset { get; set; }
    }
}
