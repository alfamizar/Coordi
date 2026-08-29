using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models.Eclipses
{
    /// <summary>
    /// One solar eclipse as seen from a specific place, ready for display.
    ///
    /// Contact times that do not occur for this eclipse at this location are left at
    /// <see langword="default"/> (0001-01-01) rather than null, which is what the
    /// <c>IsDateTimeSetConverter</c> visibility bindings test for.
    /// </summary>
    public class SolarEclipseInfo
    {
        /// <summary>Date of the eclipse.</summary>
        public DateTime Date { get; init; }

        /// <summary>Geocentric (global) eclipse type.</summary>
        public SolarEclipseType Type { get; init; }

        /// <summary>Type of eclipse actually seen from this location.</summary>
        public SolarEclipseLocalType LocalType { get; init; }

        /// <summary>True when at least a partial phase is visible from here with the Sun up.</summary>
        public bool IsVisible { get; init; }

        /// <summary>C1 — the partial phase begins.</summary>
        public DateTime PartialEclipseBegin { get; init; }

        /// <summary>C4 — the partial phase ends.</summary>
        public DateTime PartialEclipseEnd { get; init; }

        /// <summary>Local greatest eclipse.</summary>
        public DateTime MaximumEclipse { get; init; }

        /// <summary>C2 — the annular or total (central) phase begins, if it occurs here.</summary>
        public DateTime CentralEclipseBegin { get; init; }

        /// <summary>C3 — the annular or total (central) phase ends, if it occurs here.</summary>
        public DateTime CentralEclipseEnd { get; init; }

        /// <summary>Length of the annular or total phase, if it occurs here.</summary>
        public TimeSpan CentralDuration { get; init; }

        /// <summary>Fraction of the Sun's diameter covered at local maximum.</summary>
        public double Magnitude { get; init; }
    }
}
