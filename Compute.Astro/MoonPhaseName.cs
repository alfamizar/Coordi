namespace Compute.Astro
{
    /// <summary>
    /// The eight named lunar phases (for display), distinct from the four principal
    /// <see cref="MoonPhaseType"/>.
    /// </summary>
    public enum MoonPhaseName
    {
        /// <summary>New Moon.</summary>
        New,

        /// <summary>Waxing crescent.</summary>
        WaxingCrescent,

        /// <summary>First quarter.</summary>
        FirstQuarter,

        /// <summary>Waxing gibbous.</summary>
        WaxingGibbous,

        /// <summary>Full Moon.</summary>
        Full,

        /// <summary>Waning gibbous.</summary>
        WaningGibbous,

        /// <summary>Last quarter.</summary>
        LastQuarter,

        /// <summary>Waning crescent.</summary>
        WaningCrescent,
    }

    /// <summary>Naming of the Moon's visible phase.</summary>
    public static class MoonPhaseNaming
    {
        /// <summary>
        /// The named phase for a given illuminated fraction (0..1) and waxing/waning state.
        /// Quarters use a small band around 50% illumination.
        /// </summary>
        public static MoonPhaseName Of(double illuminatedFraction, bool waxing)
        {
            if (illuminatedFraction < 0.02) return MoonPhaseName.New;
            if (illuminatedFraction > 0.98) return MoonPhaseName.Full;
            if (illuminatedFraction >= 0.48 && illuminatedFraction <= 0.52)
            {
                return waxing ? MoonPhaseName.FirstQuarter : MoonPhaseName.LastQuarter;
            }

            if (waxing) return illuminatedFraction < 0.5 ? MoonPhaseName.WaxingCrescent : MoonPhaseName.WaxingGibbous;
            return illuminatedFraction < 0.5 ? MoonPhaseName.WaningCrescent : MoonPhaseName.WaningGibbous;
        }

        /// <summary>Convenience: the Moon's named phase at the given UTC Julian Day.</summary>
        public static MoonPhaseName At(double jd) => Of(Moon.IlluminatedFraction(jd), Moon.IsWaxing(jd));
    }
}
