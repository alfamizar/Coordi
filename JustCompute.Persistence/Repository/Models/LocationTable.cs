using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistence.Repository.Models.Base;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustCompute.Persistence.Repository.Models
{
    [Table("locations")]
    public class LocationTable : BaseTable
    {
        public LocationTable() { }

        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int CityId { get; set; }

        public int IsActive { get; set; }

        public int IsCurrent { get; set; }

        /// <summary>
        /// Legacy whole-hour offset. Still written so an older build reading this database keeps
        /// working, but <see cref="TimeZoneId"/> is what the app resolves times from.
        /// </summary>
        public int TimeZoneOffset { get; set; }

        /// <summary>IANA zone id, or a fixed <c>UTC±HH:MM</c> id when the user pinned the offset.</summary>
        public string? TimeZoneId { get; set; }
    }
}
