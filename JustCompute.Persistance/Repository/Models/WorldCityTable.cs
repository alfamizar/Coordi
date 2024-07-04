using JustCompute.Persistance.Repository.Models.Base;
using TableAttribute = SQLite.TableAttribute;

namespace JustCompute.Persistance.Repository.Models
{
    [Table("worldcities")]
    public class WorldCityTable : BaseTable
    {
        public string City { get; set; } = string.Empty;

        public string CityAscii { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }

        public string Iso2 { get; set; } = string.Empty;

        public string Iso3 { get; set; } = string.Empty;

        public string AdminName { get; set; } = string.Empty;

        public string Capital { get; set; } = string.Empty;

        public int Population { get; set; }
    }
}
