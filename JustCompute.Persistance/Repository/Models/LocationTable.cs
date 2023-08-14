using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistance.Repository.Models.Base;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustCompute.Persistance.Repository.Models
{
    [Table("locations")]
    public class LocationTable : BaseTable
    {
        public LocationTable() { }

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int CityId { get; set; }

        public int IsActive { get; set; }

        public int IsCurrent { get; set; }

        public int TimeZoneOffset { get; set; }
    }
}
