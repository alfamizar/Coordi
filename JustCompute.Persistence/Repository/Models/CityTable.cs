using JustCompute.Persistence.Repository.Models.Base;
using SQLite;

namespace JustCompute.Persistence.Repository.Models
{
    [Table("cities")]
    public class CityTable : BaseTable
    {
        public CityTable() { }

        public string? CityName { get; set; }

        public string? CountryName { get; set; }

        public int Population { get; set; }
    }
}
