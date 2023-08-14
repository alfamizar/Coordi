using JustCompute.Persistance.Repository.Models.Base;
using TableAttribute = SQLite.TableAttribute;

namespace JustCompute.Persistance.Repository.Models
{
    [Table("worldcities")]
    public class WorldCityTable : BaseTable
    {
        public string City { get; set; }

        public string CityAscii { get; set; }
        
        public string Country { get; set; }

        public double Lat { get; set; }

        public double Lng { get; set; }

        public string Iso2 { get; set; }

        public string Iso3 { get; set; }

        public string AdminName { get; set; }

        public string Capital { get; set; }

        public int Population { get; set; }
    }
}
