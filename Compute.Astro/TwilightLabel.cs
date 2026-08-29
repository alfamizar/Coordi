namespace Compute.Astro
{
    /// <summary>Labels for the individual twilight-stage rows shown in Sun-related cards.</summary>
    public enum TwilightLabel
    {
        /// <summary>Astronomical dawn — the Sun reaches −18°.</summary>
        FirstLight,

        /// <summary>Nautical dawn — the Sun reaches −12°.</summary>
        NauticalDawn,

        /// <summary>Civil dawn — the Sun reaches −6°.</summary>
        CivilDawn,

        /// <summary>Civil dusk — the Sun drops past −6°.</summary>
        CivilDusk,

        /// <summary>Nautical dusk — the Sun drops past −12°.</summary>
        NauticalDusk,

        /// <summary>Astronomical dusk — the Sun drops past −18°.</summary>
        LastLight,
    }
}
