using JustCompute.Persistance.Repository.Models.Base;
using SQLite;

namespace JustCompute.Persistance.Repository.Models
{
    [Table("cities")]
    public class CityTable : BaseTable
    {
        public CityTable() { }

        public string CityName { get; set; }

        public string CountryName { get; set; }

        public int Population { get; set; }
    }
}
