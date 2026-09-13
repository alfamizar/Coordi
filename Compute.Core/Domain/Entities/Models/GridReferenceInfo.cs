using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>A UTM grid reference, shaped for display.</summary>
    public class UtmInfo
    {
        /// <summary>Reciprocal of the WGS84 flattening — constant, shown for reference.</summary>
        public double InverseFlattening => 298.257223563;

        /// <summary>Metres east within the zone (includes the 500 km false easting).</summary>
        public double Easting { get; init; }

        /// <summary>Metres north (false-northing adjusted, so positive in both hemispheres).</summary>
        public double Northing { get; init; }

        /// <summary>MGRS latitude-band letter, C..X.</summary>
        public string LatZone { get; init; } = string.Empty;

        /// <summary>Longitudinal zone number, 1..60.</summary>
        public int LongZone { get; init; }

        /// <summary>Grid system in use. Always UTM here — polar UPS is not supported.</summary>
        public string SystemType => "UTM";

        /// <summary>Builds the display model from a computed <see cref="Utm"/>.</summary>
        public static UtmInfo From(Utm utm) => new()
        {
            Easting = utm.Easting,
            Northing = utm.Northing,
            LatZone = utm.LatBand.ToString(),
            LongZone = utm.ZoneNumber,
        };
    }

    /// <summary>An MGRS grid reference, shaped for display.</summary>
    public class MgrsInfo
    {
        /// <summary>The two-letter 100 km square identifier.</summary>
        public string Digraph { get; init; } = string.Empty;

        /// <summary>Metres east within the 100 km square, 0..99999.</summary>
        public int Easting { get; init; }

        /// <summary>Metres north within the 100 km square, 0..99999.</summary>
        public int Northing { get; init; }

        /// <summary>MGRS latitude-band letter, C..X.</summary>
        public string LatZone { get; init; } = string.Empty;

        /// <summary>Longitudinal zone number, 1..60.</summary>
        public int LongZone { get; init; }

        /// <summary>Grid system in use. Always MGRS here — polar UPS is not supported.</summary>
        public string SystemType => "MGRS";

        /// <summary>Builds the display model from a computed <see cref="Mgrs"/>.</summary>
        public static MgrsInfo From(Mgrs mgrs) => new()
        {
            Digraph = mgrs.Digraph,
            Easting = mgrs.Easting,
            Northing = mgrs.Northing,
            LatZone = mgrs.LatBand.ToString(),
            LongZone = mgrs.ZoneNumber,
        };
    }
}
