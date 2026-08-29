using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models.Eclipses
{
    /// <summary>
    /// One lunar eclipse, ready for display. A lunar eclipse looks the same for every
    /// observer on Earth's night side, so these contact times are global.
    ///
    /// Phases that do not occur for this eclipse are left at <see langword="default"/>
    /// (0001-01-01) rather than null, which is what the <c>IsDateTimeSetConverter</c>
    /// visibility bindings test for.
    /// </summary>
    public class LunarEclipseInfo
    {
        /// <summary>Date of the eclipse.</summary>
        public DateTime Date { get; init; }

        /// <summary>Eclipse type.</summary>
        public LunarEclipseType Type { get; init; }

        /// <summary>
        /// True when the Moon is above the horizon at greatest eclipse from the location the
        /// table was computed for. A lunar eclipse is identical for everyone on the night side,
        /// so visibility is purely a question of whether the Moon has risen.
        /// </summary>
        public bool IsVisible { get; init; }

        /// <summary>P1 — the Moon first touches the penumbra.</summary>
        public DateTime PenumbralEclipseBegin { get; init; }

        /// <summary>P4 — the Moon leaves the penumbra.</summary>
        public DateTime PenumbralEclipseEnd { get; init; }

        /// <summary>U1 — the umbral (partial) phase begins, if it occurs.</summary>
        public DateTime PartialEclipseBegin { get; init; }

        /// <summary>U4 — the umbral (partial) phase ends, if it occurs.</summary>
        public DateTime PartialEclipseEnd { get; init; }

        /// <summary>U2 — totality begins, if it occurs.</summary>
        public DateTime TotalEclipseBegin { get; init; }

        /// <summary>U3 — totality ends, if it occurs.</summary>
        public DateTime TotalEclipseEnd { get; init; }

        /// <summary>Greatest eclipse.</summary>
        public DateTime MidEclipse { get; init; }

        /// <summary>Fraction of the Moon's diameter inside the umbra at greatest eclipse.</summary>
        public double UmbralMagnitude { get; init; }

        /// <summary>Fraction of the Moon's diameter inside the penumbra at greatest eclipse.</summary>
        public double PenumbralMagnitude { get; init; }
    }
}
